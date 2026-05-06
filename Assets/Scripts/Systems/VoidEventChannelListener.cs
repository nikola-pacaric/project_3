using UnityEngine;
using UnityEngine.Events;
using Warblade.Data.Events;

namespace Warblade.Systems
{
    /// <summary>
    /// Bridges a void ScriptableObject event channel to inspector-wired UnityEvent responses.
    /// </summary>
    public class VoidEventChannelListener : MonoBehaviour
    {
        [SerializeField] private VoidEventChannel _channel;
        [SerializeField] private UnityEvent _response;

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

        private void Respond()
        {
            _response?.Invoke();
        }
    }
}
