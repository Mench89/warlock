using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour, IDrownable
{
    enum PlayerState
    {
        Alive, Zombified, Drowned
    }
    [SyncVar] public string playerName;
    [SerializeField] public AudioClip[] fallingSounds;
    [SerializeField] public AudioClip[] deathSounds;
    [SerializeField] public AudioClip[] takingDamageSounds;
    private HealthHandler healthHandler;
    private const float rotationSpeed = 360.0f;
    private const float raiseSpeed = 45.0f;
    private const float movementSpeed = 2.0f;
    private const float DEATH_MOVEMENT_FACTOR = 0.3f;
    private const float DEFAULT_MOVEMENT_FACTOR = 1.0f;
    // 1.0 by default but could be affected by events and other effects, like death.
    private float movementFactor = DEFAULT_MOVEMENT_FACTOR;
    private Material DeadMaterial;
    private Rigidbody rigidBody;

    // TODO: Make server property
    private bool isFalling;
    [SyncVar(hook = nameof(SetPlayerState))] private PlayerState playerState = PlayerState.Alive;
    [SyncVar] public int playerId;

    // Start is called before the first frame update
    void Start()
    {
        DeadMaterial = Resources.Load<Material>("Materials/Toon Chicken Dead");
        healthHandler = GetComponent<HealthHandler>();
        healthHandler.OnDeathDelegate = OnDeath;
        healthHandler.OnDamageTakenDelegate = OnDamageTaken;
        rigidBody = GetComponent<Rigidbody>();
        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material = MaterialForPlayerId(playerId);
        if (playerState == PlayerState.Drowned || playerState == PlayerState.Zombified)
        {
            KillPlayer(false);
        }
    }

    [Client]
    void Update()
    {
        CheckIfFalling();
        if (!isOwned) { return; }
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
                    GetComponent<AudioSource>().PlayOneShot(GetRandomFallingSound(), 0.7f);
                }
                isFalling = true;
            }
        }
        else if (rigidBody.velocity.y > -4f)
        {
            isFalling = false;
        }
    }

    private void OnDamageTaken(int damage)
    {
        GetComponent<AudioSource>().PlayOneShot(GetRandomDamageSound(), 0.7f);
    }

    [Server]
    private void OnDeath()
    {
        playerState = PlayerState.Zombified;
    }

    [Server]
    public void OnDrown()
    {
        playerState = PlayerState.Drowned;
    }

    private void ResetPlayer()
    {
        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material = MaterialForPlayerId(playerId);
        movementFactor = DEFAULT_MOVEMENT_FACTOR;
        isFalling = false;
    }

    private void KillPlayer(bool playAudio)
    {
        if (playerState == PlayerState.Alive && playAudio)
        {
            GetComponent<AudioSource>().PlayOneShot(GetRandomDeathSound(), 0.7f);
        }

        movementFactor = DEATH_MOVEMENT_FACTOR;
        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material = DeadMaterial;
    }

    private void SetPlayerState(PlayerState oldState, PlayerState newState)
    {
        if (oldState == newState) { return; }
        switch (newState)
        {
            case PlayerState.Alive:
                ResetPlayer();
                break;
            case PlayerState.Zombified:
                KillPlayer(true);
                break;
            case PlayerState.Drowned:
                KillPlayer(true);
                break;
        }
    }

    [Command]
    private void CommandRequestRespawn()
    {
        if (playerState != PlayerState.Drowned) { return; }
        healthHandler.ResetHealth();
        var startTransform = WLNetworkManager.singleton.GetStartPosition();
        playerState = PlayerState.Alive;
        RpcRespawnPlayer(startTransform.position, startTransform.rotation);
    }

    [ClientRpc]
    private void RpcRespawnPlayer(Vector3 startPosition, Quaternion startRotation)
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        rigidBody.velocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
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

    private AudioClip GetRandomFallingSound()
    {
        return fallingSounds[Random.Range(0, fallingSounds.Length - 1)];
    }

    private AudioClip GetRandomDeathSound()
    {
        return deathSounds[Random.Range(0, deathSounds.Length - 1)];
    }

    private AudioClip GetRandomDamageSound()
    {
        return takingDamageSounds[Random.Range(0, takingDamageSounds.Length - 1)];
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
