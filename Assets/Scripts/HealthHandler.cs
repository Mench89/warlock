using UnityEngine;
using Unity.Netcode;

public interface IHealthEvents
{
    void OnDeath();
}

public class HealthHandler : NetworkBehaviour
{
    [SerializeField] public int MaxHealth = 10;
    public NetworkVariable<bool> IsDead = new(false);

    public delegate void OnDamageTaken(int damage);
    public OnDamageTaken OnDamageTakenDelegate;
    public delegate void OnDeath();
    public OnDeath OnDeathDelegate;

    public NetworkVariable<int> CurrentHealth = new(1);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CurrentHealth.Value = MaxHealth;
        IsDead.OnValueChanged += SetIsDead;
    }

    //[Server]
    public void ResetHealth()
    {
        CurrentHealth.Value = MaxHealth;
        IsDead.Value = false;
    }

    //[Server]
    public void ApplyDamage(int damage)
    {
        CurrentHealth.Value -= damage;
        Debug.Log(damage + " damage taken!");
        if (CurrentHealth.Value > 0)
        {
            if (OnDamageTakenDelegate != null)
            {
                OnDamageTakenDelegate(damage);
            }
        } else {
            CurrentHealth.Value = 0;
            Debug.Log("Object is dead!");
            IsDead.Value = true;
        }
    }

    //[Server]
    public void SetIsDead(bool oldValue, bool newValue)
    {
        // Only report when we've just died
        if (!oldValue && newValue)
        {
            OnDeathDelegate?.Invoke();
        }
    }
}
