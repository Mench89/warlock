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
    private ListView listView;
    List<ScoreItem> items;

    private void OnEnable()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
        listView = rootVisualElement.Q<ListView>("ScoreList");

        listView.makeItem = () => listItemAsset.Instantiate();
        listView.bindItem = BindItem;
        
    }

    private void Start()
    {
        Debug.Log("OnEnable 1");
        Scoreboard.instance.AddScoreboardPointsListener(this);
        Debug.Log("OnEnable 2");
        populateListView();
    }

    void populateListView()
    {
        // Create a list of data. In this case, numbers from 1 to 1000.
        int itemCount = Scoreboard.instance.playerScores.Count;
        Debug.Log("OnEnable 3");
        items = new List<ScoreItem>(itemCount);
        foreach (var item in Scoreboard.instance.playerScores)
        {
            Debug.Log("Added player to scoreboard:" + item.Key.playerName);
            items.Add(new ScoreItem(item.Key, item.Value));
        }
            
        
        listView.itemsSource = items;

        listView.onItemsChosen += objects => Debug.Log(objects);
        listView.onSelectionChange += objects => Debug.Log(objects);
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
        populateListView();
    }
    
    public void PlayerRemvoved(Player player) {
        populateListView();
    }

    public void AddPointsToPlayer(Player player, int points) {
        populateListView();
    }
}

