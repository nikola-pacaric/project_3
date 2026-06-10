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
        [Header("Selection")]
        [SerializeField] private GameObject _defaultSelected;
        [SerializeField] private GameObject _settingsDefaultSelected;

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
            GameManager.Instance?.EnterPlaying();
            UiSelectionHelper.ClearSelectionStack();
        }

        public void RestartRun()
        {
            GameManager.Instance?.RestartRun();
            UiSelectionHelper.ClearSelectionStack();
        }

        public void OpenSettings()
        {
            SetSettingsVisible(true);
            UiSelectionHelper.PushSelectionAndSelectNextFrame(this, _settingsDefaultSelected, _defaultSelected);
        }

        public void ReturnToMainMenu()
        {
            GameManager.Instance?.RestartToMainMenu();
            UiSelectionHelper.ClearSelectionStack();
        }

        private void HandleGameStateChanged(GameState gameState)
        {
            bool isPaused = gameState == GameState.Paused;
            SetVisible(isPaused);

            if (isPaused)
            {
                SetSettingsVisible(false);
                UiSelectionHelper.ClearSelectionStack();
                UiSelectionHelper.SelectNextFrame(this, _defaultSelected);
            }
            else
            {
                SetSettingsVisible(false);
                UiSelectionHelper.ClearSelectionStack();
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

            if (isVisible)
            {
                UiSelectionHelper.ApplyPanelNavigation(_root);
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

                if (isVisible)
                {
                    UiSelectionHelper.ApplySelectableAudioFeedback(_settingsPanel);
                    UiSelectionHelper.ApplyPanelNavigation(_settingsPanel);
                }
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
