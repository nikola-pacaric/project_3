using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Systems;

namespace Warblade.Managers
{
    /// <summary>
    /// Owns the top-level game state for gameplay, pause, shop, and game-over flow.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private InputReader _input;
        [SerializeField] private GameState _startingState = GameState.Playing;
        [Header("Events")]
        [SerializeField] private GameStateEventChannel _gameStateChanged;

        public GameState CurrentState { get; private set; }
        public bool IsPlaying => CurrentState == GameState.Playing;
        public bool IsPaused => CurrentState == GameState.Paused;
        public bool IsShopOpen => CurrentState == GameState.Shop;
        public bool IsGameOver => CurrentState == GameState.GameOver;

        public event Action<GameState> StateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            SetState(_startingState, true);
        }

        private void OnEnable()
        {
            if (_input != null)
            {
                _input.PausePressed += HandlePausePressed;
            }
        }

        private void OnDisable()
        {
            if (_input != null)
            {
                _input.PausePressed -= HandlePausePressed;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Returns gameplay to the active playing state.
        /// </summary>
        public void EnterPlaying()
        {
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Enters pause only from active gameplay.
        /// </summary>
        public void EnterPaused()
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.Paused);
        }

        /// <summary>
        /// Enters the shop state from any non-game-over state.
        /// </summary>
        public void EnterShop()
        {
            if (CurrentState == GameState.GameOver) return;
            if (CurrentState == GameState.Shop) return;

            SetState(GameState.Shop);
            AudioManager.Instance?.PlayOneShot(AudioCue.ShopOpen);
        }

        /// <summary>
        /// Leaves the shop and returns to active gameplay.
        /// </summary>
        public void LeaveShop()
        {
            if (CurrentState != GameState.Shop) return;

            SetState(GameState.Playing);
            AudioManager.Instance?.PlayOneShot(AudioCue.ShopLeave);
        }

        /// <summary>
        /// Enters game over and resets run-only stats.
        /// </summary>
        public void EnterGameOver()
        {
            if (CurrentState == GameState.GameOver) return;

            BuffManager.Instance?.ClearAllBuffs();
            RunStatsManager.Instance?.ResetRun();
            SetState(GameState.GameOver);
            AudioManager.Instance?.PlayMusicIfConfigured(AudioCue.MusicGameOver);
            AudioManager.Instance?.PlayOneShot(AudioCue.GameOver);
        }

        /// <summary>
        /// Toggles between active gameplay and pause. Shop and game-over state ignore pause toggles.
        /// </summary>
        public void TogglePause()
        {
            if (CurrentState == GameState.Playing)
            {
                SetState(GameState.Paused);
                AudioManager.Instance?.PlayOneShot(AudioCue.Pause);
            }
            else if (CurrentState == GameState.Paused)
            {
                SetState(GameState.Playing);
                AudioManager.Instance?.PlayOneShot(AudioCue.Pause);
            }
        }

        private void HandlePausePressed()
        {
            if (CurrentState == GameState.Shop)
            {
                LeaveShop();
                return;
            }

            TogglePause();
        }

        /// <summary>
        /// Resets run and score state, restores time scale, and reloads the active scene.
        /// </summary>
        public void RestartCurrentScene()
        {
            BuffManager.Instance?.ClearAllBuffs();
            RunStatsManager.Instance?.ResetRun();
            ScoreManager.Instance?.ResetScore();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void SetState(GameState newState, bool force = false)
        {
            if (!force && CurrentState == newState) return;

            CurrentState = newState;
            Time.timeScale = newState == GameState.Playing ? 1f : 0f;
            StateChanged?.Invoke(CurrentState);
            _gameStateChanged?.Raise(CurrentState);
        }
    }
}
