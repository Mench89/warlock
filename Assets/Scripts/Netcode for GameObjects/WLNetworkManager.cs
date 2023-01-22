using Unity;
using Unity.Netcode;
using UnityEngine;

public class WLNetworkManager : NetworkManager
{
    private const string PLACEHOLDER_PLAYER_NAME = "Player name";
    [SerializeField] public Scoreboard scoarboard;
    public string playerName = PLACEHOLDER_PLAYER_NAME;

   private void Setup()
{
    NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    //NetworkManager.Singleton.StartHost();
}
private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
{
    // The client identifier to be authenticated
    var clientId = request.ClientNetworkId;

    // Additional connection data defined by user code
    var connectionData = request.Payload;

    // Your approval logic determines the following values
    response.Approved = true;
    response.CreatePlayerObject = true;

    // The prefab hash value of the NetworkPrefab, if null the default NetworkManager player prefab is used
    response.PlayerPrefabHash = null;

    // Position to spawn the player object (if null it uses default of Vector3.zero)
    response.Position = Vector3.zero;

    // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)
    response.Rotation = Quaternion.identity;

    // If additional approval steps are needed, set this to true until the additional steps are complete
    // once it transitions from true to false the connection approval response will be processed.
    response.Pending = false;
}

    // public override void OnStartServer()
    // {
    //     base.OnStartServer();

    //     NetworkServer.RegisterHandler<CreatePlayerMessage>(OnCreatePlayer);
    // }

    // public override void OnClientConnect()
    // {
    //     base.OnClientConnect();

    //     // you can send the message here, or wherever else you want
    //     CreatePlayerMessage playerMessage = new CreatePlayerMessage
    //     {
    //         name = playerName
    //     };

    //     NetworkClient.Send(playerMessage);
    // }

    // void OnCreatePlayer(NetworkConnectionToClient conn, CreatePlayerMessage message)
    // {
    //     Transform startPos = GetStartPosition();

    //     // playerPrefab is the one assigned in the inspector in Network
    //     // Manager but you can use different prefabs per race for example
    //     GameObject gameobject = startPos != null
    //         ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
    //         : Instantiate(playerPrefab);

    //     // call this to use this gameobject as the primary controller
    //     NetworkServer.AddPlayerForConnection(conn, gameobject);

    //     // Apply data from the message however appropriate for your game
    //     // Typically Player would be a component you write with syncvars or properties
    //     Player player = gameobject.GetComponent<Player>();
    //     int playerId = numPlayers;
    //   /*  player.SetPlayerId(playerId, message.name);
    //     PlayerInfo playerInfo = new PlayerInfo(playerId, message.name);
    //     scoarboard.AddPlayerToList(playerInfo); */
    // }


}