using System;
using UnityEngine;
using UnityEngine.Events;

namespace Warblade.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for boss health changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Warblade/Events/Boss Health Event Channel", fileName = "BossHealthEventChannel")]
    public class BossHealthEventChannel : ScriptableObject
    {
        [Serializable]
        public class BossHealthUnityEvent : UnityEvent<int, int> { }

        [SerializeField] private BossHealthUnityEvent _onEventRaised;

        public event Action<int, int> OnEventRaised;

        /// <summary>
        /// Raises the event with current health and max health.
        /// </summary>
        public void Raise(int currentHealth, int maxHealth)
        {
            OnEventRaised?.Invoke(currentHealth, maxHealth);
            _onEventRaised?.Invoke(currentHealth, maxHealth);
        }
    }
}
