using UnityEngine;
using Unity.Netcode;

public class WaterDrownHandler : NetworkBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        IDrownable drownable = collision.gameObject.GetComponent<IDrownable>();
        if (drownable != null)
        {
            drownable.OnDrown();
        }
    }
}
