using System.Collections;
using UnityEngine;

namespace Warblade.UI
{
    public class UiPanelTransition : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panel;
        [SerializeField] private Vector2 _hiddenOffset = new Vector2(0f, 160f);
        [SerializeField] private Vector3 _hiddenScale = new Vector3(0.96f, 0.96f, 1f);
        [SerializeField] private float _showDuration = 0.48f;
        [SerializeField] private float _hideDuration = 0.24f;

        private Vector2 _shownPosition;
        private Vector3 _shownScale;
        private Coroutine _transitionRoutine;
        private bool _hasCachedShownTransform;

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_panel == null)
            {
                _panel = transform as RectTransform;
            }

            CacheShownTransform();
        }

        public void Configure(GameObject root, CanvasGroup canvasGroup, RectTransform panel)
        {
            _root = root;
            _canvasGroup = canvasGroup;
            _panel = panel;
            _hasCachedShownTransform = false;
            CacheShownTransform();
        }

        public void Show()
        {
            CacheShownTransform();
            RunTransition(true);
        }

        public void Hide()
        {
            CacheShownTransform();
            RunTransition(false);
        }

        public void ShowImmediate()
        {
            CacheShownTransform();
            StopActiveTransition();
            SetRootActive(true);
            ApplyVisualState(1f, _shownPosition, _shownScale);
            SetInteraction(true);
        }

        public void HideImmediate()
        {
            CacheShownTransform();
            StopActiveTransition();
            ApplyVisualState(0f, _shownPosition + _hiddenOffset, _hiddenScale);
            SetInteraction(false);
            SetRootActive(false);
        }

        private void RunTransition(bool isShowing)
        {
            if (isShowing)
            {
                SetRootActive(true);
            }

            StopActiveTransition();

            if (!isActiveAndEnabled)
            {
                if (isShowing)
                {
                    ShowImmediate();
                }
                else
                {
                    HideImmediate();
                }

                return;
            }

            _transitionRoutine = StartCoroutine(RunTransitionRoutine(isShowing));
        }

        private IEnumerator RunTransitionRoutine(bool isShowing)
        {
            SetInteraction(false);

            float duration = Mathf.Max(0.01f, isShowing ? _showDuration : _hideDuration);
            float elapsed = 0f;
            float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : isShowing ? 0f : 1f;
            float targetAlpha = isShowing ? 1f : 0f;

            Vector2 hiddenPosition = _shownPosition + _hiddenOffset;
            Vector2 startPosition = _panel != null ? _panel.anchoredPosition : _shownPosition;
            Vector2 targetPosition = isShowing ? _shownPosition : hiddenPosition;

            Vector3 startScale = _panel != null ? _panel.localScale : _shownScale;
            Vector3 targetScale = isShowing ? _shownScale : _hiddenScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutCubic(t);

                ApplyVisualState(
                    Mathf.Lerp(startAlpha, targetAlpha, eased),
                    Vector2.Lerp(startPosition, targetPosition, eased),
                    Vector3.Lerp(startScale, targetScale, eased));

                yield return null;
            }

            ApplyVisualState(targetAlpha, targetPosition, targetScale);
            SetInteraction(isShowing);

            if (!isShowing)
            {
                SetRootActive(false);
            }

            _transitionRoutine = null;
        }

        private void ApplyVisualState(float alpha, Vector2 anchoredPosition, Vector3 scale)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }

            if (_panel != null)
            {
                _panel.anchoredPosition = anchoredPosition;
                _panel.localScale = scale;
            }
        }

        private void SetInteraction(bool isInteractive)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.interactable = isInteractive;
            _canvasGroup.blocksRaycasts = isInteractive;
        }

        private void SetRootActive(bool isActive)
        {
            if (_root != null)
            {
                _root.SetActive(isActive);
            }
        }

        private void CacheShownTransform()
        {
            if (_hasCachedShownTransform || _panel == null) return;

            _shownPosition = _panel.anchoredPosition;
            _shownScale = _panel.localScale;
            _hasCachedShownTransform = true;
        }

        private void StopActiveTransition()
        {
            if (_transitionRoutine == null) return;

            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }

        private static float EaseOutCubic(float t)
        {
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }
    }
}
