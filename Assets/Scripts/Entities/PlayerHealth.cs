using System;
using UnityEngine;
using UnityEngine.Events;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public static event Action GameOverRaised;

        [SerializeField] private UnityEvent _onGameOver;

        private bool _isDead;

        public void TakeDamage(int amount)
        {
            if (_isDead) return;
            if (amount <= 0) return;

            ResolveHit();
        }

        private void ResolveHit()
        {
            RunStatsManager runStats = RunStatsManager.Instance;
            if (runStats == null)
            {
                Debug.LogError($"[{nameof(PlayerHealth)}] Cannot resolve player damage without a {nameof(RunStatsManager)}.");
                return;
            }

            if (IsShieldActive(runStats))
            {
                Debug.Log("Player hit blocked by shield.");
                return;
            }

            if (runStats.TryConsumeArmour())
            {
                Debug.Log($"Player hit. Armour remaining: {runStats.Armour}");
                return;
            }

            runStats.DowngradeWeaponTier();
            bool outOfLives = runStats.LoseLife();
            Debug.Log($"Player hit. Lives remaining: {runStats.Lives}");

            if (outOfLives)
            {
                RaiseGameOver();
            }
        }

        private void RaiseGameOver()
        {
            if (_isDead) return;

            _isDead = true;
            GameManager.Instance?.EnterGameOver();
            GameOverRaised?.Invoke();
            _onGameOver?.Invoke();
        }

        private bool IsShieldActive(RunStatsManager runStats)
        {
            if (BuffManager.Instance != null)
            {
                return BuffManager.Instance.IsShieldActive;
            }

            return runStats.IsShieldActive;
        }
    }
}
