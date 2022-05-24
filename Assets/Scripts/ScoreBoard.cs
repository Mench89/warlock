using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public interface IScoreboardPoints
{
    public void PlayerAdded(Player player, int points);
    public void PlayerRemvoved(Player player);
    public void AddPointsToPlayer(Player player, int points);
}

public class Scoreboard : NetworkBehaviour
{
    public static Scoreboard instance = null;
    public SyncDictionary<Player, int> playerScores { get; private set; } = new SyncDictionary<Player, int>(); 
    private List<IScoreboardPoints> scoreboardListeners = new List<IScoreboardPoints>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        } else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void AddScoreboardPointsListener(IScoreboardPoints listener)
    {
        scoreboardListeners.Add(listener);
    }

    public void RemoveScorebardListener(IScoreboardPoints listener) {
        scoreboardListeners.Remove(listener);
    }

    [Server]
    public void AddPlayerToList(Player player)
    {
        if (player == null) { return; }
        const int initialPoints = 0;
        if (!playerScores.ContainsKey(player))
        {
            playerScores.Add(player, initialPoints);
        }
        RpcPlayerAdded(player, initialPoints);
    }

    [Server]
    public void RemovePlayerFromList(Player player)
    {
        if (player == null) { return; }
        playerScores.Remove(player);

    }

    [Server]
    public void AddPointToPlayer(int points, Player player)
    {
        if (player == null) { return; }
        if (!playerScores.ContainsKey(player)) {
            playerScores.Add(player, points);
        } else
        {
            playerScores[player] += points;
        }
        RpcPlayerScored(player, points);
    }

    [ClientRpc]
    public void RpcPlayerScored(Player player, int points)
    {
        Debug.Log("Player: " + player.playerName + " scored " + points + " points!");
        ReportPointsForPlayerToListeners(player, points);
    }

    [ClientRpc]
    public void RpcPlayerAdded(Player player, int initialPoints)
    {
        ReportAddedPlayerToListeners(player, initialPoints);
    }

    [ClientRpc]
    public void RpcPlayerRemoved(Player player)
    {
        ReportRemovedPlayerToListeners(player);
    }

    private void ReportPointsForPlayerToListeners(Player player, int points)
    {
        foreach (IScoreboardPoints listener in scoreboardListeners)
        {
            listener.AddPointsToPlayer(player, points);
        }
    }

    private void ReportAddedPlayerToListeners(Player player, int initialPoints)
    {
        foreach (IScoreboardPoints listener in scoreboardListeners)
        {
            listener.PlayerAdded(player, initialPoints);
        }
    }

    private void ReportRemovedPlayerToListeners(Player player)
    {
        foreach (IScoreboardPoints listener in scoreboardListeners)
        {
            listener.PlayerRemvoved(player);
        }
    }
}
