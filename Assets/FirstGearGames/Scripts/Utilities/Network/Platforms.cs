using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Mirror;


namespace FirstGearGames.Utilities.Networks
{

    public static class Platforms
    {
        /// <summary>
        /// Returns the NetworkId for a NetworkIdentity.
        /// </summary>
        /// <param name="ni"></param>
        /// <returns></returns>
        public static uint ReturnNetworkId(this NetworkIdentity ni)
        {
            return ni.netId;
        }
        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nm"></param>
        /// <param name="msg"></param>
        /// <param name="channel"></param>
        public static void ClientSend<T>(NetworkManager nm, T msg, int channel) where T : struct, NetworkMessage
        {
            NetworkClient.Send(msg, channel);
        }

        /// <summary>
        /// Sends a message to all clients.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nm"></param>
        /// <param name="msg"></param>
        /// <param name="channel"></param>
        public static void ServerSendToAll<T>(NetworkManager nm, T msg, int channel) where T : struct, NetworkMessage
        {
            NetworkServer.SendToAll(msg, channel, true);
        }

        /// <summary>
        /// Returns true if object has an owner.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnHasOwner(this NetworkBehaviour nb)
        {
            return (nb.connectionToClient != null);
        }

        /// <summary>
        /// Returns the networkId for the networkIdentity.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReturnNetId(this NetworkBehaviour nb)
        {
            return nb.netIdentity.netId;
        }
        /// <summary>
        /// Returns current owner of this object.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkConnection ReturnOwner(this NetworkBehaviour nb)
        {
            return nb.connectionToClient;
        }

        /// <summary>
        /// Returns if current client has authority.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnHasAuthority(this NetworkBehaviour nb)
        {
            return nb.hasAuthority;
        }
        /// <summary>
        /// Returns if is server.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnIsServer(this NetworkBehaviour nb)
        {
            return nb.isServer;
        }
        /// <summary>
        /// Returns if is server only.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnIsServerOnly(this NetworkBehaviour nb)
        {
            return nb.isServerOnly;
        }
        /// <summary>
        /// Returns if is client.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnIsClient(this NetworkBehaviour nb)
        {
            return nb.isClient;
        }

        /// <summary>
        /// Returns if client is active.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnServerActive(NetworkManager nm)
        {
            return NetworkServer.active;
        }
        /// <summary>
        /// Returns if client is active.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReturnClientActive(NetworkManager nm)
        {
            return NetworkClient.active;
        }

        /// <summary>
        /// Returns if a connection is ready.
        /// </summary>
        /// <param name="nc"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReady(this NetworkConnection nc)
        {
            return nc.isReady;
        }

        /// <summary>
        /// Returns currently spawned NetworkIdentities.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<uint, NetworkIdentity> ReturnSpawned(NetworkManager nm)
        {
            return NetworkIdentity.spawned;
        }

        /// <summary>
        /// Sets MTU values.
        /// </summary>
        /// <param name="reliable"></param>
        /// <param name="unreliable"></param>
        /// <returns></returns>
        public static void SetMTU(ref int reliable, ref int unreliable, int maxPacketSize)
        {
            if (Transport.activeTransport != null)
            {
                reliable = Mathf.Min(maxPacketSize, Transport.activeTransport.GetMaxPacketSize(0));
                unreliable = Mathf.Min(maxPacketSize, Transport.activeTransport.GetMaxPacketSize(1));
            }

            //If packet sizes are not calculated then use max.
            if (reliable == -1)
                reliable = maxPacketSize;
            if (unreliable == -1)
                unreliable = maxPacketSize;
        }  

    }

}