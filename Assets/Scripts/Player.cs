using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour, IDrownable
{
    enum PlayerState
    {
        Alive, Zombified, Drowned
    }
    [SyncVar] public string playerName;
    private HealthHandler healthHandler;
    private const float rotationSpeed = 360.0f;
    private const float raiseSpeed = 45.0f;
    private const float movementSpeed = 2.0f;
    private const float DEATH_MOVEMENT_FACTOR = 0.3f;
    private const float DEFAULT_MOVEMENT_FACTOR = 1.0f;
    // 1.0 by default but could be affected by events and other effects, like death.
    private float movementFactor = DEFAULT_MOVEMENT_FACTOR;
    private Material DeadMaterial;
    private bool hasHandledDeath;
    private Rigidbody rigidBody;
    private AudioClip fallingSound;
    private AudioClip deathSound;
    // TODO: Make server property
    private bool isFalling;
    private PlayerState playerState;
    [SyncVar] private int playerId;
    // Start is called before the first frame update
    void Start()
    {
        DeadMaterial = Resources.Load<Material>("Materials/Toon Chicken Dead");
        fallingSound = Resources.Load<AudioClip>("Audio/Wilhelm-Scream");
        deathSound = Resources.Load<AudioClip>("Audio/death1");
        healthHandler = GetComponent<HealthHandler>();
        healthHandler.OnDeathDelegate = OnDeath;
        rigidBody = GetComponent<Rigidbody>();
        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material = MaterialForPlayerId(playerId);
        playerState = PlayerState.Alive;
    }

    [Client]
    void Update()
    {
        if (!hasHandledDeath && healthHandler.IsDead)
        {
            OnDeath();
        }
        CheckIfFalling();
        if (!hasAuthority) { return; }
    /*    transform.Rotate(Vector3.up, updatedRotationDelta);
        updatedRotationDelta = 0.0f;
        transform.Translate(updatedTranslationDelta);
        updatedTranslationDelta = Vector3.zero;*/
        // TODO: We need to change from client authentication
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");
        
        transform.Rotate(Vector3.up, horizontal * rotationSpeed * movementFactor * Time.deltaTime);
        transform.Translate(vertical * Vector3.forward * movementSpeed * movementFactor * Time.deltaTime);

        // serverRequestMovement(horizontal, vertical);
        var raise = Input.GetAxis("Raise");
        RaisePlayer(raise);

        if (Input.GetKeyDown(KeyCode.R)) {
            if (playerState == PlayerState.Drowned)
            {
                CommandRequestRespawn();
            }
        }


    }

    [ClientRpc]
    public void SetPlayerId(int playerId, string name)
    {
        this.playerId = playerId;
        playerName = name;
        Debug.Log("Magnus, player named: " + name + " has joined the game! And player id: " + playerId);
        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material = MaterialForPlayerId(playerId);
    }

    private void CheckIfFalling()
    {
        if (rigidBody.velocity.y < -5f)
        {
            if (!isFalling)
            {
                if (!healthHandler.IsDead)
                {
                    GetComponent<AudioSource>().PlayOneShot(fallingSound, 0.7f);
                }
                isFalling = true;
            }
        }
        else if (rigidBody.velocity.y > -4f)
        {
            isFalling = false;
        }
    }

    // TODO: Make this a call from server
    [ClientRpc]
    private void OnDeath()
    {
        KillPlayer();
        playerState = PlayerState.Zombified;
    }

    public void OnDrown()
    {
        KillPlayer();
        playerState = PlayerState.Drowned;
    }

    private void KillPlayer()
    {
        if (playerState == PlayerState.Alive)
        {
            GetComponent<AudioSource>().PlayOneShot(deathSound, 0.7f);
        }
        
        movementFactor = DEATH_MOVEMENT_FACTOR;
        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material = DeadMaterial;
        hasHandledDeath = true;
    }

    [Command]
    private void CommandRequestRespawn()
    {
        if (playerState != PlayerState.Drowned) { return; }
        healthHandler.ResetHealth();
        var startTransform = WLNetworkManager.singleton.GetStartPosition();
        RpcRespawnPlayer(startTransform.position, startTransform.rotation);
    }

    [ClientRpc]
    private void RpcRespawnPlayer(Vector3 startPosition, Quaternion startRotation)
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material = MaterialForPlayerId(playerId);
        movementFactor = DEFAULT_MOVEMENT_FACTOR;
        hasHandledDeath = false;
        isFalling = false;
        playerState = PlayerState.Alive;
    }

    // TODO: Make as a server command
    private void RaisePlayer(float raiseValue)
    {
        if (raiseValue < 0.01)
        {
            return;
        }
        transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        return;
        // TODO: Take a look at this again, should nere work.
        /*if (raiseValue > 0.0)
        {
            var prelAngleRaise = raiseValue * raiseSpeed * MovementFactor * Time.deltaTime;
            float angleX = 0.0f;
            float angleZ = 0.0f;

            if (Mathf.Abs(transform.rotation.eulerAngles.x) > 5.0f)
            {
                if (transform.rotation.eulerAngles.x > 0)
                {
                    angleX = Mathf.Min(-prelAngleRaise, 0);
                }
                else
                {
                    angleX = Mathf.Max(prelAngleRaise, 0);
                }
                Debug.Log("Magnus, angleX NEW: " + angleX);
            }

            if (Mathf.Abs(transform.rotation.eulerAngles.z) > 5.0f)
            {
                if (transform.rotation.eulerAngles.z > 0)
                {
                    angleZ = Mathf.Min(-prelAngleRaise, 0);
                }
                else
                {
                    angleZ = Mathf.Max(prelAngleRaise, 0);
                }
                Debug.Log("Magnus, angleZ NEW: " + angleZ);
            }


            Debug.Log("Magnus, angleX: " + transform.eulerAngles.x + " angleZ: " + transform.rotation.eulerAngles.z);
            //transform.Rotate(Vector3.forward, angle);
             //transform.Rotate(angleX, 0.0f, angleZ);
            // transform.rotation *= Quaternion.AngleAxis(angleX, Vector3.right);
            //transform.rotation *= Quaternion.AngleAxis(angleZ, Vector3.forward);
            transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

            Vector3 to = new Vector3(90, transform.eulerAngles.y, 90);
            if (Vector3.Distance(transform.eulerAngles, to) > 1.0f)
            {
               // transform.eulerAngles = Vector3.Lerp(transform.rotation.eulerAngles, to, Time.deltaTime);
            }
        }
        */
    }

    private Material MaterialForPlayerId(int playerId)
    {
        if (playerId == 1) { return Resources.Load<Material>("Materials/Toon Chicken Yellow"); }
        if (playerId == 2) { return Resources.Load<Material>("Materials/Toon Chicken Red"); }
        if (playerId == 3) { return Resources.Load<Material>("Materials/Toon Chicken Blue"); }
        if (playerId == 4) { return Resources.Load<Material>("Materials/Toon Chicken Green"); }
        return Resources.Load<Material>("Materials/Toon Chicken White");
    }
}
