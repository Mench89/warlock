using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;

public class ScoreboardViewController : NetworkBehaviour, IScoreboardPoints
{
    public struct ScoreItem
    {
        public ScoreItem(PlayerInfo playerInfo, int score)
        {
            PlayerInfo = playerInfo;
            Score = score;
        }
        public PlayerInfo PlayerInfo;
        public int Score;
    }

    [SerializeField] public VisualTreeAsset listItemAsset;
    [SerializeField] public Scoreboard scoreboard;
    List<ScoreItem> items;
    private ListView listView;

    private void OnEnable()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        listView = rootVisualElement.Q<ListView>("ScoreList");
        listView.makeItem = MakeItem;// () => listItemAsset.CloneTree();
        listView.bindItem = BindItem;
    }

    private VisualElement MakeItem()
    {
        //Here we take the uxml and make a VisualElement
        VisualElement listItem = listItemAsset.CloneTree();
        listItem.SetEnabled(true);
        return listItem;

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        scoreboard.AddScoreboardPointsListener(this);
        PopulateListView();
    }

    private void PopulateListView()
    {
        int itemCount = scoreboard.playerScores.Value.Count;
        items = new List<ScoreItem>(itemCount);
        foreach (var item in scoreboard.playerScores.Value)
        {
            Debug.Log("Added player to scoreboard:" + item.Key.Name);
            items.Add(new ScoreItem(item.Key, item.Value));
        }

        listView.itemsSource = items;
        listView.Rebuild();
    }

    private void BindItem(VisualElement e, int index)
    {
        Label nameLabel = e.Q<Label>("NameLabel");
        nameLabel.text = items[index].PlayerInfo.Name.ToString();
        Label pointsLabel = e.Q<Label>("PointsLabel");
        pointsLabel.text = items[index].Score.ToString();
    }

    // IScoreboardPoints methods

    // TODO: Optimize these methods
    public void PlayerAdded(PlayerInfo player, int points) {
        PopulateListView();
    }

    public void PlayerRemvoved(PlayerInfo player) {
        PopulateListView();
    }

    public void AddPointsToPlayer(PlayerInfo player, int points) {
        PopulateListView();
    }
}

