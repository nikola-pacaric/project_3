using System;
using UnityEngine;
using UnityEngine.Events;

namespace Warblade.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for boss phase changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Warblade/Events/Boss Phase Event Channel", fileName = "BossPhaseEventChannel")]
    public class BossPhaseEventChannel : ScriptableObject
    {
        [Serializable]
        public class BossPhaseUnityEvent : UnityEvent<BossPhaseData> { }

        [SerializeField] private BossPhaseUnityEvent _onEventRaised;

        public event Action<BossPhaseData> OnEventRaised;

        /// <summary>
        /// Raises the event with the active boss phase data.
        /// </summary>
        public void Raise(BossPhaseData bossPhaseData)
        {
            OnEventRaised?.Invoke(bossPhaseData);
            _onEventRaised?.Invoke(bossPhaseData);
        }
    }
}
