using System;
using UnityEngine;
using UnityEngine.Events;
using Warblade.Data.Events;

namespace Warblade.Systems
{
    /// <summary>
    /// Bridges an int ScriptableObject event channel to inspector-wired UnityEvent responses.
    /// </summary>
    public class IntEventChannelListener : MonoBehaviour
    {
        [Serializable]
        public class IntUnityEvent : UnityEvent<int> { }

        [SerializeField] private IntEventChannel _channel;
        [SerializeField] private IntUnityEvent _response;

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

        private void Respond(int value)
        {
            _response?.Invoke(value);
        }
    }
}
