using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScoreboardViewController : MonoBehaviour, IScoreboardPoints
{
    public struct ScoreItem
    {
        public ScoreItem(Player player, int score)
        {
            Player = player;
            Score = score;
        }
        public Player Player;
        public int Score;
    }

    [SerializeField] public VisualTreeAsset listItemAsset;
    List<ScoreItem> items;
    private ListView listView;

    private void OnEnable()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        listView = rootVisualElement.Q<ListView>("ScoreList");
        listView.makeItem = () => listItemAsset.CloneTree();
        listView.bindItem = BindItem;
        
    }

    private void Start()
    {
        Scoreboard.instance.AddScoreboardPointsListener(this);
        PopulateListView();
    }

    private void PopulateListView()
    {
        // Create a list of data. In this case, numbers from 1 to 1000.
        int itemCount = Scoreboard.instance.playerScores.Count;
        items = new List<ScoreItem>(itemCount);
        foreach (var item in Scoreboard.instance.playerScores)
        {
            Debug.Log("Added player to scoreboard:" + item.Key.playerName);
            items.Add(new ScoreItem(item.Key, item.Value));
        }

        listView.itemsSource = items;
        listView.Rebuild();
    }

    private void BindItem(VisualElement e, int index)
    {
        Label nameLabel = e.Q<Label>("NameLabel");
        nameLabel.text = items[index].Player.playerName;
        Label pointsLabel = e.Q<Label>("PointsLabel");
        pointsLabel.text = items[index].Score.ToString();
    }

    // IScoreboardPoints methods

    // TODO: Need to optimize these methods
    public void PlayerAdded(Player player, int points) {
        PopulateListView();
    }
    
    public void PlayerRemvoved(Player player) {
        PopulateListView();
    }

    public void AddPointsToPlayer(Player player, int points) {
        PopulateListView();
    }
}

