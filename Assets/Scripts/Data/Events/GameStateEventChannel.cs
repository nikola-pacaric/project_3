using System;
using UnityEngine;
using UnityEngine.Events;
using Warblade.Data;

namespace Warblade.Data.Events
{
    /// <summary>
    /// ScriptableObject event channel for top-level game state changes.
    /// </summary>
    [CreateAssetMenu(menuName = "Warblade/Events/Game State Event Channel", fileName = "GameStateEventChannel")]
    public class GameStateEventChannel : ScriptableObject
    {
        [Serializable]
        public class GameStateUnityEvent : UnityEvent<GameState> { }

        [SerializeField] private GameStateUnityEvent _onEventRaised;

        public event Action<GameState> OnEventRaised;

        /// <summary>
        /// Raises the event with the current game state.
        /// </summary>
        public void Raise(GameState state)
        {
            OnEventRaised?.Invoke(state);
            _onEventRaised?.Invoke(state);
        }
    }
}
