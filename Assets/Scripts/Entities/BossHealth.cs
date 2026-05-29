using UnityEngine;
using Warblade.Data;
using Warblade.Systems;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class BossHealth : MonoBehaviour, IDamageable
    {
        private Boss _boss;
        private int _currentHealth;

        internal int CurrentHealth => _currentHealth;

        internal void Initialize(Boss boss)
        {
            _boss = boss;
        }

        internal void Spawn(int maxHealth)
        {
            _currentHealth = Mathf.Max(1, maxHealth);
        }

        internal void SetCurrentHealth(int currentHealth, int maxHealth)
        {
            _currentHealth = Mathf.Clamp(currentHealth, 0, Mathf.Max(1, maxHealth));
        }

        public void TakeDamage(int amount)
        {
            if (_boss == null || !_boss.CanTakeDamage || amount <= 0)
            {
                return;
            }

            _currentHealth = Mathf.Max(0, _currentHealth - amount);
            _boss.RaiseHealthChanged();

            if (_currentHealth <= 0)
            {
                _boss.Defeat();
                return;
            }

            VfxManager.Instance?.Play(VfxCue.BossHit, transform.position);
            _boss.UpdatePhase();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_boss == null || !_boss.CanDealContactDamage)
            {
                return;
            }

            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(_boss.Data.ContactDamage);
            }
        }
    }
}
