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

    public delegate void OnDamageTaken(int damage);
    public OnDamageTaken OnDamageTakenDelegate;
    public delegate void OnDeath();
    public OnDeath OnDeathDelegate;

    [SyncVar] public int CurrentHealth;

    // Start is called before the first frame update
    void Start()
    {
        CurrentHealth = MaxHealth;
    }

    [Server]
    public void ResetHealth()
    {
        CurrentHealth = MaxHealth;
        IsDead = false;
    }

    [Server]
    public void ApplyDamage(int damage)
    {
        CurrentHealth -= damage;
        Debug.Log(damage + " damage taken!");
        if (CurrentHealth > 0)
        {
            if (OnDamageTakenDelegate != null)
            {
                OnDamageTakenDelegate(damage);
            }
        } else {
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
