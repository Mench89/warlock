using Unity;
using Mirror;
using UnityEngine;

public class WLNetworkManager : NetworkManager
{
    private const string PLACEHOLDER_PLAYER_NAME = "Player name";
    public string playerName = PLACEHOLDER_PLAYER_NAME;
    public override void OnStartServer()
    {
        base.OnStartServer();

        NetworkServer.RegisterHandler<CreatePlayerMessage>(OnCreatePlayer);
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        // you can send the message here, or wherever else you want
        CreatePlayerMessage playerMessage = new CreatePlayerMessage
        {
            name = playerName
        };

        NetworkClient.Send(playerMessage);
    }

    void OnCreatePlayer(NetworkConnectionToClient conn, CreatePlayerMessage message)
    {
        Transform startPos = GetStartPosition();

        // playerPrefab is the one assigned in the inspector in Network
        // Manager but you can use different prefabs per race for example
        GameObject gameobject = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        // call this to use this gameobject as the primary controller
        NetworkServer.AddPlayerForConnection(conn, gameobject);

        // Apply data from the message however appropriate for your game
        // Typically Player would be a component you write with syncvars or properties
        Player player = gameobject.GetComponent<Player>();
        int playerId = numPlayers;
        player.SetPlayerId(playerId, message.name);
        Scoreboard.instance.AddPlayerToList(player);
    }

}
