using FirstGearGames.Utilities.Networks;
using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{
    [System.Serializable]
    public struct TransformDataMessage : NetworkMessage
    {
        public ushort SequenceId;
        public ArraySegment<byte> Data;
    }

    public class FlexNetworkTransformManager : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// Found NetworkManager singleton.
        /// </summary>
        private NetworkManager _networkManager = null;
        /// <summary>
        /// Current NetworkManager.
        /// </summary>
        public NetworkManager NetworkManager { get { return _networkManager; } }
        #endregion

        #region Private.
        /// <summary>
        /// Active FlexNetworkTransform components.
        /// </summary>
        private static List<FlexNetworkTransformBase> _activeFlexNetworkTransforms = new List<FlexNetworkTransformBase>();
        /// <summary>
        /// FlexNetworkTransforms which must be smoothed in LateUpdate.
        /// </summary>
        private static List<FlexNetworkTransformBase> _lateUpdateSmoothing = new List<FlexNetworkTransformBase>();
        /// <summary>
        /// Unreliable datas to send to all.
        /// </summary>
        private static List<ArraySegment<byte>> _toAllUnreliableData = new List<ArraySegment<byte>>();
        /// <summary>
        /// Reliable datas to send to all.
        /// </summary>
        private static List<ArraySegment<byte>> _toAllReliableData = new List<ArraySegment<byte>>();
        /// <summary>
        /// Unreliable datas to send to server.
        /// </summary>
        private static List<ArraySegment<byte>> _toServerUnreliableData = new List<ArraySegment<byte>>();
        /// <summary>
        /// Reliable datas to send send to server.
        /// </summary>
        private static List<ArraySegment<byte>> _toServerReliableData = new List<ArraySegment<byte>>();
        /// <summary>
        /// Unreliable datas sent to specific observers.
        /// </summary>
        private static Dictionary<NetworkConnection, List<ArraySegment<byte>>> _observerUnreliableData = new Dictionary<NetworkConnection, List<ArraySegment<byte>>>();
        /// <summary>
        /// Reliable datas sent to specific observers.
        /// </summary>
        private static Dictionary<NetworkConnection, List<ArraySegment<byte>>> _observerReliableData = new Dictionary<NetworkConnection, List<ArraySegment<byte>>>();
        /// <summary>
        /// True if a fixed frame.
        /// </summary>
        private bool _fixedFrame = false;
        /// <summary>
        /// Last fixed frame.
        /// </summary>
        private int _lastFixedFrame = -1;
        /// <summary>
        /// Last sequenceId sent by server.
        /// </summary>
        private ushort _lastServerSentSequenceId = 0;
        /// <summary>
        /// Last sequenceId received from server.
        /// </summary>
        private ushort _lastServerReceivedSequenceId = 0;
        /// <summary>
        /// Last sequenceId sent by this client.
        /// </summary>
        private ushort _lastClientSentSequenceId = 0;
        /// <summary>
        /// Last NetworkClient.active state.
        /// </summary>
        private bool _lastClientActive = false;
        /// <summary>
        /// Last NetworkServer.active state.
        /// </summary>
        private bool _lastServerActive = false;
        /// <summary>
        /// How much data can be bundled per reliable message.
        /// </summary>
        private int _reliableMTU = -1;
        /// <summary>
        /// How much data can be bundled per unreliable message.
        /// </summary>
        private int _unreliableMTU = -1;
        /// <summary>
        /// Buffer to send outgoing data. Segments will always be 1200 or less.
        /// </summary>
        private byte[] _writerBuffer = new byte[1200];
        /// <summary>
        /// Used to prevent GC with GetComponents.
        /// </summary>
        private List<FlexNetworkTransformBase> _getComponents = new List<FlexNetworkTransformBase>();
        /// <summary>
        /// Singleton of this script. Used to ensure script is not loaded more than once. This will change for NG once custom message subscriptions are supported.
        /// </summary>
        private static FlexNetworkTransformManager _instance;
        #endregion

        #region Const.
        /// <summary>
        /// Maximum packet size by default. This is used when packet size is unknown.
        /// </summary>
        private const int MAXIMUM_PACKET_SIZE = 1200;
        /// <summary>
        /// Guestimated amount of how much MTU will be needed to send one transform on any transport. This will likely never be a problem but just incase.
        /// </summary>
        private const int MINIMUM_MTU_REQUIREMENT = 150;
        #endregion

#if MIRROR
        /// <summary>
        /// Automatically initializes script. Only works for Mirror.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void MirrorFirstInitialize()
        {
            GameObject go = new GameObject();
            go.name = "FlexNetworkTransformManager";
            go.AddComponent<FlexNetworkTransformManager>();
        }
#endif

        private void Awake()
        {
            FirstInitialize();
        }

        /// <summary>
        /// Initializes script.
        /// </summary>
        private void FirstInitialize()
        {
            if (_instance != null)
            {
                Debug.LogError("Multiple FlexNetworkTransformManager instances found. This new instance will be destroyed.");
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void FixedUpdate()
        {
            /* Don't send if the same frame. Since
            * physics aren't actually involved there is
            * no reason to run logic twice on the
            * same frame; that will only hurt performance
            * and the network more. */
            if (Time.frameCount == _lastFixedFrame)
                return;
            _lastFixedFrame = Time.frameCount;

            _fixedFrame = true;
        }

        private void Update()
        {
#if MIRROR
            CheckRegisterHandlers();
#endif
            //Run updates on FlexNetworkTransforms.
            for (int i = 0; i < _activeFlexNetworkTransforms.Count; i++)
                _activeFlexNetworkTransforms[i].ManualUpdate(_fixedFrame);

            _fixedFrame = false;
            //Send any queued messages.
            SendMessages();
        }

        private void LateUpdate()
        {
            for (int i = 0; i < _lateUpdateSmoothing.Count; i++)
                _lateUpdateSmoothing[i].ManualLateUpdate();
        }


        /// <summary>
        /// Registers handlers for the client.
        /// </summary>
        private void CheckRegisterHandlers()
        {
            bool ncActive = Platforms.ReturnClientActive(NetworkManager);
            bool nsActive = Platforms.ReturnServerActive(NetworkManager);
            bool changed = (_lastClientActive != ncActive || _lastServerActive != nsActive);
            //If wasn't active previously but is now then get handlers again.
            if (changed && ncActive)
                NetworkReplaceHandlers(true);
            if (changed && nsActive)
                NetworkReplaceHandlers(false);

            _lastClientActive = ncActive;
            _lastServerActive = nsActive;
        }

        /// <summary>
        /// Adds to ActiveFlexNetworkTransforms.
        /// </summary>
        /// <param name="fntBase"></param>
        public static void AddToActive(FlexNetworkTransformBase fntBase)
        {
            _activeFlexNetworkTransforms.Add(fntBase);
            fntBase.SetManagerInternal(_instance);

            if (fntBase.SmoothingLoop == SmoothingLoops.LateUpdate)
                _lateUpdateSmoothing.Add(fntBase);
        }
        /// <summary>
        /// Removes from ActiveFlexNetworkTransforms.
        /// </summary>
        /// <param name="fntBase"></param>
        public static void RemoveFromActive(FlexNetworkTransformBase fntBase)
        {
            _activeFlexNetworkTransforms.Remove(fntBase);

            if (fntBase.SmoothingLoop == SmoothingLoops.LateUpdate)
                _lateUpdateSmoothing.Remove(fntBase);
        }

        /// <summary>
        /// Sends data to server.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="reliable"></param>
        public static void SendToServer(ref ArraySegment<byte> segment, bool reliable)
        {
            if (reliable)
                _toServerReliableData.Add(segment);
            else
                _toServerUnreliableData.Add(segment);
        }

        /// <summary>
        /// Sends data to all.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="reliable"></param>
        public static void SendToAll(ref ArraySegment<byte> segment, bool reliable)
        {
            if (reliable)
                _toAllReliableData.Add(segment);
            else
                _toAllUnreliableData.Add(segment);
        }
        /// <summary>
        /// Sends data to observers.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="segment"></param>
        /// <param name="reliable"></param>
        public static void SendToObserver(NetworkConnection conn, ref ArraySegment<byte> segment, bool reliable)
        {
            /* Do not send as reference because a copy needs to be made to ensure
            * data is not replaced in the collection. */
            /* Actually, it should be okay to use ref because each sync data is going to be the most recent.
             * And since I'm not ACTUALLY using delta, this shouldn't cause any harm. */
            Dictionary<NetworkConnection, List<ArraySegment<byte>>> dict = (reliable) ? _observerReliableData : _observerUnreliableData;

            List<ArraySegment<byte>> writers;
            //If doesn't have datas for connection yet then make new datas.
            if (!dict.TryGetValue(conn, out writers))
            {
                writers = new List<ArraySegment<byte>>();
                dict[conn] = writers;
            }

            writers.Add(segment);
        }

        /// <summary>
        /// Sends queued messages.
        /// </summary>
        private void SendMessages()
        {
            //If MTUs haven't been set yet.
            if (_reliableMTU == -1 || _unreliableMTU == -1)
                Platforms.SetMTU(ref _reliableMTU, ref _unreliableMTU, MAXIMUM_PACKET_SIZE);

            //Server.
            if (Platforms.ReturnServerActive(NetworkManager))
            {
                bool dataSent = false;

                //Reliable to all.
                if (SendTransformDatas(_lastServerSentSequenceId, false, null, _toAllReliableData, true))
                    dataSent = true;
                //Unreliable to all.
                if (SendTransformDatas(_lastServerSentSequenceId, false, null, _toAllUnreliableData, false))
                    dataSent = true;
                //Reliable to observers.
                foreach (KeyValuePair<NetworkConnection, List<ArraySegment<byte>>> item in _observerReliableData)
                {
                    //Null or unready network connection.
                    if (item.Key == null || !item.Key.IsReady())
                        continue;

                    if (SendTransformDatas(_lastServerSentSequenceId, false, item.Key, item.Value, true))
                        dataSent = true;
                }
                //Unreliable to observers.
                foreach (KeyValuePair<NetworkConnection, List<ArraySegment<byte>>> item in _observerUnreliableData)
                {
                    //Null or unready network connection.
                    if (item.Key == null || !item.Key.IsReady())
                        continue;

                    if (SendTransformDatas(_lastServerSentSequenceId, false, item.Key, item.Value, false))
                        dataSent = true;
                }

                if (dataSent)
                {
                    _lastServerSentSequenceId++;
                    if (_lastServerSentSequenceId == ushort.MaxValue)
                        _lastServerSentSequenceId = 0;
                }
            }
            //Client.
            if (Platforms.ReturnClientActive(NetworkManager))
            {
                bool dataSent = false;

                //Reliable to all.
                if (SendTransformDatas(_lastClientSentSequenceId, true, null, _toServerReliableData, true))
                    dataSent = true;
                //Unreliable to all.
                if (SendTransformDatas(_lastClientSentSequenceId, true, null, _toServerUnreliableData, false))
                    dataSent = true;

                if (dataSent)
                {
                    _lastClientSentSequenceId++;
                    if (_lastClientSentSequenceId == ushort.MaxValue)
                        _lastClientSentSequenceId = 0;
                }
            }

            ClearSegmentsCache();
        }

        /// <summary>
        /// Disposes of all cached writers.
        /// </summary>
        private void ClearSegmentsCache()
        {
            _toServerReliableData.Clear();
            _toServerUnreliableData.Clear();
            _toAllReliableData.Clear();
            _toAllUnreliableData.Clear();
            _observerReliableData.Clear();
            _observerUnreliableData.Clear();
        }

        /// <summary>
        /// Sends data to all or specified connection.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="writers"></param>
        /// <param name="reliable"></param>
        private bool SendTransformDatas(ushort sequenceId, bool toServer, NetworkConnection conn, List<ArraySegment<byte>> segments, bool reliable)
        {
            int index = 0;
            int channel = (reliable) ? 0 : 1;
            int mtu = (reliable) ? _reliableMTU : _unreliableMTU;
            //Subtract a set amount from mtu to account for headers and misc data.
            mtu -= 75;
#if UNITY_EDITOR
            if (mtu < MINIMUM_MTU_REQUIREMENT)
                Debug.LogWarning("MTU is dangerously low on channel " + channel + ". Data may not send properly.");
#endif

            bool dataSent = false;
            while (index < segments.Count)
            {
                int writerPosition = 0;
                //Write until break or all data is written.
                while (writerPosition < mtu && index < segments.Count)
                {
                    ArraySegment<byte> segment = segments[index];
                    int segmentCount = segment.Count;
                    //If will fit into the packet.
                    if (segmentCount + writerPosition <= mtu)
                    {
                        Array.Copy(segment.Array, 0, _writerBuffer, writerPosition, segmentCount);
                        writerPosition += segmentCount;
                        //Writers used to be disposed here. See bottom of method notes.
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }

                TransformDataMessage msg = new TransformDataMessage()
                {
                    SequenceId = sequenceId,
                    Data = new ArraySegment<byte>(_writerBuffer, 0, writerPosition)
                };

                if (toServer)
                {
                    Platforms.ClientSend(NetworkManager, msg, channel);
                }
                else
                {
                    //If no connection then send to all.
                    if (conn == null)
                        Platforms.ServerSendToAll(NetworkManager, msg, channel);
                    //Otherwise send to connection.
                    else
                        conn.Send(msg, channel);
                }

                dataSent = true;
            }

            return dataSent;
        }


        /// <summary>
        /// Received on clients when server sends data.
        /// </summary>
        /// <param name="msg"></param>
        private void OnServerTransformData(TransformDataMessage msg)
        {
            //Old packet.
            if (IsOldPacket(_lastServerReceivedSequenceId, msg.SequenceId))
                return;

            _lastServerReceivedSequenceId = msg.SequenceId;
            int readPosition = 0;
            while (readPosition < msg.Data.Count)
            {
                //Deserialize data to find component/
                uint netId;
                byte componentIndex;
                SyncProperties syncProperties;
                Serialization.DeserializeComponent(ref readPosition, ref msg.Data, out netId, out componentIndex, out syncProperties);
                //Try to find fnt for netId/componentIndex.
                FlexNetworkTransformBase fntBase = null;
                if (Platforms.ReturnSpawned(NetworkManager).TryGetValue(netId, out NetworkIdentity ni))
                    fntBase = ReturnFNTBaseOnNetworkIdentity(ni, componentIndex);

                //Still need to deserialize the remainder otherwise the stream position will be off.
                TransformData td = new TransformData();
                Serialization.DeserializeTransformData(ref readPosition, ref msg.Data, ref td, fntBase, netId, componentIndex, syncProperties);

                if (fntBase != null)
                    fntBase.ServerDataReceived(ref td);
            }

        }

        /// <summary>
        /// Received on server when client sends data.
        /// </summary>
        /// <param name="msg"></param>
        private void OnClientTransformData(TransformDataMessage msg)
        {
            //Have to check sequence id against the FNT sending.

            int readPosition = 0;
            while (readPosition < msg.Data.Count)
            {
                //Deserialize data to find component.
                uint netId;
                byte componentIndex;
                SyncProperties syncProperties;
                Serialization.DeserializeComponent(ref readPosition, ref msg.Data, out netId, out componentIndex, out syncProperties);

                //Try to find fnt for netId/componentIndex.
                FlexNetworkTransformBase fntBase = null;
                if (Platforms.ReturnSpawned(NetworkManager).TryGetValue(netId, out NetworkIdentity ni))
                    fntBase = ReturnFNTBaseOnNetworkIdentity(ni, componentIndex);

                //Still need to deserialize the remainder otherwise the stream position will be off.
                TransformData td = new TransformData();
                Serialization.DeserializeTransformData(ref readPosition, ref msg.Data, ref td, fntBase, netId, componentIndex, syncProperties);
                if (fntBase != null)
                {
                    //Skip if old packet.
                    if (IsOldPacket(fntBase.LastClientSequenceId, msg.SequenceId))
                        continue;

                    /* SequenceId is set per FNT because clients will be sending
                     * different sequenceIds each. */
                    fntBase.SetLastClientSequenceIdInternal(msg.SequenceId);
                    fntBase.ClientDataReceived(ref td);
                }
            }
        }

        /// <summary>
        /// Returns a FlexNetworkTransformBase on a networkIdentity using a componentIndex.
        /// </summary>
        /// <param name="componentIndex"></param>
        /// <returns></returns>
        private FlexNetworkTransformBase ReturnFNTBaseOnNetworkIdentity(NetworkIdentity ni, byte componentIndex)
        {
            NetworkBehaviour nb = Lookups.ReturnNetworkBehaviour(ni, componentIndex);
            if (nb == null)
                return null;

            nb.GetComponents<FlexNetworkTransformBase>(_getComponents);
            /* Now find the FNTBase which matches the component index. There is probably only one FNT
             * but if the user were using FNT + FNT Child there could be more so it's important to get all FNT
             * on the object. */
            for (int i = 0; i < _getComponents.Count; i++)
            {
                //Match found.
                if (_getComponents[i].CachedComponentIndex == componentIndex)
                    return _getComponents[i];
            }

            /* If here then the component index was found but the fnt with the component index
             * was not. This should never happen. */
            Debug.LogWarning("ComponentIndex found but FlexNetworkTransformBase was not.");
            return null;
        }


        /// <summary>
        /// Returns if a packet is old or out of order.
        /// </summary>
        /// <param name="lastSequenceId"></param>
        /// <param name="sequenceId"></param>
        /// <returns></returns>
        private bool IsOldPacket(ushort lastSequenceId, ushort sequenceId, ushort resetRange = 256)
        {
            /* New Id is equal or higher. Allow equal because
             * the same sequenceId will be used for when bundling
             * hundreds of FNTs over multiple sends. */
            if (sequenceId >= lastSequenceId)
            {
                return false;
            }
            //New sequenceId isn't higher, check if perhaps the sequenceId reset to 0.
            else
            {
                ushort difference = (ushort)Mathf.Abs(lastSequenceId - sequenceId);
                /* Return old packet if difference isnt beyond
                 * the reset range. Difference should be extreme if a reset occurred. */
                return (difference < resetRange);
            }
        }

        /// <summary>
        /// Replaces handlers.
        /// </summary>
        /// <param name="client">True to replace for client.</param>
        private void NetworkReplaceHandlers(bool client)
        {
            if (client)
                NetworkClient.ReplaceHandler<TransformDataMessage>(OnServerTransformData);
            else
                NetworkServer.ReplaceHandler<TransformDataMessage>(OnClientTransformData);
        }
    }


}