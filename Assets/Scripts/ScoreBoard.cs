using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Scoreboard : NetworkBehaviour
{
    public static Scoreboard instance = null;
    private IDictionary<Player, int> playerScores = new Dictionary<Player, int>();

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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Server]
    public void AddPointToPlayer(int points, Player player)
    {
        if (player == null) { return; }
        if (!playerScores.Keys.Contains(player)) {
            playerScores.Add(player, points);
        } else
        {
            playerScores[player] += points;
        }
        RpcPlayerScored(points, player);
    }


    [ClientRpc]
    public void RpcPlayerScored(int points, Player player)
    {
        Debug.Log("Player: " + player.playerName + " scored " + points + " points!");
    }
}
