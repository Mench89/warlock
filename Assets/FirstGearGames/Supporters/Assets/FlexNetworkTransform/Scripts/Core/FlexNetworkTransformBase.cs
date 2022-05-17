using FirstGearGames.Utilities.Networks;
using FirstGearGames.Utilities.Editors;
using FirstGearGames.Utilities.Maths;
using FirstGearGames.Utilities.Objects;
using System;
using UnityEngine;
using Mirror;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{


    public abstract class FlexNetworkTransformBase : NetworkBehaviour
    {
        #region Types.
        /// <summary>
        /// TransformData cached after a forced send.
        /// </summary>
        private struct CachedTransformData
        {
            /// <summary>
            /// Cached TransformData.
            /// </summary>
            public TransformData Data;
            /// <summary>
            /// True if data was received from the server.
            /// </summary>
            public bool FromServer;
            /// <summary>
            /// Number of times data has been cached before being processed.
            /// This is similar to using a buffer but instead of adding onto the buffer and iterating over it calculations are made based on this value to determine how long it should take to travel to the most recently cached data.
            /// Realistically only one data will be cached at a time but even if more were to be the odds of there being any visual disturbance are extremely low. A good trade off for performance gained by not using a buffer.
            /// </summary>
            public int CachedCount { get; private set; }

            public void CacheData(TransformData transformData, bool fromServer)
            {
                Data = transformData;
                FromServer = fromServer;
                CachedCount++;
            }

            /// <summary>
            /// Resets the CachedCount to 0.
            /// </summary>
            public void ResetCachedCount()
            {
                CachedCount = 0;
            }
        }
        /// <summary>
        /// Extrapolation for the most recent received data.
        /// </summary>
        protected struct ExtrapolationData
        {
            public float Remaining;
            public Vector3 Position;
            public bool IsSet;

            public void UpdateValues(Vector3 position, float remaining, bool isSet)
            {
                Remaining = remaining;
                Position = position;
                IsSet = isSet;
            }
        }

        /// <summary>
        /// Move rates for the most recent received data.
        /// </summary>
        protected struct MoveRateData
        {
            public float Position;
            public float Rotation;
            public float Scale;
        }

        /// <summary>
        /// Data used to manage moving towards a target.
        /// </summary>
        protected struct GoalData
        {
            public void UpdateValues(TransformData transformData, MoveRateData moveRates, ExtrapolationData extrapolationData)
            {
                Goal = transformData;
                MoveRates = moveRates;
                Extrapolation = extrapolationData;
                IsSet = true;
            }

            /// <summary>
            /// Transform goal data for this update.
            /// </summary>
            public TransformData Goal;
            /// <summary>
            /// How quickly to move towards each transform property.
            /// </summary>
            public MoveRateData MoveRates;
            /// <summary>
            /// How much extrapolation time remains.
            /// </summary>
            public ExtrapolationData Extrapolation;
            /// <summary>
            /// True if values are set.
            /// </summary>
            public bool IsSet;
            /// <summary>
            /// Resets data as though nothing exist.
            /// </summary>
            public void Reset()
            {
                IsSet = false;
            }
        }
        /// <summary>
        /// Ways to synchronize datas.
        /// </summary>
        [System.Serializable]
        public enum SynchronizeTypes : int
        {
            Normal = 0,
            NoSynchronization = 1
        }
        /// <summary>
        /// Interval types to determine when to synchronize data.
        /// </summary>
        [System.Serializable]
        private enum IntervalTypes : int
        {
            Timed = 0,
            FixedUpdate = 1
        }
        #endregion

        #region Public.
        /// <summary>
        /// Dispatched when server receives data from a client while using client authoritative.
        /// </summary>
        public event Action<ReceivedClientData> OnClientDataReceived;
        /// <summary>
        /// Transform to monitor and modify.
        /// </summary>
        public abstract Transform TargetTransform { get; }
        /// <summary>
        /// AttachedData for what this object is attached to.
        /// </summary>
        public AttachedGoalData Attached { get; private set; } = new AttachedGoalData();
        /// <summary>
        /// Sets which object this transform is attached to.
        /// </summary>
        /// <param name="attached"></param>
        protected void SetAttachedInternal(NetworkIdentity attached, sbyte componentIndex)
        {
            uint netId = (attached == null) ? 0 : attached.ReturnNetworkId();
            UpdateAttached(netId, componentIndex);
        }
        /// <summary>
        /// LastSequenceId received from the client for this FlexNetworkTransformBase.
        /// </summary>
        public ushort LastClientSequenceId { get; private set; }
        /// <summary>
        /// Sets the LastSequenceId value.
        /// </summary>
        /// <param name="value"></param>
        public void SetLastClientSequenceIdInternal(ushort value)
        {
            LastClientSequenceId = value;
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True to synchronize using LocalSpace values. False to use WorldSpace.")]
        [SerializeField]
        private bool _useLocalSpace = true;
        /// <summary>
        /// True to synchronize using LocalSpace values. False to use WorldSpace.
        /// </summary>
        protected bool UseLocalSpace { get { return _useLocalSpace; } }
        /// <summary>
        /// How to operate synchronization timings. Timed will synchronized every specified interval while FixedUpdate will synchronize every FixedUpdate.
        /// </summary>
        [Tooltip("How to operate synchronization timings. Timed will synchronized every specified interval while FixedUpdate will synchronize every FixedUpdate.")]
        [SerializeField]
        private IntervalTypes _intervalType = IntervalTypes.Timed;
        /// <summary>
        /// How often to synchronize this transform.
        /// </summary>
        [Tooltip("How often to synchronize this transform.")]
        [Range(0.00f, 0.5f)]
        [SerializeField]
        private float _synchronizeInterval = 0.1f;
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("When to move towards latest values received from the server.")]
        [SerializeField]
        private SmoothingLoops _smoothingLoop = SmoothingLoops.Update;
        /// <summary>
        /// When to move towards latest values received from the server.
        /// </summary>
        public SmoothingLoops SmoothingLoop { get { return _smoothingLoop; } }
        /// <summary>
        /// How far in the past objects should be for interpolation. Higher values will result in smoother movement with network fluctuations but lower values will result in objects being closer to their actual position. Lower values can generally be used for longer synchronization intervalls.
        /// </summary>
        [Tooltip("How far in the past objects should be for interpolation. Higher values will result in smoother movement with network fluctuations but lower values will result in objects being closer to their actual position. Lower values can generally be used for longer synchronization intervals.")]
        [Range(0.00f, 0.5f)]
        [SerializeField]
        private float _interpolationFallbehind = 0.06f;
        /// <summary>
        /// How long to extrapolate when data is expected but does not arrive. Smaller values are best for fast synchronization intervals. For precision or fast reaction games you may want to use no extrapolation or only one or two synchronization intervals worth. Extrapolation is client-side only.
        /// </summary>
        [Tooltip("How long to extrapolate when data is expected but does not arrive. Smaller values are best for fast synchronization intervals. For precision or fast reaction games you may want to use no extrapolation or only one or two synchronization intervals worth. Extrapolation is client-side only.")]
        [Range(0f, 5f)]
        [SerializeField]
        private float _extrapolationSpan = 0f;
        /// <summary>
        /// Teleport the transform if the distance between received data exceeds this value. Use 0f to disable.
        /// </summary>
        [Tooltip("Teleport the transform if the distance between received data exceeds this value. Use 0f to disable.")]
        [SerializeField]
        private float _teleportThreshold = 0f;
        /// <summary>
        /// True if using client authoritative movement.
        /// </summary>
        [Tooltip("True if using client authoritative movement.")]
        [SerializeField]
        private bool _clientAuthoritative = true;
        /// <summary>
        /// True to synchronize server results back to owner. Typically used when you are sending inputs to the server and are relying on the server response to move the transform.
        /// </summary>
        [Tooltip("True to synchronize server results back to owner. Typically used when you are sending inputs to the server and are relying on the server response to move the transform.")]
        [SerializeField]
        private bool _synchronizeToOwner = true;
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True to compress small values on position and scale. Values will be rounded to the hundredth decimal place, eg: 102.12f.")]
        [SerializeField]
        private bool _compressSmall = true;
        /// <summary>
        /// True to compress small values on position and scale. Values will be rounded to the hundredth decimal place, eg: 102.12f.
        /// </summary>
        public bool CompressSmall { get { return _compressSmall; } }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Synchronize options for position.")]
        [SerializeField]
        private SynchronizeTypes _synchronizePosition = SynchronizeTypes.Normal;
        /// <summary>
        /// Sets SynchronizePosition value.
        /// </summary>
        /// <param name="syncType"></param>
        public void SetSynchronizePosition(SynchronizeTypes syncType) { _synchronizePosition = syncType; }
        /// <summary>
        /// Gets SynchronizePosition value.
        /// </summary>
        /// <param name="syncType"></param>
        public SynchronizeTypes GetSynchronizePosition() { return _synchronizePosition; }
        /// <summary>
        /// Euler axes on the position to snap into place rather than move towards over time.
        /// </summary>
        [Tooltip("Euler axes on the rotation to snap into place rather than move towards over time.")]
        [SerializeField]
        [BitMask(typeof(Vector3Axes))]
        private Vector3Axes _snapPosition = (Vector3Axes)0;
        /// <summary>
        /// Sets SnapPosition value. For internal use only. Must be public for editor script.
        /// </summary>
        /// <param name="value"></param>
        public void SetSnapPosition(Vector3Axes value) { _snapPosition = value; }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Synchronize states for position.")]
        [SerializeField]
        private SynchronizeTypes _synchronizeRotation = SynchronizeTypes.Normal;
        /// <summary>
        /// Sets SynchronizeRotation value.
        /// </summary>
        /// <param name="syncType"></param>
        public void SetSynchronizeRotation(SynchronizeTypes syncType) { _synchronizeRotation = syncType; }
        /// <summary>
        /// Gets SynchronizeRotation value.
        /// </summary>
        /// <param name="syncType"></param>
        public SynchronizeTypes GetSynchronizeRotation() { return _synchronizeRotation; }
        /// <summary>
        /// Euler axes on the rotation to snap into place rather than move towards over time.
        /// </summary>
        [Tooltip("Euler axes on the rotation to snap into place rather than move towards over time.")]
        [SerializeField]
        [BitMask(typeof(Vector3Axes))]
        private Vector3Axes _snapRotation = (Vector3Axes)0;
        /// <summary>
        /// Sets SnapRotation value. For internal use only. Must be public for editor script.
        /// </summary>
        /// <param name="value"></param>
        public void SetSnapRotation(Vector3Axes value) { _snapRotation = value; }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Synchronize states for scale.")]
        [SerializeField]
        private SynchronizeTypes _synchronizeScale = SynchronizeTypes.Normal;
        /// <summary>
        /// Sets SynchronizeScale value.
        /// </summary>
        /// <param name="syncType"></param>
        public void SetSynchronizeScale(SynchronizeTypes syncType) { _synchronizeScale = syncType; }
        /// <summary>
        /// Gets SynchronizeScale value.
        /// </summary>
        /// <param name="syncType"></param>
        public SynchronizeTypes GetSynchronizeScale() { return _synchronizeScale; }
        /// <summary>
        /// Euler axes on the scale to snap into place rather than move towards over time.
        /// </summary>
        [Tooltip("Euler axes on the scale to snap into place rather than move towards over time.")]
        [SerializeField]
        [BitMask(typeof(Vector3Axes))]
        private Vector3Axes _snapScale = (Vector3Axes)0;
        /// <summary>
        /// Sets SnapScale value. For internal use only. Must be public for editor script.
        /// </summary>
        /// <param name="value"></param>
        public void SetSnapScale(Vector3Axes value) { _snapScale = value; }
        #endregion

        #region Private.
        #region Cached network information.
        /// <summary>
        /// True if is server.
        /// </summary>
        private bool _isServer = false;
        /// <summary>
        /// True if is server only.
        /// </summary>
        private bool _isServerOnly { get { return (_isServer && !_isClient); } }
        /// <summary>
        /// True if is client.
        /// </summary>
        private bool _isClient = false;
        /// <summary>
        /// True if has an owner.
        /// </summary>
        private bool _hasOwner { get { return (_owner != null); } }
        /// <summary>
        /// Current owner.
        /// </summary>
        private NetworkConnection _owner = null;
        /// <summary>
        /// True if has authority.
        /// </summary>
        private bool _hasAuthority = false;
        /// <summary>
        /// True if attachedValid.
        /// </summary>
        private bool _attachedValid = false;
        /// <summary>
        /// netId for this NetworkBehaviour.
        /// </summary>
        private uint _netId = 0;
        #endregion
        /// <summary>
        /// True to hold the next change one interval before sending to clients.
        /// </summary>
        private bool _holdToClients = true;
        /// <summary>
        /// True to hold the next change one interval before sending to server.
        /// </summary>
        private bool _holdToServer = true;
        /// <summary>
        /// TransformData which is cached due to a forced data being fulfilled.
        /// </summary>
        private CachedTransformData _cachedTransformData;
        /// <summary>
        /// TransformData which was received OnDeserialize. This is a work-around for Mirror not having
        /// all NetworkIdentities spawned before pushing through initialization data.
        /// </summary>
        private TransformData _deserializedTransformData;
        /// <summary>
        /// Last TransformData sent by client.
        /// </summary>
        private TransformData _clientTransformData;
        /// <summary>
        /// Last TransformData sent by server.
        /// </summary>
        private TransformData _serverTransformData;
        /// <summary>
        /// GoalData to move towards.
        /// </summary>
        private GoalData _targetData;
        /// <summary>
        /// Next time client may send data.
        /// </summary>
        private float _nextClientSendTime = 0f;
        /// <summary>
        /// Next time server may send data.
        /// </summary>
        private float _nextServerSendTime = 0f;
        /// <summary>
        /// When sending data from client, after the transform stops changing and when using unreliable this becomes true while a reliable packet is being sent.
        /// </summary>
        private bool _clientSettleSent = false;
        /// <summary>
        /// When sending data from server, after the transform stops changing and when using unreliable this becomes true while a reliable packet is being sent.
        /// </summary>
        private bool _serverSettleSent = false;
        /// <summary>
        /// TeleportThreshold value squared.
        /// </summary>
        private float _teleportThresholdSquared;
        /// <summary>
        /// Time in which the transform was detected as idle.
        /// </summary>
        private float _transformIdleStart = -1f;
        /// <summary>
        /// FlexNetworkTransformManager reference.
        /// </summary>
        private FlexNetworkTransformManager _manager;
        /// <summary>
        /// Sets the FlexNetworkTransformManager reference.
        /// </summary>
        /// <param name="manager"></param>
        public void SetManagerInternal(FlexNetworkTransformManager manager) { _manager = manager; }
        /// <summary>
        /// Last authoritative client for this object.
        /// </summary>
        private NetworkConnection _lastOwner = null;
        /// <summary>
        /// Time when MoveTowards should be completed based on sync interval and fallbehind.
        /// </summary>
        private float _moveTowardsEndTime = -1f;
        /// <summary>
        /// Updates going to the server.
        /// </summary>
        private byte[] _toServerBuffer = new byte[0];
        /// <summary>
        /// Updates going to clients.
        /// </summary>
        private byte[] _toClientsBuffer = new byte[0];
        /// <summary>
        /// 
        /// </summary>
        private byte? _cachedComponentIndex = null;
        /// <summary>
        /// Cached ComponentIndex for the NetworkBehaviour this FNT is on. This is because Mirror codes bad.
        /// </summary>
        public byte CachedComponentIndex
        {
            get
            {
                if (_cachedComponentIndex == null)
                {
                    //Exceeds value.
                    if (base.ComponentIndex > 255)
                    {
                        Debug.LogError("ComponentIndex is larger than supported type.");
                        _cachedComponentIndex = 0;
                    }
                    //Doesn't exceed value.
                    else
                    {
                        _cachedComponentIndex = (byte)Mathf.Abs(base.ComponentIndex);
                    }
                }

                return _cachedComponentIndex.Value;
            }
        }
        #endregion

#pragma warning disable CS0618, CS0672

        #region Initializers and Ticks
        protected virtual void Awake()
        {
            SetTeleportThresholdSquared();
        }

        protected virtual void OnDestroy()
        {
            if (Attached.MovingTarget != null)
                Destroy(Attached.MovingTarget.gameObject);
        }

        protected virtual void OnEnable()
        {
            FlexNetworkTransformManager.AddToActive(this);
        }
        protected virtual void OnDisable()
        {
            FlexNetworkTransformManager.RemoveFromActive(this);
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                /* Serialization will already include pos/rot/scale if not a child object.
                 * Can use TargetTransformIsChild to check if child. Also must send no matter what if attached is valid
                 * as that feature uses it's own space system, and in addition must send attached data. */
                bool sendData = (_attachedValid || TargetTransformIsChild());
                writer.WriteBoolean(sendData);
                if (sendData)
                {
                    //By default send all configured properties.
                    SyncProperties sp = ReturnConfiguredProperties();
                    //Apply any special properties, such as Attached.
                    ApplySpecialProperties(ref sp, true);
                    //If targetdata is already set then get the values from target data.
                    bool usingTargetData = _targetData.IsSet;

                    //TransformData which will be populated.
                    TransformData td = new TransformData();
                    /* Since only updated values are sent to receivers
                    * the syncproperties on the included targetData will only
                    * have those changed properties. Because of this I must
                    * override the targetData syncproperties with all configured
                    * properties. Even though SyncProperties might be missing the
                    * transform properties are not as they are filled with
                    * last received values upon targetData creation. */
                    if (usingTargetData)
                        UpdateTransformData(ref td, sp, ref _targetData, -1, false);
                    //Not using target data.
                    else
                        UpdateTransformData(ref td, sp, -1, false);

                    Serialization.SerializeTransformData(writer, td, this);
                }
            }

            return base.OnSerialize(writer, initialState);
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                bool dataSent = reader.ReadBoolean();
                if (dataSent)
                {
                    /* At the time of this release Mirror deserializer is showing 5 more bytes
                     * than what's being sent. Because of this I cannot properly fetch the appropriate amount
                     * of data to be deserialized for the transform update. As a work around I will
                     * store read position, grab all of the data, reset the read position, and then run it through
                     * my serializer which will bump the read position up the appropriate amount. FishNet needs to happen. */
                    int initialReadPosition = reader.Position;
                    int readLength = (reader.Length - reader.Position);
                    //Get all remaining data.
                    ArraySegment<byte> data = reader.ReadBytesSegment(readLength);
                    reader.Position = initialReadPosition;

                    //Deserialize data to find component.
                    uint netId;
                    byte componentIndex;
                    SyncProperties syncProperties;
                    Serialization.DeserializeComponent(reader, ref initialReadPosition, ref data, out netId, out componentIndex, out syncProperties);
                    //Deserialize values.
                    TransformData td = new TransformData();
                    Serialization.DeserializeTransformData(reader, ref initialReadPosition, ref data, ref td, this, netId, componentIndex, syncProperties);
                    //If not server then set deserialized transform data.
                    if (!_isServer)
                        _deserializedTransformData = td;
                }
            }

            base.OnDeserialize(reader, initialState);
        }


