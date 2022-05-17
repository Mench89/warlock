using Mirror;
using System;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms
{

    [DisallowMultipleComponent]
    public class FlexNetworkTransform : FlexNetworkTransformBase
    {
        #region Public.
        /// <summary>
        /// Transform to synchronize.
        /// </summary>
        public override Transform TargetTransform => base.transform;
        #endregion

        protected override void OnEnable()
        {
            base.OnEnable();
        }
        protected override void OnDisable()
        {
            base.OnDisable();
        }

        /// <summary>
        /// Sets which object this transform is on.
        /// </summary>
        /// <param name="attachedIdentity">NetworkIdentity of the object this transform is on.</param>
        /// <param name="componentIndex">ComponentIndex of the NetworkBehaviour for the attachedIdentity to use. Used if you wish to attach to child NetworkBehaviours.</param>
        public void SetAttached(NetworkIdentity attachedIdentity, sbyte componentIndex)
        {
            if (componentIndex > sbyte.MaxValue)
            {
                Debug.LogError("ComponentIndex must be less than " + sbyte.MaxValue.ToString() + ".");
                return;
            }

            base.SetAttachedInternal(attachedIdentity, componentIndex);
        }

        /// <summary>
        /// Sets which object this transform is on. 
        /// </summary>
        /// <param name="attachedIdentity">NetworkIdentity of the object this transform is on.</param>
        public void SetAttached(NetworkIdentity attachedIdentity)
        {
            base.SetAttachedInternal(attachedIdentity, -1);
        }

        /// <summary>
        /// Sets which platform this transform is on.
        /// </summary>
        /// <param name="attachedIdentity"></param>
        [Obsolete("SetPlatform is being replaced with SetAttached to support child objects. Please use SetAttached.")]
        public void SetPlatform(NetworkIdentity platformIdentity)
        {
            base.SetAttachedInternal(platformIdentity, -1);
        }

    }
}

