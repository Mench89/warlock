using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public string playerName;
    private HealthHandler healthHandler;
    private const float rotationSpeed = 360.0f;
    private const float movementSpeed = 2.0f;
    private const float DEATH_MOVEMENT_FACTOR = 0.3f;
    // 1.0 by default but could be affected by events and other effects, like death.
    private float MovementFactor = 1.0f;
    private Material DeadMaterial;
    private bool HasHandledDeath;
    // Start is called before the first frame update
    void Start()
    {
        DeadMaterial = Resources.Load("Materials/Toon Chicken Dead", typeof(Material)) as Material;
        healthHandler = GetComponent<HealthHandler>();
        healthHandler.OnDeathDelegate = OnDeath;
    }

    private void FixedUpdate()
    {

    }

    [Client]
    void Update()
    {
        if (!HasHandledDeath && healthHandler.IsDead)
        {
            OnDeath();
        }
        if (!hasAuthority) { return; }
    /*    transform.Rotate(Vector3.up, updatedRotationDelta);
        updatedRotationDelta = 0.0f;
        transform.Translate(updatedTranslationDelta);
        updatedTranslationDelta = Vector3.zero;*/
        // TODO: We need to change from client authentication
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");
        transform.Rotate(Vector3.up, horizontal * rotationSpeed * MovementFactor * Time.deltaTime);
        transform.Translate(vertical * Vector3.forward * movementSpeed * MovementFactor * Time.deltaTime);
        // serverRequestMovement(horizontal, vertical);
  
    }

    [Command]
    private void serverRequestMovement(float horizontalMovement, float verticalMovement)
    {
        // TODO: Validate requested data.
        // RpcMovePlayer(horizontalMovement, verticalMovement);
        float rotationDelta = horizontalMovement * rotationSpeed * Time.deltaTime;
        Vector3 translationDelta = verticalMovement * Vector3.forward * movementSpeed * Time.deltaTime;
        RpcMovePlayer(translationDelta, rotationDelta);
    }

    [ClientRpc]
    private void RpcMovePlayer(Vector3 translationDelta, float rotationDelta)
    {
        if (!hasAuthority) { return; }
       // updatedRotationDelta = rotationDelta;
     //   updatedTranslationDelta = translationDelta;
    }

    // TODO: Make this a call from server
    private void OnDeath()
    {
        MovementFactor = DEATH_MOVEMENT_FACTOR;
        gameObject.GetComponentInChildren<SkinnedMeshRenderer>().material = DeadMaterial;
        HasHandledDeath = true;
        //Destroy(gameObject);
    }
}
