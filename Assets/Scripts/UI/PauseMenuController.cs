using UnityEngine;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Managers;

namespace Warblade.UI
{
    [DisallowMultipleComponent]
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameStateEventChannel _gameStateChanged;

        private bool _isSubscribedToGameManager;

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            ResolveCanvasGroup();
            SetSettingsVisible(false);
            SetVisible(false);
        }

        private void Start()
        {
            TrySubscribeToGameManager();
            RefreshVisibilityFromGameState();
        }

        private void OnEnable()
        {
            if (_gameStateChanged != null)
            {
                _gameStateChanged.OnEventRaised += HandleGameStateChanged;
            }

            TrySubscribeToGameManager();
            RefreshVisibilityFromGameState();
        }

        private void OnDisable()
        {
            if (_gameStateChanged != null)
            {
                _gameStateChanged.OnEventRaised -= HandleGameStateChanged;
            }

            UnsubscribeFromGameManager();
        }

        public void Resume()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            GameManager.Instance?.EnterPlaying();
        }

        public void RestartRun()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            GameManager.Instance?.RestartRun();
        }

        public void OpenSettings()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            SetSettingsVisible(true);
        }

        public void ReturnToMainMenu()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            GameManager.Instance?.RestartToMainMenu();
        }

        private void HandleGameStateChanged(GameState gameState)
        {
            bool isPaused = gameState == GameState.Paused;
            SetVisible(isPaused);

            if (isPaused)
            {
                SetSettingsVisible(false);
            }
            else
            {
                SetSettingsVisible(false);
            }
        }

        private void RefreshVisibilityFromGameState()
        {
            GameState state = GameManager.Instance != null
                ? GameManager.Instance.CurrentState
                : GameState.Playing;

            HandleGameStateChanged(state);
        }

        private void SetVisible(bool isVisible)
        {
            bool canToggleRootObject = _root != null && _root != gameObject;
            if (canToggleRootObject)
            {
                _root.SetActive(isVisible);
            }

            if (_canvasGroup == null) return;

            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
        }

        private void SetSettingsVisible(bool isVisible)
        {
            if (_settingsPanel != null)
            {
                if (isVisible)
                {
                    _settingsPanel.transform.SetAsLastSibling();
                }

                _settingsPanel.SetActive(isVisible);
            }
        }

        private void ResolveCanvasGroup()
        {
            if (_canvasGroup != null) return;

            if (_root != null)
            {
                _canvasGroup = _root.GetComponent<CanvasGroup>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void TrySubscribeToGameManager()
        {
            if (_gameStateChanged != null || _isSubscribedToGameManager || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.StateChanged += HandleGameStateChanged;
            _isSubscribedToGameManager = true;
        }

        private void UnsubscribeFromGameManager()
        {
            if (!_isSubscribedToGameManager || GameManager.Instance == null)
            {
                _isSubscribedToGameManager = false;
                return;
            }

            GameManager.Instance.StateChanged -= HandleGameStateChanged;
            _isSubscribedToGameManager = false;
        }
    }
}
