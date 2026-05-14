using System;
using UnityEngine;
using UnityEngine.Events;

namespace Warblade.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for boss lifecycle events.
    /// </summary>
    [CreateAssetMenu(menuName = "Warblade/Events/Boss Data Event Channel", fileName = "BossDataEventChannel")]
    public class BossDataEventChannel : ScriptableObject
    {
        [Serializable]
        public class BossDataUnityEvent : UnityEvent<BossData> { }

        [SerializeField] private BossDataUnityEvent _onEventRaised;

        public event Action<BossData> OnEventRaised;

        /// <summary>
        /// Raises the event with the boss data asset for C# and inspector-wired listeners.
        /// </summary>
        public void Raise(BossData bossData)
        {
            OnEventRaised?.Invoke(bossData);
            _onEventRaised?.Invoke(bossData);
        }
    }
}
