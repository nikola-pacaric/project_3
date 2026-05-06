using System;
using UnityEngine;
using UnityEngine.Events;
using Warblade.Data;
using Warblade.Data.Events;

namespace Warblade.Systems
{
    /// <summary>
    /// Bridges a game-state ScriptableObject event channel to inspector-wired UnityEvent responses.
    /// </summary>
    public class GameStateEventChannelListener : MonoBehaviour
    {
        [Serializable]
        public class GameStateUnityEvent : UnityEvent<GameState> { }

        [SerializeField] private GameStateEventChannel _channel;
        [SerializeField] private GameStateUnityEvent _response;

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

        private void Respond(GameState state)
        {
            _response?.Invoke(state);
        }
    }
}
