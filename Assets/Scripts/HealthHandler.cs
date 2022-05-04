using UnityEngine;

public interface IHealthEvents
{
    void OnDeath();
}

public class HealthHandler : MonoBehaviour
{
    [SerializeField] public int MaxHealth = 5;

    public delegate void OnDeath();
    public OnDeath OnDeathDelegate;

    public int CurrentHealth
    {
        get;
        private set;
    }

    // TODO: Should we handle "Death" event here, or delegate it?

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = MaxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ApplyDamage(int damage)
    {
        CurrentHealth -= damage;
        Debug.Log(damage + " damage taken!");
        if (CurrentHealth <= 0) {
            CurrentHealth = 0;
            Debug.Log("Object is dead!");
            if (OnDeathDelegate != null)
            {
                OnDeathDelegate();
            }
        }

    }
}
