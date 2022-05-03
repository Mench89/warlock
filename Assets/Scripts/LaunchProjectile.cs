using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LaunchProjectile : NetworkBehaviour
{
    [SerializeField] [SyncVar] public Transform projectTileLaunchPosition;
    public GameObject projectile;
    public float launchVelocity = 700f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasAuthority) { return; }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            commandShootBullet();
        }
    }

    [Command]
    private void commandShootBullet()
    {
        GameObject bullet = Instantiate(projectile,
            projectTileLaunchPosition.position,
            projectTileLaunchPosition.rotation);
        bullet.GetComponent<Rigidbody>().AddForce(projectTileLaunchPosition.forward * 100);
        NetworkServer.Spawn(bullet);
        
        // RpcOnFire();
    }
}
