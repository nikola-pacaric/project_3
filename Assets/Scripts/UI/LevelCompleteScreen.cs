using System.Collections;
using TMPro;
using UnityEngine;
using Warblade.Data.Events;
using Warblade.Managers;

namespace Warblade.UI
{
    public class LevelCompleteScreen : MonoBehaviour
    {
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private IntEventChannel _levelStarted;
        [SerializeField] private IntEventChannel _levelCompleted;
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private string _readyMessageFormat = "Get ready!\nLevel {0}";
        [SerializeField] private string _messageFormat = "Level {0} Complete";
        [SerializeField] private string _bonusScoreMessageFormat = "Congratulations!\n{0} Bonus Points!";
        [SerializeField, Min(0f)] private float _visibleDuration = 1.5f;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.5f;

        private Coroutine _showRoutine;

        private void Awake()
        {
            HideImmediate();
        }

        private void OnEnable()
        {
            if (_levelManager == null)
            {
                _levelManager = LevelManager.Instance;
            }

            if (_levelCompleted != null)
            {
                _levelCompleted.OnEventRaised += HandleLevelCompleted;
            }
            else if (_levelManager != null)
            {
                _levelManager.OnLevelCompleted.AddListener(HandleLevelCompleted);
            }

            if (_levelStarted != null)
            {
                _levelStarted.OnEventRaised += HandleLevelStarted;
            }
            else if (_levelManager != null)
            {
                _levelManager.OnLevelStarted.AddListener(HandleLevelStarted);
            }
        }

        private void OnDisable()
        {
            if (_levelCompleted != null)
            {
                _levelCompleted.OnEventRaised -= HandleLevelCompleted;
            }
            else if (_levelManager != null)
            {
                _levelManager.OnLevelCompleted.RemoveListener(HandleLevelCompleted);
            }

            if (_levelStarted != null)
            {
                _levelStarted.OnEventRaised -= HandleLevelStarted;
            }
            else if (_levelManager != null)
            {
                _levelManager.OnLevelStarted.RemoveListener(HandleLevelStarted);
            }
        }

        public void HandleLevelStarted(int startedLevel)
        {
            ShowMessage(string.Format(_readyMessageFormat, startedLevel));
        }

        public void HandleLevelCompleted(int completedLevel)
        {
            int bonusScore = _levelManager != null
                ? _levelManager.LastLevelCompletionBonusScore
                : 0;

            if (bonusScore > 0)
            {
                ShowMessage(string.Format(_bonusScoreMessageFormat, bonusScore));
                return;
            }

            ShowMessage(string.Format(_messageFormat, completedLevel));
        }

        private void ShowMessage(string message)
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
            }

            _showRoutine = StartCoroutine(ShowRoutine(message));
        }

        private IEnumerator ShowRoutine(string message)
        {
            SetVisible(true);
            SetAlpha(1f);
            if (_messageText != null)
            {
                _messageText.text = message;
            }

            if (_visibleDuration > 0f)
            {
                yield return new WaitForSeconds(_visibleDuration);
            }

            if (_fadeDuration > 0f && _canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < _fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / _fadeDuration);
                    SetAlpha(1f - t);
                    yield return null;
                }
            }

            SetAlpha(0f);
            SetVisible(false);
            _showRoutine = null;
        }

        private void HideImmediate()
        {
            if (_showRoutine != null)
            {
                StopCoroutine(_showRoutine);
                _showRoutine = null;
            }

            SetAlpha(0f);
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            bool canToggleRootObject = _root != null && _root != gameObject;
            if (canToggleRootObject)
            {
                _root.SetActive(isVisible);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = isVisible;
                _canvasGroup.blocksRaycasts = isVisible;
            }
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
        }
    }
}
