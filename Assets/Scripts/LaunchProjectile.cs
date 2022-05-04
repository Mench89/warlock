using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class LaunchProjectile : NetworkBehaviour
{
    [SerializeField] public Transform projectTileLaunchPosition;
    public GameObject projectile;
    public float launchVelocity = 700f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!base.IsOwner) { return; }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            commandShootBullet();
        }
    }

    [ServerRpc]
    private void commandShootBullet()
    {
        GameObject bullet = Instantiate(projectile,
            projectTileLaunchPosition.position,
            projectTileLaunchPosition.rotation);
        bullet.GetComponent<Rigidbody>().AddForce(projectTileLaunchPosition.forward * 100);
        base.Spawn(bullet);
        
        // RpcOnFire();
    }
}
