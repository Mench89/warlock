using Mirror;
using System.Runtime.CompilerServices;


namespace FirstGearGames.Utilities.Networks
{


    public static class Lookups
    {
        /// <summary>
        /// Returns the NetworkBehaviour for the specified NetworkIdentity and component index.
        /// </summary>
        /// <param name="componentIndex"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NetworkBehaviour ReturnNetworkBehaviour(NetworkIdentity netIdentity, byte componentIndex)
        {
            if (netIdentity == null)
                return null;
            /* Networkbehaviours within the collection are the same order as compenent indexes.
            * I can save several iterations by simply grabbing the index from the networkbehaviours collection rather than iterating
            * it. */
            //A network behaviour was removed or added at runtime, component counts don't match up.
            if (componentIndex >= netIdentity.NetworkBehaviours.Length)
                return null;

            return netIdentity.NetworkBehaviours[componentIndex];
        }
    }


}
