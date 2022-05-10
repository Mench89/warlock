using UnityEngine;

public class FloatingNameLabel : MonoBehaviour
{
    [SerializeField] public GameObject objectToFloatAbove;
    [SerializeField] public string nameToShow;
    private TextMesh textMesh;

    // Start is called before the first frame update
    void Start()
    {
        gameObject.transform.rotation = Camera.main.transform.rotation; // Causes the text faces camera.
        textMesh = gameObject.AddComponent<TextMesh>();
        textMesh.text = GetName();
        textMesh.color = new Color(0.2f, 0.7f, 0.2f);
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;

        textMesh.characterSize = 0.0325f;
        textMesh.fontSize = 30;
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: Add interface to be notified about player name change
        textMesh.text = GetName();
        gameObject.transform.rotation = Camera.main.transform.rotation; // Causes the text faces camera.
        transform.position = objectToFloatAbove.transform.position + Vector3.up * 0.5f;
    }

    private string GetName()
    {
        if (nameToShow.Length > 0)
        {
            return nameToShow;
        }
        Player player = GetComponentInParent<Player>();
        if (player != null)
        {
            return player.playerName;
        }
        return "Unkown";
    }
}
