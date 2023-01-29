using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public interface IScoreboardPoints
{
    public void PlayerAdded(PlayerInfo playerInfo, int points);
    public void PlayerRemvoved(PlayerInfo player);
    public void AddPointsToPlayer(PlayerInfo player, int points);
}

public struct PlayerInfo
{
    public int PlayerId;
    public string Name;

    public PlayerInfo(int playerId, string name)
    {
        PlayerId = playerId;
        Name = name;
    }
}

public class Scoreboard : NetworkBehaviour
{
    public readonly SyncDictionary<PlayerInfo, int> playerScores = new SyncDictionary<PlayerInfo, int>();
    private readonly List<IScoreboardPoints> scoreboardListeners = new List<IScoreboardPoints>();
    [SerializeField]
    private ScoreboardViewController scoreboardViewController;

    public override void OnStartClient()
    {
        base.OnStartClient();
        playerScores.Callback += OnPlayerScoreChanged;
        AddScoreboardPointsListener(scoreboardViewController);
        scoreboardViewController.scoreboard = this;
    }

    void OnPlayerScoreChanged(SyncDictionary<PlayerInfo, int>.Operation op, PlayerInfo key, int item)
    {
        switch (op)
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
        }
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

    [Server]
    public void AddPlayerToList(PlayerInfo playerInfo)
    {
        const int initialPoints = 0;
        if (!playerScores.ContainsKey(playerInfo))
        {
            playerScores.Add(playerInfo, initialPoints);
        }
    }

    [Server]
    public void RemovePlayerFromList(Player player)
    {
        if (player == null) { return; }
        PlayerInfo playerInfo = new(player.playerId, player.playerName);
        playerScores.Remove(playerInfo);
    }

    [Server]
    public void AddPointToPlayer(int points, Player player)
    {
        if (player == null) { return; }
        PlayerInfo playerInfo = new(player.playerId, player.playerName);
        if (!playerScores.ContainsKey(playerInfo)) {
            playerScores.Add(playerInfo, points);
        } else
        {
            playerScores[playerInfo] += points;
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
