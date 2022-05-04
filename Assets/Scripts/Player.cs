using FishNet.Object;
using UnityEngine;

public class Player : NetworkBehaviour
{
    private HealthHandler healthHandler;
    private const float rotationSpeed = 360.0f;
    private const float movementSpeed = 2.0f;
    private float updatedRotationDelta = 0.0f;
    private Vector3 updatedTranslationDelta = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        healthHandler = GetComponent<HealthHandler>();
        healthHandler.OnDeathDelegate = OnDeath;
    }

    private void FixedUpdate()
    {

    }

    [Client]
    void Update()
    {
    /*    if (!base.IsOwner) { return; }
        transform.Rotate(Vector3.up, updatedRotationDelta);
        updatedRotationDelta = 0.0f;
        transform.Translate(updatedTranslationDelta);
        updatedTranslationDelta = Vector3.zero;
        // TODO: We need to change from client authentication
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");
       // transform.Rotate(Vector3.up, horizontal * rotationSpeed * Time.deltaTime);
      //  transform.Translate(vertical * Vector3.forward * movementSpeed * Time.deltaTime);
         serverRequestMovement(horizontal, vertical);
  */
    }

    [ServerRpc]
    private void serverRequestMovement(float horizontalMovement, float verticalMovement)
    {
        // TODO: Validate requested data.
        // RpcMovePlayer(horizontalMovement, verticalMovement);
        float rotationDelta = horizontalMovement * rotationSpeed * Time.deltaTime;
        Vector3 translationDelta = verticalMovement * Vector3.forward * movementSpeed * Time.deltaTime;
        RpcMovePlayer(translationDelta, rotationDelta);
    }

    [ObserversRpc]
    private void RpcMovePlayer(Vector3 translationDelta, float rotationDelta)
    {
        if (!base.IsOwner) { return; }
        updatedRotationDelta = rotationDelta;
        updatedTranslationDelta = translationDelta;
    }

    private void OnDeath()
    {
        Destroy(gameObject);
    }
}
