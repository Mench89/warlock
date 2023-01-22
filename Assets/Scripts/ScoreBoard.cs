using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public interface IScoreboardPoints
{
    public void PlayerAdded(PlayerInfo playerInfo, int points);
    public void PlayerRemvoved(PlayerInfo player);
    public void AddPointsToPlayer(PlayerInfo player, int points);
}

public struct PlayerInfo: INetworkSerializable
{
    public ulong PlayerId;
    public FixedString64Bytes Name;

    public PlayerInfo(ulong playerId, FixedString64Bytes name)
    {
        PlayerId = playerId;
        Name = name;
    }

    void INetworkSerializable.NetworkSerialize<T>(BufferSerializer<T> serializer)
    {
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref Name);
    }
}

public class Scoreboard : NetworkBehaviour
{

    public readonly NetworkVariable<Dictionary<PlayerInfo, int>> playerScores = new ();
    private readonly List<IScoreboardPoints> scoreboardListeners = new List<IScoreboardPoints>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        playerScores.OnValueChanged += OnPlayerScoreChanged;
    }

    void OnPlayerScoreChanged(Dictionary<PlayerInfo, int> oldList, Dictionary<PlayerInfo, int> newList)
    {

// TODO: Update
        /*switch (op)
        {
            case SyncDictionary<PlayerInfo, int>.Operation.OP_ADD:
                PlayerAdded(key, item);
                break;
            case SyncDictionary<PlayerInfo, int>.Operation.OP_SET:
                PlayerScored(key, item);
                break;
            case SyncDictionary<PlayerInfo, int>.Operation.OP_REMOVE:
                PlayerRemoved(key);
                break;
            case SyncDictionary<PlayerInfo, int>.Operation.OP_CLEAR:
                break;
        } */
    }

    public void AddScoreboardPointsListener(IScoreboardPoints listener)
    {
        if (!scoreboardListeners.Contains(listener))
        {
            scoreboardListeners.Add(listener);
        }
    }

    public void RemoveScorebardListener(IScoreboardPoints listener) {
        scoreboardListeners.Remove(listener);
    }

    [ServerRpc]
    public void AddPlayerToListServerRpc(PlayerInfo playerInfo)
    {
        const int initialPoints = 0;
        if (!playerScores.Value.ContainsKey(playerInfo))
        {
            playerScores.Value.Add(playerInfo, initialPoints);
        }
    }

    [ServerRpc]
    public void RemovePlayerFromListServerRpc(ulong ownerClientId, string playerName)
    {
        PlayerInfo playerInfo = new(ownerClientId, playerName);
        playerScores.Value.Remove(playerInfo);
    }

    [ServerRpc]
    public void AddPointToPlayerServerRpc(int points, ulong ownerClientId, string playerName)
    {
        PlayerInfo playerInfo = new(OwnerClientId, playerName);
        if (!playerScores.Value.ContainsKey(playerInfo)) {
            playerScores.Value.Add(playerInfo, points);
        } else
        {
            playerScores.Value[playerInfo] += points;
        }
    }

    private void PlayerAdded(PlayerInfo player, int initialPoints)
    {
        ReportAddedPlayerToListeners(player, initialPoints);
    }

    private void PlayerRemoved(PlayerInfo player)
    {
        ReportRemovedPlayerToListeners(player);
    }

    private void PlayerScored(PlayerInfo player, int points)
    {
        Debug.Log("Player: " + player.Name + " scored " + points + " points!");
        ReportPointsForPlayerToListeners(player, points);
    }

    private void ReportPointsForPlayerToListeners(PlayerInfo player, int points)
    {
        foreach (IScoreboardPoints listener in scoreboardListeners)
        {
            listener.AddPointsToPlayer(player, points);
        }
    }

    private void ReportAddedPlayerToListeners(PlayerInfo player, int initialPoints)
    {
        foreach (IScoreboardPoints listener in scoreboardListeners)
        {
            listener.PlayerAdded(player, initialPoints);
        }
    }

    private void ReportRemovedPlayerToListeners(PlayerInfo player)
    {
        foreach (IScoreboardPoints listener in scoreboardListeners)
        {
            listener.PlayerRemvoved(player);
        }
    }
}
