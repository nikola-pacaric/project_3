using System;
using UnityEngine;
using UnityEngine.Events;
using Warblade.Data;
using Warblade.Data.Events;

namespace Warblade.Systems
{
    /// <summary>
    /// Bridges a weapon-tier ScriptableObject event channel to inspector-wired UnityEvent responses.
    /// </summary>
    public class WeaponTierEventChannelListener : MonoBehaviour
    {
        [Serializable]
        public class WeaponTierUnityEvent : UnityEvent<WeaponTier> { }

        [SerializeField] private WeaponTierEventChannel _channel;
        [SerializeField] private WeaponTierUnityEvent _response;

        private void OnEnable()
        {
            if (_channel != null)
            {
                _channel.OnEventRaised += Respond;
            }
        }

        private void OnDisable()
        {
            if (_channel != null)
            {
                _channel.OnEventRaised -= Respond;
            }
        }

        private void Respond(WeaponTier weaponTier)
        {
            _response?.Invoke(weaponTier);
        }
    }
}
