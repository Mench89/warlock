using UnityEngine;
using Mirror;

public class WaterDrownHandler : NetworkBehaviour
{
    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        IDrownable drownable = collision.gameObject.GetComponent<IDrownable>();
        if (drownable != null)
        {
            drownable.OnDrown();
        }
    }
}
