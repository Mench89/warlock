using Mirror;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SyncVar]
    public Player owningPlayer;
    // Automatically destroy the bullet after some time.
    private const float MAX_TIME_TO_LIVE = 5.0f;
    private const int DAMAGE = 1;

    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, MAX_TIME_TO_LIVE);
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    [ServerCallback]
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
                var isAlive = !healthHandler.IsDead;
                healthHandler.ApplyDamage(DAMAGE);
                // TODO: Remove later
                Scoreboard.instance.AddPointToPlayer(1, owningPlayer);
                // Check for killing blow
                if (healthHandler.IsDead && isAlive)
                {
                    Debug.Log("Killing blow!");
                    Scoreboard.instance.AddPointToPlayer(1, owningPlayer);
                }
            }
            Destroy(gameObject);
        }
    }
}
