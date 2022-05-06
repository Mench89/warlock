using UnityEngine;
using Mirror;

public interface IHealthEvents
{
    void OnDeath();
}

public class HealthHandler : NetworkBehaviour
{
    [SerializeField] public int MaxHealth = 10;
    [SyncVar] public bool IsDead;

    public delegate void OnDeath();
    public OnDeath OnDeathDelegate;

    public int CurrentHealth
    {
        get;
        private set;
    }

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = MaxHealth;
    }

    public void ApplyDamage(int damage)
    {
        CurrentHealth -= damage;
        Debug.Log(damage + " damage taken!");
        if (CurrentHealth <= 0) {
            CurrentHealth = 0;
            Debug.Log("Object is dead!");
            IsDead = true;
            if (OnDeathDelegate != null)
            {
                OnDeathDelegate();
            }
        }

    }
}
