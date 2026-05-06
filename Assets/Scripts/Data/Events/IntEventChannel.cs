using System;
using UnityEngine;
using UnityEngine.Events;

namespace Warblade.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for integer payloads such as cash, lives, armour, and level numbers.
    /// </summary>
    [CreateAssetMenu(menuName = "Warblade/Events/Int Event Channel", fileName = "IntEventChannel")]
    public class IntEventChannel : ScriptableObject
    {
        [Serializable]
        public class IntUnityEvent : UnityEvent<int> { }

        [SerializeField] private IntUnityEvent _onEventRaised;

        public event Action<int> OnEventRaised;

        /// <summary>
        /// Raises the event with an integer payload for C# and inspector-wired listeners.
        /// </summary>
        public void Raise(int value)
        {
            OnEventRaised?.Invoke(value);
            _onEventRaised?.Invoke(value);
        }
    }
}
