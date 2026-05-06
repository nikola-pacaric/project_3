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

        [SerializeField, Min(1)] private int _fallbackMaxLives = 3;
        [SerializeField] private UnityEvent _onGameOver;

        private int _fallbackCurrentLives;
        private bool _isDead;

        private void Awake()
        {
            _fallbackCurrentLives = _fallbackMaxLives;
        }

        public void TakeDamage(int amount)
        {
            if (_isDead) return;
            LoseLife();
        }

        private void LoseLife()
        {
            if (RunStatsManager.Instance != null)
            {
                if (RunStatsManager.Instance.TryConsumeArmour())
                {
                    Debug.Log($"Player hit. Armour remaining: {RunStatsManager.Instance.Armour}");
                    return;
                }

                RunStatsManager.Instance.DowngradeWeaponTier();
                bool outOfLives = RunStatsManager.Instance.LoseLife();
                Debug.Log($"Player hit. Lives remaining: {RunStatsManager.Instance.Lives}");

                if (outOfLives)
                {
                    RaiseGameOver();
                }

                return;
            }

            _fallbackCurrentLives--;
            Debug.Log($"Player hit. Lives remaining: {_fallbackCurrentLives}");

            if (_fallbackCurrentLives <= 0)
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
    }
}
