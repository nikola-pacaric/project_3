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
        [Header("Selection")]
        [SerializeField] private GameObject _defaultSelected;
        [SerializeField] private GameObject _settingsDefaultSelected;
        [SerializeField] private GameObject _leaderboardDefaultSelected;
        [Header("Start Transition")]
        [SerializeField, Min(0f)] private float _startFadeInDuration = 0.18f;
        [SerializeField, Min(0f)] private float _startFadeHoldDuration = 0.06f;
        [SerializeField, Min(0f)] private float _startFadeOutDuration = 0.24f;

        private bool _isSubscribedToGameManager;
        private bool _isStartingGame;

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
            if (_isStartingGame)
            {
                return;
            }

            _isStartingGame = true;
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            HideSubPanels();
            SetMenuInteraction(false);
            UiSelectionHelper.ClearSelectionStack();

            ScreenFadeController.RuntimeInstance.PlayFadeThroughBlack(
                _startFadeInDuration,
                _startFadeHoldDuration,
                _startFadeOutDuration,
                StartNewRunAfterFadeIn,
                HandleStartFadeComplete);
        }

        public void OpenSettings()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            SetLeaderboardVisible(false);
            SetSettingsVisible(true);
            UiSelectionHelper.PushSelectionAndSelectNextFrame(this, _settingsDefaultSelected, _defaultSelected);
        }

        public void OpenLeaderboard()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            SetSettingsVisible(false);
            SetLeaderboardVisible(true);
            UiSelectionHelper.PushSelectionAndSelectNextFrame(this, _leaderboardDefaultSelected, _defaultSelected);
        }

        public void CloseLeaderboard()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            SetLeaderboardVisible(false);
            UiSelectionHelper.RestorePreviousSelectionNextFrame(this, _defaultSelected);
        }

        private void HandleGameStateChanged(GameState gameState)
        {
            bool isMainMenu = gameState == GameState.MainMenu;
            SetVisible(isMainMenu);

            if (!isMainMenu)
            {
                HideSubPanels();
                UiSelectionHelper.ClearSelectionStack();
                return;
            }

            UiSelectionHelper.ClearSelectionStack();
            UiSelectionHelper.SelectNextFrame(this, _defaultSelected);
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

            if (isVisible)
            {
                UiSelectionHelper.ApplyPanelNavigation(_root);
            }

            if (_canvasGroup == null) return;

            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
        }

        private void SetMenuInteraction(bool isInteractive)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.interactable = isInteractive;
            _canvasGroup.blocksRaycasts = isInteractive;
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

                if (isVisible)
                {
                    UiSelectionHelper.ApplyPanelNavigation(_settingsPanel);
                }
            }
        }

        private void SetLeaderboardVisible(bool isVisible)
        {
            if (_leaderboardPanel != null)
            {
                if (isVisible)
                {
                    _leaderboardPanel.transform.SetAsLastSibling();
                }

                _leaderboardPanel.SetActive(isVisible);

                if (isVisible)
                {
                    UiSelectionHelper.ApplyPanelNavigation(_leaderboardPanel);
                }
            }
        }

        private void HideSubPanels()
        {
            SetSettingsVisible(false);
            SetLeaderboardVisible(false);
        }

        private void StartNewRunAfterFadeIn()
        {
            GameManager.Instance?.StartNewRun();
        }

        private void HandleStartFadeComplete()
        {
            _isStartingGame = false;

            if (GameManager.Instance == null || GameManager.Instance.IsMainMenu)
            {
                SetMenuInteraction(true);
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
