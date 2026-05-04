using System;
using UnityEngine;
using UnityEngine.Events;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public static event Action GameOverRaised;

        [SerializeField] private int _maxLives = 3;
        [SerializeField] private UnityEvent _onGameOver;

        private int _currentLives;
        private bool _isDead;

        private void Awake()
        {
            _currentLives = _maxLives;
        }

        public void TakeDamage(int amount)
        {
            if (_isDead) return;
            LoseLife();
        }

        private void LoseLife()
        {
            _currentLives--;
            Debug.Log($"Player hit. Lives remaining: {_currentLives}");

            if (_currentLives <= 0)
            {
                _isDead = true;
                GameOverRaised?.Invoke();
                _onGameOver?.Invoke();
            }
        }
    }
}
