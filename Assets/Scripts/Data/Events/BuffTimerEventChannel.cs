using System;
using UnityEngine;
using UnityEngine.Events;

namespace Warblade.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for timed buff state changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Warblade/Events/Buff Timer Event Channel", fileName = "BuffTimerEventChannel")]
    public class BuffTimerEventChannel : ScriptableObject
    {
        [Serializable]
        public class BuffTimerUnityEvent : UnityEvent<BuffType, float, float> { }

        [SerializeField] private BuffTimerUnityEvent _onEventRaised;

        public event Action<BuffType, float, float> OnEventRaised;

        /// <summary>
        /// Raises the event with buff type, remaining seconds, and total duration seconds.
        /// </summary>
        public void Raise(BuffType buffType, float remainingSeconds, float durationSeconds)
        {
            OnEventRaised?.Invoke(buffType, remainingSeconds, durationSeconds);
            _onEventRaised?.Invoke(buffType, remainingSeconds, durationSeconds);
        }
    }
}