#if MIRROR
        public override void OnStartServer()
        {
            base.OnStartServer();
            StartServer();
        }
        public override void OnStopServer()
        {
            base.OnStopServer();
            StopServer();
        }
#endif
        private void StartServer()
        {
            _netId = base.netId;
            _isServer = true;
        }
        private void StopServer()
        {
            _isServer = false;
        }

#if MIRROR
        public override void OnStartClient()
        {
            base.OnStartClient();
            StartClient();
        }
        public override void OnStopClient()
        {
            base.OnStopClient();
            StopClient();
        }
#endif
        private void StartClient()
        {
            _netId = base.netId;
            _isClient = true;
            CheckCreateTransformGoalDataData();
        }
        private void StopClient()
        {
            _isClient = false;
        }


#if MIRROR
        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            StartAuthority();
        }
#endif
        private void StartAuthority()
        {
            _hasAuthority = true;
            /* If have authority and client authoritative then there is
            * no reason to have a targe data. */
            if (_clientAuthoritative)
                UnsetTargetDatas();
        }

#if MIRROR
        public override void OnStopAuthority()
        {
            base.OnStopAuthority();
            StopAuthority();
        }
#endif

        private void StopAuthority()
        {
            _hasAuthority = false;
            CheckCreateTransformGoalDataData();
        }

        public void ManualUpdate(bool fixedUpdate)
        {
            /* This is a hack to fix attached not working via OnDeserialize
             * because Mirror doesn't ensure all objects are spawned before
             * OnDeserialize comes through, resulting in null lookups
             * for attached network identities. */
            if (_deserializedTransformData.IsSet)
            {
                ServerDataReceived(ref _deserializedTransformData);
                _deserializedTransformData.IsSet = false;
            }

            /* These values must be set every frame because there's no
             * no way to know when they've changed. */
            _attachedValid = AttachedValid();

            if (_isServer)
                _owner = base.connectionToClient;

            CheckResetSequenceIds();
            CheckSendToServer(fixedUpdate);
            CheckSendToClients(fixedUpdate);

            bool moveTowardsTarget = (SmoothingLoop == SmoothingLoops.Update) ||
                (fixedUpdate && SmoothingLoop == SmoothingLoops.FixedUpdate);
            if (moveTowardsTarget)
                MoveTowardsTarget();

            /* Attached target is moved smoothly on clients and the actual
             * transform snaps to the target. */
            SnapToAttached();
        }

        public void ManualLateUpdate()
        {
            /* As of now ManualLateUpdate is only called
             * when smoothingLoop is set to LateUpdate. But to be
             * thorough check smoothingLoop anyway. */
            if (SmoothingLoop == SmoothingLoops.LateUpdate)
                MoveTowardsTarget();
        }

        /// <summary>
        /// Sets TeleportThresholdSquared value.
        /// </summary>
        private void SetTeleportThresholdSquared()
        {
            if (_teleportThreshold < 0f)
                _teleportThreshold = 0f;

            _teleportThresholdSquared = (_teleportThreshold * _teleportThreshold);
        }

        /// <summary>
        /// Checks if conditions are met to call CreateTransformGoalData.
        /// </summary>
        private void CheckCreateTransformGoalDataData()
        {
            /* If a client starts without being allowed to move the object a target data
            * must be set using current transform values so that the client may not move
             * the object. */
            /* If target data has not already been received.
             * If not server, since server is boss and shouldn't be blocked.
             * If client does not have authority or 
             * have authority but not client authoritative. */
            if (!_targetData.IsSet && !_isServer && (!_hasAuthority || (_hasAuthority && !_clientAuthoritative)))
                CreateTransformGoalData();
        }

        /// <summary>
        /// Creates target data according to where the transform is currently.
        /// </summary>
        protected void CreateTransformGoalData()
        {
            TransformData td = new TransformData();
            UpdateTransformData(
                ref td, 0,
                TargetTransform.GetPosition(UseLocalSpace), TargetTransform.GetRotation(UseLocalSpace), TargetTransform.GetScale(),
                new AttachedData(), false
                );

            //Create new target data without extrpaolation.
            ExtrapolationData extrapolation = new ExtrapolationData();
            MoveRateData moveRates = SetInstantMoveRates();
            UpdateGoalData(ref _targetData, ref td, ref moveRates, ref extrapolation);
        }
        #endregion

        #region CheckSendTo
        /// <summary>
        /// Checks if client needs to send data to server.
        /// </summary>
        private void CheckSendToServer(bool fixedUpdate, bool forced = false)
        {
            if (!_clientAuthoritative || !_isClient || !_hasAuthority)
                return;

            SyncProperties sp = SyncProperties.None;
            //Not forced, use normal time checks.
            if (!forced)
            {
                //Timed interval.
                if (_intervalType == IntervalTypes.Timed)
                {
                    if (Time.time < _nextClientSendTime)
                        return;
                }
                //Fixed interval.
                else
                {
                    if (!fixedUpdate)
                        return;
                }
            }
            //Forced send.
            else
            {
                sp |= SyncProperties.Forced;
                //Reset hold so data goes through no matter what.
                _holdToServer = false;
            }

            /* Teleport attached goal as if attached is valid this is what
            * will be chacked against. */
            SnapAttachedTargetToOwner();

            //True if transform has changed, and those changes are put into sync properties.
            bool changed = TransformChanged(ref sp, ref _clientTransformData);
            //Default for using reliable.
            bool useReliable = false;

            /* If settled was set then override useReliable to true. */
            if (SetSettled(changed, ref sp, ref _clientSettleSent))
            {
                useReliable = true;
                /* Unset hold because we don't want settled packets to
                * take any longer than they need to. */
                _holdToServer = false;
            }

            /* Nothing to send. If forced then proceed normally as the send must go through,
            * even if empty. This will ensure the point will be hit if a send
            * goes through immediately after. */
            if (!forced && sp == SyncProperties.None)
            {
                /* When server is sending to clients the server adds a small delay before
                 * checking to send again where when sending to server from client there
                 * is no delay between checks. This is because the server will likely
                 * manage many objects and those delays add a decent performance increase,
                 * while client will likely only handle a few objects at most. */
                _holdToServer = true;
                return;
            }

            //If using timed then reset next send time.
            if (_intervalType == IntervalTypes.Timed)
                _nextClientSendTime = Time.time + _synchronizeInterval;
            //If send should be hold then exit immediately after resetting timer, and unset hold.
            if (_holdToServer)
            {
                _holdToServer = false;
                return;
            }

            //Add additional sync properties.
            ApplySpecialProperties(ref sp, false);

            float moveRate = -1f;
            //If not settled and has previous sent data then update move rate.
            if (_clientTransformData.IsSet)
            {
                float dist = Vector3.Distance(_clientTransformData.Position, GetTransformPosition(_attachedValid));
                moveRate = dist / ReturnSyncInterval();
            }

            UpdateTransformData(ref _clientTransformData, sp, moveRate, forced);

            //Send to server.
            ArraySegment<byte> segment = CopyToBufferSegment(
                Serialization.SerializeTransformData(_clientTransformData, this),
                ref _toServerBuffer);

            FlexNetworkTransformManager.SendToServer(ref segment, useReliable);
        }

        /// <summary>
        /// Forces the current transform data to be sent.
        /// </summary>
        /// <param name="asServer">True to send as server. Being true would result data being sent to clients. False would try to send data to server from client; this requires client authority.</param>
        public void ForceSendTransform(bool asServer)
        {
            if (asServer && !base.isServer)
                return;
            if (!asServer && (!_clientAuthoritative || !base.hasAuthority))
                return;

            if (asServer)
                CheckSendToClients(false, true);
            else
                CheckSendToServer(false, true);
        }
        /// <summary>
        /// Checks if server needs to send data to clients.
        /// </summary>
        private void CheckSendToClients(bool fixedUpdate, bool forced = false)
        {
            if (!_isServer)
                return;

            SyncProperties sp = SyncProperties.None;
            //Not forced, use normal time checks.
            if (!forced)
            {
                //Timed interval.
                if (_intervalType == IntervalTypes.Timed)
                {
                    //Not enough time has passed to send.
                    if (Time.time < _nextServerSendTime)
                        return;
                }
                //Fixed interval.
                else
                {
                    if (!fixedUpdate)
                        return;
                }
            }
            //Forced send.
            else
            {
                sp |= SyncProperties.Forced;
                //Reset hold so data goes through no matter what.
                _holdToClients = false;
            }

            /* Teleport attached goal as if attached is valid this is what
             * will be chacked against. */
            SnapAttachedTargetToOwner();

            /* TargetData is generated when there is a goal to move towards.
             * While not set it's safe to assume the transform was snapped or is being controlled
             * as a client host, so data is up to date. So with that in mind,
             * if not set we can compare against current transform values.
             * 
             * However, if set, we must compare against the TargetData as it
             * will contain the most up to date values. This is because the transform may be
             * moving towards the target data, but not yet arrived. Therefor, using the transform
             * would always provide results that are behind. */
            bool usingTargetData = _targetData.IsSet;

            //True if transform has changed, and those changes are put into sync properties.
            bool changed = (usingTargetData) ?
                TransformDataChanged(ref sp, ref _serverTransformData, ref _targetData) :
                TransformChanged(ref sp, ref _serverTransformData); ;
            //Default for using reliable.
            bool useReliable = false;

            /* If settled was set then override useReliable to true. */
            if (SetSettled(changed, ref sp, ref _serverSettleSent))
            {
                useReliable = true;
                /* Unset hold because we don't want settled packets to
                 * take any longer than they need to. */
                _holdToClients = false;
            }

            /* Nothing to send. Normally the timer resets a portion to
             * save performance rather than checking every frame. However,
             * if forced then proceed normally as the send must go through,
             * even if empty. This will ensure the point will be hit if a send
             * goes through immediately after. */
            if (!forced && sp == SyncProperties.None)
            {
                /* If cannot send then slightly reset send time to improve performance. This will
                * result in data not being sent as quickly as it could be but the delay shouldn't be noticeable.
                * This currently is only done on server as server would be performing these checks per object
                * while client would only need to check on it's owned objects. */
                if (_intervalType == IntervalTypes.Timed)
                    _nextServerSendTime = Time.time + Mathf.Min(_interpolationFallbehind, _synchronizeInterval * 0.25f);

                _holdToClients = true;
                return;
            }
            //Data to send or is forced.
            else
            {
                //If using timed then reset send time.
                if (_intervalType == IntervalTypes.Timed)
                    _nextServerSendTime = Time.time + _synchronizeInterval;
                /* If send should be hold then exit immediately after resetting timer, and unset hold.
                 * Only hold if not a settled packet. */
                if (_holdToClients)
                {
                    _holdToClients = false;
                    return;
                }
            }

            //Add additional sync properties.
            ApplySpecialProperties(ref sp, false);
            //Not using target data, update from transform.
            if (!usingTargetData)
                UpdateTransformData(ref _serverTransformData, sp, -1f, forced);
            //Using targetData, just resend it.
            else
                UpdateTransformData(ref _serverTransformData, sp, ref _targetData, -1f, forced);
            //if using client auth and sending move rates then resend whatever client sent ^
            //otherwise calculate dist and send from server. //moverates
            ArraySegment<byte> segment = CopyToBufferSegment(
                Serialization.SerializeTransformData(_serverTransformData, this),
                ref _toClientsBuffer);

            //Send to clients.
            foreach (NetworkConnection item in base.netIdentity.observers.Values)
                FlexNetworkTransformManager.SendToObserver(item, ref segment, useReliable);
        }

        /// <summary>
        /// Copies a writers data to this FNT locally buffer then returns an array segment for the buffer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private ArraySegment<byte> CopyToBufferSegment(NetworkWriterPooled writer, ref byte[] buffer)
        {
            /* Mirrors network writer doesn't have a way to return the array whole so I must get
            * the arraysegment instead, then copy it to my buffer, then make a new array segment
            * to send to manager. */
            ArraySegment<byte> writerSegment = writer.ToArraySegment();
            //Ensure lenghth of buffer.
            if (buffer.Length < writerSegment.Count)
                Array.Resize(ref buffer, writerSegment.Count * 2);

            Buffer.BlockCopy(writerSegment.Array, writerSegment.Offset, buffer, 0, writerSegment.Count);
            return new ArraySegment<byte>(buffer, 0, writerSegment.Count);
        }

        /// <summary>
        /// Snaps the Attached MovingTarget to it's owner.
        /// </summary>
        private void SnapAttachedTargetToOwner()
        {
            //If owner of target then move target to this transforms position.
            if (_attachedValid && IsAttachedOwner())
            {
                Attached.MovingTarget.SetPosition(false, TargetTransform.GetPosition(false));
                Attached.MovingTarget.SetRotation(false, TargetTransform.GetRotation(false));
            }
        }

        /// <summary>
        /// Creates attached data using current Attached.
        /// </summary>
        /// <returns></returns>
        private AttachedData CreateAttachedData()
        {
            AttachedData result = new AttachedData();
            if (Attached.Identity == null)
                result.IsSet = false;
            else
                result.SetData(Attached.Identity.ReturnNetworkId(), Attached.AttachedTargetIndex);

            return result;
        }
        #endregion

        /// <summary>
        /// Checks if sequenceIds need to be reset.
        /// </summary>
        private void CheckResetSequenceIds()
        {
            if (_owner == _lastOwner)
                return;
            _lastOwner = _owner;

            LastClientSequenceId = 0;
        }

        /// <summary>
        /// Updates the attached cache and returns true if attached exist.
        /// </summary>
        private void UpdateAttached(AttachedData attached)
        {
            if (!attached.IsSet)
                UpdateAttached(0, -1);
            else
                UpdateAttached(attached.NetId, attached.AttachedTargetIndex);
        }

        /// <summary>
        /// Updates the attached cache and returns true if attached exist.
        /// </summary>
        private void UpdateAttached(uint netId, sbyte componentIndex)
        {
            NetworkIdentity currentAttachedIdentity = Attached.Identity;
            //If net id is 0 then attached cannot be looked up.
            if (netId == 0)
            {
                if (Attached.MovingTarget != null)
                    Destroy(Attached.MovingTarget.gameObject);

                Attached.Reset();
                _attachedValid = false;
            }
            else
            {
                /* True if current attached exist, and it's netId matches passed in Id. 
                * In other words, attached values have not changed so there is no reason
                * to update. */
                bool blockUpdate = (Attached.MovingTarget != null && Attached.AttachedTargetIndex == componentIndex && currentAttachedIdentity != null && currentAttachedIdentity.ReturnNetworkId() == netId);
                if (!blockUpdate)
                {
                    //Create Attached target if not present.
                    if (Attached.MovingTarget == null)
                    {
                        Attached.MovingTarget = new GameObject().transform;
                        NameAttachedTarget(Attached.MovingTarget);
                    }

                    if (Platforms.ReturnSpawned(_manager.NetworkManager).TryGetValue(netId, out NetworkIdentity ni))
                    {
                        Attached.Identity = ni;
                        Attached.AttachedTargetIndex = componentIndex;
                    }
                    else
                    {
                        Attached.Reset();
                    }

                    //Set new value of attached valid. Expected to be true but may not be if identity wasn't found.
                    _attachedValid = AttachedValid();
                    //Child target to new attached object.
                    Transform attachTarget = null;
                    if (_attachedValid)
                    {
                        //If using a component to attach to.
                        if (Attached.AttachedTargetIndex >= 0)
                        {
                            if (Attached.Identity.GetComponent<FlexAttachTargets>() is FlexAttachTargets foa)
                            {
                                GameObject go = foa.ReturnTarget(Attached.AttachedTargetIndex);
                                if (go != null)
                                    attachTarget = go.transform;
                            }
                            //Object attacher not found.
                            else
                            {
                                Debug.LogWarning("FlexAttachTargets was not found on identity " + Attached.Identity.gameObject + ".");
                            }
                        }
                        //Attaching to root.
                        else
                        {
                            attachTarget = Attached.Identity.transform;
                        }

                        //Set target to local space.
                        Attached.MovingTarget.SetParent(attachTarget);
                        /* Set attached to transform position so that it doesn't interfer
                         * with smoothing between space change. */
                        Attached.MovingTarget.position = TargetTransform.GetPosition(false);
                        Attached.MovingTarget.rotation = TargetTransform.GetRotation(false);
                    }
                }
            }
        }

        /// <summary>
        /// Names the AttachedTarget object relevant to it's purpose. EG: if for local player, or spectator. Only needed for debugging.
        /// </summary>
        /// <param name="go"></param>
        private void NameAttachedTarget(Transform t)
        {
#if UNITY_EDITOR
            t.name = IsAttachedOwner() ?
                "OwnerTarget " + gameObject.name :
                "SpectatorTarget " + gameObject.name;
#endif
        }

        /// <summary>
        /// Returns true if owner to attached.
        /// </summary>
        /// <returns></returns>
        private bool IsAttachedOwner()
        {
            /* Can be attached owner under the following conditions:
             * IsServer, ClientAuthoritative and no owner.
             * IsClient, ClientAuthoritative and is owner. */
            if (_clientAuthoritative)
            {
                //Server with no owner.
                if (_isServer && !_hasOwner)
                    return true;
                //Client and is owner.
                if (_isClient)
                    return _hasAuthority;
            }
            else
            {
                return _isServer;
            }

            //Fall through.
            return false;
        }
        /// <summary>
        /// Returns if Attached is valid.
        /// </summary>
        /// <returns></returns>
        private bool AttachedValid()
        {
            return (Attached.Identity != null && Attached.Identity.ReturnNetworkId() != 0);
        }

        /// <summary>
        /// Gets the position of the attached if one is used, otherwise uses transform values.
        /// </summary>
        private Vector3 GetTransformPosition(bool attachedSpace)
        {
            //Attached exist.
            if (attachedSpace)
                return Attached.MovingTarget.GetPosition(true);
            //No attached.
            else
                return TargetTransform.GetPosition(UseLocalSpace);
        }

        /// <summary>
        /// Gets the rotation of the attached if one is used, otherwise uses transform values.
        /// </summary>
        private Quaternion GetTransformRotation(bool attachedSpace)
        {
            //Attached exist.
            if (attachedSpace)
                return Attached.MovingTarget.GetRotation(true);
            //No attached.
            else
                return TargetTransform.GetRotation(UseLocalSpace);
        }

        /// <summary>
        /// Applies SyncProperties which are required based on settings.
        /// </summary>
        /// <param name="sp"></param>
        private void ApplySpecialProperties(ref SyncProperties sp, bool forceAll)
        {
            //Add in compression values for NetId.
            if (_netId <= 255)
                sp |= SyncProperties.Id1;
            else if (_netId <= 65535)
                sp |= SyncProperties.Id2;

            /* //FEATURE Attached is always sent when it exist.
             * In the future I'd like to only send if changed but this
             * won't be possible for UDP. */
            if (forceAll || _attachedValid)
                sp |= SyncProperties.Attached;

            //If to force all.
            if (forceAll)
            {
                sp |= ReturnConfiguredProperties();
            }
            //If has settled then must include all transform values to ensure a perfect match.
            else if (EnumContains.SyncPropertiesContains(sp, SyncProperties.Settled))
            {
                sp |= ReturnConfiguredProperties();
            }
        }

        /// <summary>
        /// Returns properties which are configured to be synchronized.
        /// </summary>
        /// <returns></returns>
        private SyncProperties ReturnConfiguredProperties()
        {
            SyncProperties sp = SyncProperties.None;

            if (_synchronizePosition == SynchronizeTypes.Normal)
                sp |= SyncProperties.Position;
            if (_synchronizeRotation == SynchronizeTypes.Normal)
                sp |= SyncProperties.Rotation;
            if (_synchronizeScale == SynchronizeTypes.Normal)
                sp |= SyncProperties.Scale;

            return sp;
        }

        /// <summary>
        /// Returns if was able to set as settled.
        /// </summary>
        /// <returns>True if settled was set.</returns>
        private bool SetSettled(bool transformChanged, ref SyncProperties sp, ref bool settleSent)
        {
            //If nothing has changed.
            if (!transformChanged)
            {
                //Settle has already been sent.
                if (settleSent)
                {
                    return false;
                }
                //Settle has not been sent yet.
                else
                {
                    bool idleMet = false;
                    /* If there is no owner then server is in control.
                     * Send idle quickly as possible. */
                    if (!_hasOwner)
                    {
                        idleMet = true;
                    }
                    /* If there is an owner wait a short amount before
                     * sending settled. Client packets
                     * won't arrive at a reliable interval so to prevent
                     * sending settled when a packet could just be a little
                     * behind. For this FNT will give a small delay to allow another packet
                     * before sending settled. */
                    else
                    {
                        /* If idle start hasn't yet been set
                         * then set it. */
                        if (_transformIdleStart == -1f)
                        {
                            _transformIdleStart = Time.time;
                        }
                        /* If transform already marked as idle set
                         * if Idle has been met. */
                        else
                        {
                            //If client authoritative give a little more time.
                            if (_clientAuthoritative)
                                idleMet = (Time.time - _transformIdleStart) >= _interpolationFallbehind * 0.75f;
                            /* If server auth but has ower then give less time.
                             * This assumes that the client send rate is being adjusted
                             * to keep 1:1 with server, as a true server auth system would. */
                            else
                                idleMet = (Time.time - _transformIdleStart) >= Mathf.Min(_interpolationFallbehind * 0.5f, ReturnSyncInterval());
                        }
                    }

                    if (idleMet)
                    {
                        settleSent = true;
                        sp |= SyncProperties.Settled;
                        return true;
                    }
                    //If not idle long enough, return unable to send.
                    else
                    {
                        return false;
                    }
                }
            }
            //Properties need to be synchronized.
            else
            {
                //Unset settled.
                settleSent = false;
                //Unset that transform has been idle.
                _transformIdleStart = -1f;
                return false;
            }
        }

        /// <summary>
        /// Returns SyncProperties which must be applied due to the transform changing.
        /// </summary>
        /// <returns></returns>
        private bool TransformChanged(ref SyncProperties sp, ref TransformData previouslySentData)
        {
            //Becomes true if changed.
            bool changed = false;

            //When data isn't set then each sync property is applied unconditionally.
            if (!previouslySentData.IsSet)
            {
                sp |= ReturnConfiguredProperties();
                changed = true;
            }
            //Previous data is set, see if anything has changed.
            else
            {
                //Position.
                if (_synchronizePosition == SynchronizeTypes.Normal)
                {
                    bool positionMatches = (_attachedValid) ? AttachedPositionMatches(ref previouslySentData) : TransformPositionMatches(ref previouslySentData);
                    if (!positionMatches)
                    {
                        changed = true;
                        sp |= SyncProperties.Position;
                    }
                }
                //Rotation.
                if (_synchronizeRotation == SynchronizeTypes.Normal)
                {
                    bool rotationMatches = (_attachedValid) ? AttachedRotationMatches(ref previouslySentData) : TransformRotationMatches(ref previouslySentData);
                    if (!rotationMatches)
                    {
                        changed = true;
                        sp |= SyncProperties.Rotation;
                    }
                }
                //Scale.
                if (_synchronizeScale == SynchronizeTypes.Normal)
                {
                    //Attached don't support scale, so if attached is valid then scale always matches.
                    bool scaleMatches = (_attachedValid) ? true : TransformScaleMatches(ref previouslySentData);
                    if (!scaleMatches)
                    {
                        changed = true;
                        sp |= SyncProperties.Scale;
                    }
                }
            }

            return changed;
        }
        /// <summary>
        /// Returns if any properties have changed between transformData and goalData.
        /// </summary>
        /// <param name="sp">SyncProperties to modify with changed properties.</param>
        /// <param name="transformData"></param>
        /// <param name="goalData">When specified data is compared against goalData. Otherwise, data is compared against the transform.</param>
        /// <returns></returns>
        private bool TransformDataChanged(ref SyncProperties sp, ref TransformData transformData, ref GoalData goalData)
        {
            //Becomes true if anything has changed.
            bool changed = false;

            //Cannot compare if either data is null.
            if (!transformData.IsSet || !goalData.IsSet)
            {
                sp |= ReturnConfiguredProperties();
                changed = true;
            }
            //Both datas exist.
            else
            {

                //Position.
                if (_synchronizePosition == SynchronizeTypes.Normal)
                {
                    if (!TransformDataPositionMatches(ref transformData, ref goalData))
                    {
                        changed = true;
                        sp |= SyncProperties.Position;
                    }
                }
                //Rotation.
                if (_synchronizeRotation == SynchronizeTypes.Normal)
                {
                    if (!TransformDataRotationMatches(ref transformData, ref goalData))
                    {
                        changed = true;
                        sp |= SyncProperties.Rotation;
                    }
                }
                //Scale.
                if (_synchronizeScale == SynchronizeTypes.Normal)
                {
                    if (!TransformDataScaleMatches(ref transformData, ref goalData))
                    {
                        changed = true;
                        sp |= SyncProperties.Scale;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// Snaps transform to attached Target if not attached owner.
        /// </summary>
        private void SnapToAttached()
        {
            /* Transforms are snapped to attached, and the attached
             * target is moved smoothly. */
            if (!_attachedValid)
                return;
            if (IsAttachedOwner())
                return;

            if (_synchronizePosition != SynchronizeTypes.NoSynchronization)
                TargetTransform.SetPosition(false, Attached.MovingTarget.position);
            if (_synchronizeRotation == SynchronizeTypes.Normal)
                TargetTransform.SetRotation(false, Attached.MovingTarget.rotation);
            //Attached support doesn't require scale syncing.
        }

        /// <summary>
        /// Moves towards Target data.
        /// </summary>
        private void MoveTowardsTarget()
        {
            //No data to check against.
            if (!_targetData.IsSet)
                return;

            /* Client authority but there is no owner.
             * Can happen when client authority is ticked but
            * the server takes away authority. */
            if (_isServer && _clientAuthoritative && !_hasOwner && _targetData.IsSet)
            {
                /* Remove sync data so server no longer tries to sync up to last data received from client.
                 * Object may be moved around on server at this point. */
                UnsetTargetDatas();
                return;
            }

            //Client authority, don't need to synchronize with self.
            if (_hasAuthority && _clientAuthoritative)
                return;
            //Not client authority but also not synchronize to owner.
            if (_hasAuthority && !_clientAuthoritative && !_synchronizeToOwner)
                return;

            bool attachedValid = _attachedValid;
            bool extrapolate = (_targetData.Extrapolation.IsSet && _targetData.Extrapolation.Remaining > 0f);
            //Not enough time has passed to be at goal.
            if (!extrapolate && (_moveTowardsEndTime != -1f && Time.time > _moveTowardsEndTime))
            {
                /* The time check on moveTowardsEndTime is missing one move frame
                 * because the moves are performed after the time increase. This means
                 * the transform will always be 1 frame behind. Typically this goes
                 * unnoticed unless the frames are really low and the transform is moving
                 * really fast. As a work around if moveTowardsEndTime isn't already 0f
                 * then snap it into place and set moveTowardsEndTime at 0f. In the future
                 * a more appropriate means will be used to address this issue. //Fix */
                if (_moveTowardsEndTime != 0f)
                {
                    _moveTowardsEndTime = 0f;
                }
                else
                {
                    /* If cached then update the target data immediately
                     * with the cached and run the move towards again. */
                    if (_cachedTransformData.CachedCount > 0)
                    {
                        _cachedTransformData.ResetCachedCount();
                        if (_cachedTransformData.FromServer)
                            ServerDataReceived(ref _cachedTransformData.Data);
                        else
                            ClientDataReceived(ref _cachedTransformData.Data);

                        MoveTowardsTarget();
                    }
                    return;
                }
            }

            //If was set to snap then reset.
            if (_moveTowardsEndTime == -1f)
                _moveTowardsEndTime = 0f;

            /* Move attached target towards goal. */
            if (attachedValid)
                TryMoveAttachedTarget();

            /* Only move using localspace if configured to local space
             * and if not using a attached. Attached offsets arrive in local space
             * but the transform must move in world space to the attached
             * target. */
            bool useLocalSpace = (UseLocalSpace && !attachedValid);

            //Position
            if (_synchronizePosition != SynchronizeTypes.NoSynchronization)
            {
                Vector3 positionGoal = (attachedValid) ? Attached.MovingTarget.position : _targetData.Goal.Position;

                /* If attached is valid then use instant move rates. This
                 * is because the attached target is already smooth so
                 * we can snap the transform. */
                float moveRate = (attachedValid) ? -1f : _targetData.MoveRates.Position;
                //Instantly.
                if (moveRate == -1f)
                {
                    TargetTransform.SetPosition(useLocalSpace, positionGoal);
                }
                //Over time.
                else
                {
                    //If to extrapolate then overwrite position goal with extrapolation.
                    if (extrapolate)
                        positionGoal = _targetData.Extrapolation.Position;

                    TargetTransform.SetPosition(useLocalSpace,
                        Vector3.MoveTowards(TargetTransform.GetPosition(useLocalSpace), positionGoal, moveRate * Time.deltaTime)
                        );
                }
            }
            //Rotation.
            if (_synchronizeRotation == SynchronizeTypes.Normal)
            {
                Quaternion rotationGoal = (attachedValid) ? Attached.MovingTarget.rotation : _targetData.Goal.Rotation;
                /* If attached is valid then use instant move rates. This
                * is because the attached target is already smooth so
                * we can snap the transform. */
                float moveRate = (attachedValid) ? -1f : _targetData.MoveRates.Rotation;
                //Instantly.
                if (moveRate == -1f)
                {
                    TargetTransform.SetRotation(useLocalSpace, rotationGoal);
                }
                //Over time.
                else
                {
                    TargetTransform.SetRotation(UseLocalSpace,
                        Quaternion.RotateTowards(TargetTransform.GetRotation(useLocalSpace), rotationGoal, moveRate * Time.deltaTime)
                        );
                }
            }
            //Scale.
            if (_synchronizeScale != SynchronizeTypes.NoSynchronization)
            {
                Vector3 scaleGoal = _targetData.Goal.Scale;
                //Instantly.
                if (_targetData.MoveRates.Scale == -1f)
                {
                    TargetTransform.SetScale(scaleGoal);
                }
                //Over time.
                else
                {
                    TargetTransform.SetScale(
                        Vector3.MoveTowards(TargetTransform.GetScale(), scaleGoal, _targetData.MoveRates.Scale * Time.deltaTime)
                        );
                }
            }

            //Remove from remaining extrapolation time.
            if (extrapolate)
            {
                _targetData.Extrapolation.Remaining -= Time.deltaTime;
                /* If no more extrapolation is available then
                 * modify move end time to move back to goal. */
                if (_targetData.Extrapolation.Remaining <= 0f)
                {
                    float distance = Vector3.Distance(TargetTransform.GetPosition(useLocalSpace), _targetData.Goal.Position);
                    if (distance > 0f)
                    {
                        float timeRequired = (distance / _targetData.MoveRates.Position);
                        _moveTowardsEndTime = Time.time + timeRequired;
                    }
                }
            }
        }

        /// <summary>
        /// Tries to move the attached target to it's goal position. This method assumes a attached is valid.
        /// </summary>
        private void TryMoveAttachedTarget()
        {
            //Always use local space when moving the attached.
            bool useLocalSpace = true;
            //Position
            if (_synchronizePosition != SynchronizeTypes.NoSynchronization)
            {
                //Instant.
                if (_targetData.MoveRates.Position == -1f)
                {
                    Attached.MovingTarget.SetPosition(useLocalSpace, _targetData.Goal.Position);
                }
                //Over time.
                else
                {
                    //Move target to goal.
                    Attached.MovingTarget.SetPosition(useLocalSpace,
                        Vector3.MoveTowards(Attached.MovingTarget.GetPosition(useLocalSpace), _targetData.Goal.Position, _targetData.MoveRates.Position * Time.deltaTime)
                        );
                }
            }
            //Rotation.
            if (_synchronizeRotation == SynchronizeTypes.Normal)
            {
                //Instant.
                if (_targetData.MoveRates.Rotation == -1f)
                {
                    Attached.MovingTarget.SetRotation(useLocalSpace, _targetData.Goal.Rotation);
                }
                //Over time.
                else
                {
                    //Move target to goal.
                    Attached.MovingTarget.SetRotation(useLocalSpace,
                        Quaternion.RotateTowards(Attached.MovingTarget.GetRotation(useLocalSpace), _targetData.Goal.Rotation, _targetData.MoveRates.Rotation * Time.deltaTime)
                        );
                }
            }

            //Attached target ignores scale.
        }

        /// <summary>
        /// Returns true if the passed in axes contains all axes.
        /// </summary>
        /// <param name="axes"></param>
        /// <returns></returns>
        private bool SnapAll(Vector3Axes axes)
        {
            return (axes == (Vector3Axes.X | Vector3Axes.Y | Vector3Axes.Z));
        }

        #region Position Matches.
        /// <summary>
        /// Returns if this Attached.MovingTarget position matches transformData.
        /// </summary>
        /// <param name="transformData"></param>
        /// <returns></returns>
        private bool AttachedPositionMatches(ref TransformData transformData)
        {
            if (!transformData.IsSet)
                return false;

            return Attached.MovingTarget.localPosition == transformData.Position;
        }

        /// <summary>
        /// Returns if TargetTransform position matches data.
        /// </summary>
        /// <param name="transformData"></param>
        /// <returns></returns>
        private bool TransformPositionMatches(ref TransformData transformData)
        {
            if (!transformData.IsSet)
                return false;

            return TargetTransform.GetPosition(UseLocalSpace) == transformData.Position;
        }
        /// <summary>
        /// Returns if transformData position matches goalData.
        /// </summary>
        /// <param name="transformData"></param>
        /// <returns></returns> 
        private bool TransformDataPositionMatches(ref TransformData transformData, ref GoalData goalData)
        {
            if (!transformData.IsSet || !goalData.IsSet)
                return false;

            return goalData.Goal.Position == transformData.Position;
        }
        #endregion

        #region Rotation Matches.
        /// <summary>
        /// Returns if Attached.MovingTarget rotation matches transformData.
        /// </summary>
        /// <param name="transformData"></param>
        /// <returns></returns>
        private bool AttachedRotationMatches(ref TransformData transformData)
        {
            if (!transformData.IsSet)
                return false;

            return Attached.MovingTarget.localRotation == transformData.Rotation;
        }
        /// <summary>
        /// Returns if TargetTransform rotation matches transformData.
        /// </summary>
        /// <param name="transformData"></param>
        /// <returns></returns>
        private bool TransformRotationMatches(ref TransformData transformData)
        {
            if (!transformData.IsSet)
                return false;

            return TargetTransform.GetRotation(UseLocalSpace) == transformData.Rotation;
        }
        /// <summary>
        /// Returns if transformData rotation matches goalData.
        /// </summary>
        /// <param name="transformData"></param>
        /// <returns></returns>
        private bool TransformDataRotationMatches(ref TransformData transformData, ref GoalData goalData)
        {
            if (!transformData.IsSet || !goalData.IsSet)
                return false;

            return goalData.Goal.Rotation == transformData.Rotation;
        }
        #endregion

        #region Scale Matches.
        /// <summary>
        /// Returns if TargetTransform scale matches transformData.
        /// </summary>
        /// <param name="transformData"></param>
        /// <returns></returns>
        private bool TransformScaleMatches(ref TransformData transformData)
        {
            if (!transformData.IsSet)
                return false;

            return TargetTransform.GetScale() == transformData.Scale;
        }
        /// <summary>
        /// Returns if transformData scale matches goalData.
        /// </summary>
        /// <param name="transformData"></param>
        /// <returns></returns>
        private bool TransformDataScaleMatches(ref TransformData transformData, ref GoalData goalData)
        {
            if (!transformData.IsSet || !goalData.IsSet)
                return false;

            return goalData.Goal.Scale == transformData.Scale;
        }
        #endregion

        #region Data Received.
        /// <summary>
        /// Called on clients when server data is received.
        /// </summary>
        /// <param name="data"></param>
        [Client]
        public void ServerDataReceived(ref TransformData data)
        {
            //If client host exit method.
            if (_isServer)
                return;

            //If owner of object.
            if (_hasAuthority)
            {
                //Client authoritative, already in sync.
                if (_clientAuthoritative)
                    return;
                //Not client authoritative, but also not sync to owner.
                if (!_clientAuthoritative && !_synchronizeToOwner)
                    return;
            }

            /* If last data was forced and move isnt complete yet then
             * cache the received data. This is so the forced data will
             * be completed before running future data. */
            if (!data.Forced && _targetData.Goal.Forced && Time.time < _moveTowardsEndTime)
            {
                _cachedTransformData.CacheData(data, true);
                return;
            }

            //Fill in missing data for properties that werent included in send.
            FillMissingData(ref data, ref _targetData);
            /* If attached is valid then set the target transform
            * to values received from the client. */
            UpdateAttached(data.Attached);
            ExtrapolationData extrapolation = new ExtrapolationData();
            MoveRateData moveRates = new MoveRateData();

            //If teleporting set move rates to be instantaneous.
            if (ShouldTeleport(ref data, ref _targetData))
            {
                Snap(ref data, true);
            }
            //If not teleporting calculate extrapolation and move rates.
            else
            {
                extrapolation = SetExtrapolation(ref data, ref _targetData);
                moveRates = SetMoveRates(ref data);
                Snap(ref data, false);
                //Update move towards end time.
                _moveTowardsEndTime = Time.time + ReturnSyncInterval() + _interpolationFallbehind;
            }

            //Target data has to be updated regardless so that FillMissingData will work properly.
            UpdateGoalData(ref _targetData, ref data, ref moveRates, ref extrapolation);

            //If deserialized data exist then unset it since we have new data.
            if (_deserializedTransformData.IsSet)
                _deserializedTransformData.IsSet = false;
        }


        /// <summary>
        /// Called on clients when server data is received.
        /// </summary>
        /// <param name="data"></param>
        [Server]
        public void ClientDataReceived(ref TransformData data)
        {
            //Sent to self or no authoritative client.
            if (_hasAuthority || !_hasOwner)
                return;

            /* If last data was forced and move isnt complete yet then
            * cache the received data. This is so the forced data will
            * be completed before running future data. */
            if (!data.Forced && _targetData.Goal.Forced && Time.time < _moveTowardsEndTime)
            {
                _cachedTransformData.CacheData(data, false);
                return;
            }

            //Fill in missing data for properties that werent included in send.
            FillMissingData(ref data, ref _targetData);
            /* If attached is valid then set the target transform
            * to values received from the client. */
            UpdateAttached(data.Attached);

            //Only build for event if there are listeners.
            if (OnClientDataReceived != null)
            {
                ReceivedClientData rcd = new ReceivedClientData(ReceivedClientData.DataTypes.Interval, UseLocalSpace, ref data);
                OnClientDataReceived.Invoke(rcd);

                //If data was nullified then do nothing.
                if (!rcd.Data.IsSet || !data.IsSet)
                    return;
            }

            ExtrapolationData extrapolation;
            MoveRateData moveRates;
            /* If server only then snap to target position. 
             * Should I ever add extrapolation on server only
             * then I would need to move smoothly instead and
             * perform extrapolation
             * calculations. */
            if (_isServerOnly)
            {
                Snap(ref data, true);
                /* If there is an attached then target data must
                 * be set to keep object on attached. Also, if data is
                 * forced it must be set then immediately send to clients. */
                if (_attachedValid || data.Forced)
                {
                    extrapolation = new ExtrapolationData();
                    moveRates = SetInstantMoveRates();
                    UpdateGoalData(ref _targetData, ref data, ref moveRates, ref extrapolation);
                    if (data.Forced)
                        ForceSendTransform(true);
                }
                //If not attached nor forced there's no need for target data.
                else
                {
                    UnsetTargetDatas();
                }
            }
            /* If not server only, so if client host, then set data
             * normally for smoothing. */
            else
            {
                //If teleporting set move rates to be instantaneous.
                if (ShouldTeleport(ref data, ref _targetData))
                {
                    Snap(ref data, true);
                    extrapolation = new ExtrapolationData();
                    moveRates = SetInstantMoveRates();
                    //Update targetData so it can be referenced as previous/current data later if needed.
                    UpdateGoalData(ref _targetData, ref data, ref moveRates, ref extrapolation);
                }
                //If not teleporting calculate extrapolation and move rates.
                else
                {
                    extrapolation = SetExtrapolation(ref data, ref _targetData);
                    moveRates = SetMoveRates(ref data);
                    Snap(ref data, false);
                    UpdateGoalData(ref _targetData, ref data, ref moveRates, ref extrapolation);
                    //Update move towards end time. Only needs to be done when not teleporting.
                    _moveTowardsEndTime = Time.time + ReturnSyncInterval() + _interpolationFallbehind;
                }
            }


        }
        #endregion

        #region Misc.
        /// <summary>
        /// Returns if the TargetTransform is a child object.
        /// </summary>
        /// <returns></returns>
        private bool TargetTransformIsChild()
        {
            return (transform.parent != null || TargetTransform != transform);
        }

        /// <summary>
        /// Updates a TransformData using transform.
        /// </summary>
        /// <param name="data"></param>
        private void UpdateTransformData(ref TransformData data, SyncProperties sp, float moveRate, bool forced)
        {
            Vector3 position = GetTransformPosition(_attachedValid);
            Quaternion rotation = GetTransformRotation(_attachedValid);
            Vector3 scale = TargetTransform.GetScale();

            AttachedData ad = CreateAttachedData();
            UpdateTransformData(
                ref data, sp,
                position, rotation, scale,
                ad, forced
                );
        }
        /// <summary>
        /// Updates a transformData using goalData.
        /// </summary>
        /// <param name="transformData"></param>
        private void UpdateTransformData(ref TransformData transformData, SyncProperties sp, ref GoalData goalData, float moveRate, bool forced)
        {
            UpdateTransformData(
                ref transformData, sp,
                goalData.Goal.Position, goalData.Goal.Rotation, goalData.Goal.Scale,
               goalData.Goal.Attached, forced
               );
        }

        /// <summary>
        /// Updates a transformData with passed in values.
        /// </summary>
        /// <param name="transformData"></param>
        /// <param name="sp"></param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="scale"></param>
        private void UpdateTransformData(ref TransformData transformData, SyncProperties sp, Vector3 position, Quaternion rotation, Vector3 scale, AttachedData attached, bool forced)
        {
            //Mirror stores the component index as an int but they serialize it as a byte.
            transformData.UpdateValues((byte)sp, _netId, CachedComponentIndex,
                position, rotation, scale,
                attached, forced
                );
        }

        /// <summary>
        /// Updates a goalData with transformData.
        /// </summary>
        private void UpdateGoalData(ref GoalData goalData, ref TransformData transformData, ref MoveRateData moveRates, ref ExtrapolationData extrapolationData)
        {
            goalData.UpdateValues(transformData, moveRates, extrapolationData);
        }

        /// <summary>
        /// Returns synchronization interval used.
        /// </summary>
        /// <returns></returns>
        private float ReturnSyncInterval()
        {
            return (_intervalType == IntervalTypes.FixedUpdate) ? Time.fixedDeltaTime : _synchronizeInterval;
        }

        /// <summary>
        /// Returns if the transform should teleport.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ShouldTeleport(ref TransformData data, ref GoalData targetData)
        {
            /* No previous target data to go off from.
             * This may be initial data, so teleport will be true
             * to snap to data rather than moving smoothly. */
            if (!targetData.IsSet)
                return true;
            if (_teleportThresholdSquared <= 0f)
                return false;

            Vector3 position = GetTransformPosition(_attachedValid);
            float dist = Vectors.FastSqrMagnitude(position - data.Position);
            return dist >= _teleportThresholdSquared;
        }


        /// <summary>
        /// Sets MoveRates to move instantly.
        /// </summary>
        /// <returns></returns>
        private MoveRateData SetInstantMoveRates()
        {
            MoveRateData moveRates = new MoveRateData();
            moveRates.Position = -1f;
            moveRates.Rotation = -1f;
            moveRates.Scale = -1f;

            return moveRates;
        }

        /// <summary>
        /// Sets MoveRates based on data, transform position, and synchronization interval.
        /// </summary>
        /// <param name="data"></param>
        private MoveRateData SetMoveRates(ref TransformData data)
        {
            MoveRateData moveRates = new MoveRateData();
            /* If there is cached data then use how many datas were cached as a multiplier.
             * Otherwuse use the expected sync interval. This is so that if more than one data was cached,
             * let's say 2 were, then we know the object is expected to move over two sync intervals. */
            float past = (_cachedTransformData.CachedCount > 0) ?
                (ReturnSyncInterval() * _cachedTransformData.CachedCount) + _interpolationFallbehind :
                ReturnSyncInterval() + _interpolationFallbehind;

            float distance;
            /* Position. */
            Vector3 position = GetTransformPosition(_attachedValid);
            distance = Vector3.Distance(position, data.Position);
            moveRates.Position = distance / past;
            /* Rotation. */
            Quaternion rotation = GetTransformRotation(_attachedValid);
            distance = Quaternion.Angle(rotation, data.Rotation);
            moveRates.Rotation = distance / past;
            /* Scale. */
            distance = Vector3.Distance(TargetTransform.GetScale(), data.Scale);
            moveRates.Scale = distance / past;

            return moveRates;
        }


        /// <summary>
        /// Sets extrapolation by comparing transformData against previousGoalData.
        /// </summary>
        private ExtrapolationData SetExtrapolation(ref TransformData transformData, ref GoalData previousGoalData)
        {
            ExtrapolationData extrapolation = new ExtrapolationData();
            //Will become true if set at the end.
            extrapolation.IsSet = false;

            //Don't try to extrapolate set points.
            if (transformData.Forced)
                return extrapolation;
            /* If attached Id changed. 
             * Cannot extrapolate when attached Ids change because
             * the space used is changed. */
            if (previousGoalData.IsSet && _attachedValid && !AttachedData.Matches(previousGoalData.Goal.Attached, transformData.Attached))
                return extrapolation;
            //No extrapolation.
            if (_extrapolationSpan == 0f || !previousGoalData.IsSet)
                return extrapolation;
            //Settled packet.
            if (EnumContains.SyncPropertiesContains((SyncProperties)transformData.SyncProperties, SyncProperties.Settled))
                return extrapolation;

            Vector3 positionDirection = (transformData.Position - previousGoalData.Goal.Position);
            Vector3 position = GetTransformPosition(_attachedValid);
            Vector3 goalDirectionNormalzied = (transformData.Position - position).normalized;
            /* If direction to goal is different from extrapolation direction
             * then do not extrapolate. This can occur when the extrapolation
             * overshoots. If the extrapolation was to continue like this then
             * it would likely overshoot more and more, becoming extremely
             * offset. */
            if (goalDirectionNormalzied != positionDirection.normalized)
                return extrapolation;

            float multiplier = _extrapolationSpan / ReturnSyncInterval();
            extrapolation.UpdateValues(transformData.Position + (positionDirection * multiplier),
                _extrapolationSpan + ReturnSyncInterval(), true);

            return extrapolation;
        }

        /// <summary>
        /// Snaps transforms, including attahed transforms, when applicable.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="snapAll"></param>
        private void Snap(ref TransformData data, bool snapAll)
        {
            //Snap attached, then snap transform to attached.
            if (_attachedValid)
            {
                SnapTransform(Attached.MovingTarget, ref data, snapAll, true);
                SnapTransform(TargetTransform, (SyncProperties)data.SyncProperties, Attached.MovingTarget.position, Attached.MovingTarget.rotation, data.Scale, snapAll, false);
            }
            //Just snap transform to data.
            else
            {
                SnapTransform(TargetTransform, ref data, snapAll, _attachedValid);
            }
        }
        /// <summary>
        /// Snaps the transform to data where snapping is applicable.
        /// </summary>
        /// /// <param name="t"></param>
        /// <param name="data"></param>
        /// <param name="snapAll"></param>
        private void SnapTransform(Transform t, ref TransformData data, bool snapAll, bool snappingAttached)
        {
            SnapTransform(t, (SyncProperties)data.SyncProperties, data.Position, data.Rotation, data.Scale, snapAll, snappingAttached);
        }
        /// <summary>
        /// Snaps the transform to data where snapping is applicable.
        /// </summary>
        /// <param name="targetData">Data to snap from.</param>
        private void SnapTransform(Transform t, SyncProperties sp, Vector3 position, Quaternion rotation, Vector3 scale, bool snapAll, bool snappingAttached)
        {
            if (t == null)
                return;

            //If using local space or attached space is specified.
            bool useLocalSpace = (UseLocalSpace || snappingAttached);
            if (snapAll || EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
            {
                //If to snap all.
                if (snapAll || SnapAll(_snapPosition))
                {
                    t.SetPosition(useLocalSpace, position);
                }
                //Snap some or none.
                else
                {
                    //Snap X.
                    if (EnumContains.Vector3AxesContains(_snapPosition, Vector3Axes.X))
                        t.SetPosition(useLocalSpace, new Vector3(position.x, t.GetPosition(useLocalSpace).y, t.GetPosition(useLocalSpace).z));
                    //Snap Y.
                    if (EnumContains.Vector3AxesContains(_snapPosition, Vector3Axes.Y))
                        t.SetPosition(useLocalSpace, new Vector3(t.GetPosition(useLocalSpace).x, position.y, t.GetPosition(useLocalSpace).z));
                    //Snap Z.
                    if (EnumContains.Vector3AxesContains(_snapPosition, Vector3Axes.Z))
                        t.SetPosition(useLocalSpace, new Vector3(t.GetPosition(useLocalSpace).x, t.GetPosition(useLocalSpace).y, position.z));
                }
            }

            /* Rotation. */
            if (snapAll || EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
            {
                //If to snap all.
                if (snapAll || SnapAll(_snapRotation))
                {
                    t.SetRotation(useLocalSpace, rotation);
                }
                //Snap some or none.
                else
                {
                    /* Only perform snap checks if snapping at least one
                     * to avoid extra cost of calculations. */
                    if ((int)_snapRotation != 0)
                    {
                        /* Convert to eulers since that is what is shown
                         * in the inspector. */
                        Vector3 startEuler = t.GetRotation(UseLocalSpace).eulerAngles;
                        Vector3 targetEuler = rotation.eulerAngles;
                        //Snap X.
                        if (EnumContains.Vector3AxesContains(_snapRotation, Vector3Axes.X))
                            startEuler.x = targetEuler.x;
                        //Snap Y.
                        if (EnumContains.Vector3AxesContains(_snapRotation, Vector3Axes.Y))
                            startEuler.y = targetEuler.y;
                        //Snap Z.
                        if (EnumContains.Vector3AxesContains(_snapRotation, Vector3Axes.Z))
                            startEuler.z = targetEuler.z;

                        //Rebuild into quaternion.
                        t.SetRotation(useLocalSpace, Quaternion.Euler(startEuler));
                    }
                }
            }

            /* Scale.
             * Only snap scale if not Attached Target
             * as Attached Target doesn't need scale. */
            if (t != Attached.MovingTarget && snapAll || EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
            {
                //If to snap all.
                if (snapAll || SnapAll(_snapScale))
                {
                    t.SetScale(scale);
                }
                //Snap some or none.
                else
                {
                    //Snap X.
                    if (EnumContains.Vector3AxesContains(_snapScale, Vector3Axes.X))
                        t.SetScale(new Vector3(scale.x, t.GetScale().y, t.GetScale().z));
                    //Snap Y.
                    if (EnumContains.Vector3AxesContains(_snapScale, Vector3Axes.Y))
                        t.SetPosition(UseLocalSpace, new Vector3(t.GetScale().x, scale.y, t.GetScale().z));
                    //Snap Z.
                    if (EnumContains.Vector3AxesContains(_snapScale, Vector3Axes.Z))
                        t.SetPosition(UseLocalSpace, new Vector3(t.GetScale().x, t.GetScale().y, scale.z));
                }
            }
        }

        /// <summary>
        /// Unsets current and cached targetDatas.
        /// </summary>
        private void UnsetTargetDatas()
        {
            _targetData.IsSet = false;
            _cachedTransformData.ResetCachedCount();
        }
        /// <summary>
        /// Fills in missing data within transformData by using a previous or current goalData.
        /// For example, if rotation was not included in transformData then the last datas rotation will be used, or transforms current rotation if there is no previous packet.
        /// </summary>
        private void FillMissingData(ref TransformData transformData, ref GoalData goalData)
        {
            SyncProperties sp = (SyncProperties)transformData.SyncProperties;
            /* Begin by setting goal data using what has been serialized
            * via the writer. */
            //Position wasn't included.
            if (!EnumContains.SyncPropertiesContains(sp, SyncProperties.Position))
            {
                if (!goalData.IsSet)
                    transformData.Position = TargetTransform.GetPosition(UseLocalSpace);
                else
                    transformData.Position = goalData.Goal.Position;
            }
            //Rotation wasn't included.
            if (!EnumContains.SyncPropertiesContains(sp, SyncProperties.Rotation))
            {
                if (!goalData.IsSet)
                    transformData.Rotation = TargetTransform.GetRotation(UseLocalSpace);
                else
                    transformData.Rotation = goalData.Goal.Rotation;
            }
            //Scale wasn't included.
            if (!EnumContains.SyncPropertiesContains(sp, SyncProperties.Scale))
            {
                if (!goalData.IsSet)
                    transformData.Scale = TargetTransform.GetScale();
                else
                    transformData.Scale = goalData.Goal.Scale;
            }

            /* Attached data will always be included every packet
             * if an attached is present. */
        }
        #endregion    

        #region Editor.
        private void OnValidate()
        {
            SetTeleportThresholdSquared();
        }
        #endregion
    }
}

