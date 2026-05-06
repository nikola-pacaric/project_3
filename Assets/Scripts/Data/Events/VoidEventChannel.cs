using System;
using UnityEngine;
using UnityEngine.Events;

namespace Warblade.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for signals that do not need payload data.
    /// </summary>
    [CreateAssetMenu(menuName = "Warblade/Events/Void Event Channel", fileName = "VoidEventChannel")]
    public class VoidEventChannel : ScriptableObject
    {
        [SerializeField] private UnityEvent _onEventRaised;

        public event Action OnEventRaised;

        /// <summary>
        /// Raises the event for C# subscribers and inspector-wired UnityEvent listeners.
        /// </summary>
        public void Raise()
        {
            OnEventRaised?.Invoke();
            _onEventRaised?.Invoke();
        }
    }
}
