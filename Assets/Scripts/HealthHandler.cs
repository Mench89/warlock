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
        IsDead.OnValueChanged += SetIsDeadServerRpc;
    }

    [ServerRpc]
    public void ResetHealthServerRpc()
    {
        CurrentHealth.Value = MaxHealth;
        IsDead.Value = false;
    }

    [ServerRpc]
    public void ApplyDamageServerRpc(int damage)
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

    [ServerRpc]
    public void SetIsDeadServerRpc(bool oldValue, bool newValue)
    {
        // Only report when we've just died
        if (!oldValue && newValue)
        {
            OnDeathDelegate?.Invoke();
        }
    }
}
