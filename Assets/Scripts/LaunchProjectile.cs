using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class LaunchProjectile : NetworkBehaviour
{
    [SerializeField] public Transform projectTileLaunchPosition;
    [SerializeField] public Collider colliderToIgnore;
    [SerializeField] public AudioClip[] shootingSounds;
    private HealthHandler healthHandler;
    public GameObject projectile;

    // Start is called before the first frame update
    void Start()
    {
        healthHandler = GetComponent<HealthHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isOwned || healthHandler.IsDead) { return; }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CommandShootBullet();
        }
    }

    [Command]
    private void CommandShootBullet()
    {
        GameObject bulletObject = Instantiate(projectile,
            projectTileLaunchPosition.position,
            projectTileLaunchPosition.rotation);
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.owningPlayer = GetComponent<Player>();
        }
        bulletObject.GetComponent<Rigidbody>().AddForce(projectTileLaunchPosition.forward * 10);
        if (colliderToIgnore) {
            Debug.Log("Magnus, collider component: " + colliderToIgnore.ToString());
            Physics.IgnoreCollision(bulletObject.GetComponent<Collider>(), colliderToIgnore);
        }

        NetworkServer.Spawn(bulletObject);
        RpcBulletSpawned();
    }

    [ClientRpc]
    private void RpcBulletSpawned()
    {
        GetComponent<AudioSource>().PlayOneShot(GetRandomShootingSound(), 0.7f);
    }

    private AudioClip GetRandomShootingSound()
    {
        return shootingSounds[Random.Range(0, shootingSounds.Length - 1)];
    }
}
