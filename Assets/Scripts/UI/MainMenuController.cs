using UnityEngine;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Managers;

namespace Warblade.UI
{
    [DisallowMultipleComponent]
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _leaderboardPanel;
        [SerializeField] private GameStateEventChannel _gameStateChanged;

        private bool _isSubscribedToGameManager;

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            ResolveCanvasGroup();
            HideSubPanels();
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

        public void StartGame()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            HideSubPanels();
            GameManager.Instance?.StartNewRun();
        }

        public void OpenSettings()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            SetLeaderboardVisible(false);
            SetSettingsVisible(true);
        }

        public void CloseSettings()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            SetSettingsVisible(false);
        }

        public void OpenLeaderboard()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            SetSettingsVisible(false);
            SetLeaderboardVisible(true);
        }

        public void CloseLeaderboard()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            SetLeaderboardVisible(false);
        }

        private void HandleGameStateChanged(GameState gameState)
        {
            bool isMainMenu = gameState == GameState.MainMenu;
            SetVisible(isMainMenu);

            if (!isMainMenu)
            {
                HideSubPanels();
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
                _settingsPanel.SetActive(isVisible);
            }
        }

        private void SetLeaderboardVisible(bool isVisible)
        {
            if (_leaderboardPanel != null)
            {
                _leaderboardPanel.SetActive(isVisible);
            }
        }

        private void HideSubPanels()
        {
            SetSettingsVisible(false);
            SetLeaderboardVisible(false);
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
