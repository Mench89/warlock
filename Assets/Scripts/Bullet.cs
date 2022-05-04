using FishNet.Object;
using FishNet.Managing.Logging;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    // Automatically destroy the bullet after some time.
    private const float MAX_TIME_TO_LIVE = 5.0f;
    private const int DAMAGE = 1;

    // Start is called before the first frame update
    void Start()
    {
        // TODO: Needs to fix collision ignore.....
       // Debug.Log("Magnus, collider component: " + NetworkClient.localPlayer.GetComponentInChildren<Collider>().ToString());
       // Physics.IgnoreCollision(GetComponent<Collider>(), NetworkClient.localPlayer.GetComponentInChildren<Collider>());
        Destroy(gameObject, MAX_TIME_TO_LIVE);
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    [Server(Logging = LoggingType.Off)]
    void OnCollisionEnter(Collision collision)
    {
        GameObject otherObj = collision.gameObject;
        Debug.Log("Collided with: " + otherObj);
        if (!otherObj.CompareTag(gameObject.tag))
        {
            Debug.Log("Magnus tag: " + otherObj.tag);
            otherObj.GetComponent<Rigidbody>().AddRelativeForce(gameObject.transform.forward, ForceMode.Impulse);
            HealthHandler healthHandler = otherObj.GetComponent<HealthHandler>();
            if (healthHandler != null)
            {
                healthHandler.ApplyDamage(DAMAGE);
            }
            Destroy(gameObject);
        }
    }

    [Server(Logging = LoggingType.Off)]
    void OnTriggerEnter(Collider collider)
    {
        GameObject otherObj = collider.gameObject;
        Debug.Log("Triggered with: " + otherObj);
    }
}
