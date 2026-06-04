using UnityEngine;
using Warblade.Data;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        private Enemy _enemy;
        private int _currentHealth;
        private int _contactDamage;

        internal void Initialize(Enemy enemy, int contactDamage)
        {
            _enemy = enemy;
            _contactDamage = contactDamage;
        }

        internal void Spawn(int maxHealth, int contactDamage)
        {
            _currentHealth = Mathf.Max(1, maxHealth);
            _contactDamage = contactDamage;
        }

        public void TakeDamage(int amount)
        {
            if (_enemy == null || _enemy.HasDespawned) return;

            _currentHealth -= amount;
            if (_currentHealth <= 0)
            {
                _enemy.Die();
                return;
            }

            AudioManager.Instance?.PlayOneShot(AudioCue.EnemyHit);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_enemy == null || _enemy.HasDespawned) return;

            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(_contactDamage);
            }
        }
    }
}
