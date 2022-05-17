using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{


    /// <summary>
    /// Attached object data.
    /// </summary>
    public class AttachedGoalData
    {
        /// <summary>
        /// Attached object's Networkidentity.
        /// </summary>
        public NetworkIdentity Identity = null;
        /// <summary>
        /// Index within FlexAttachTargets to use. Will be -1 if using root on Identity.
        /// </summary>
        public sbyte AttachedTargetIndex = -1;
        /// <summary>
        /// For spectators this is the transform to move towards. For owner this is where their transform is in localspace to the attached object.
        /// </summary>
        public Transform MovingTarget = null;

        /// <summary>
        /// Resets to defaults.
        /// </summary>
        public void Reset()
        {
            Identity = null;
            AttachedTargetIndex = -1;
            MovingTarget = null;
        }
    }

}