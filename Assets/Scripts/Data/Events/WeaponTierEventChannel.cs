using System;
using UnityEngine;
using UnityEngine.Events;
using Warblade.Data;

namespace Warblade.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for current weapon tier changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Warblade/Events/Weapon Tier Event Channel", fileName = "WeaponTierEventChannel")]
    public class WeaponTierEventChannel : ScriptableObject
    {
        [Serializable]
        public class WeaponTierUnityEvent : UnityEvent<WeaponTier> { }

        [SerializeField] private WeaponTierUnityEvent _onEventRaised;

        public event Action<WeaponTier> OnEventRaised;

        /// <summary>
        /// Raises the event with the current weapon tier.
        /// </summary>
        public void Raise(WeaponTier weaponTier)
        {
            OnEventRaised?.Invoke(weaponTier);
            _onEventRaised?.Invoke(weaponTier);
        }
    }
}
