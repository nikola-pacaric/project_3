using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Warblade.UI
{
    [DisallowMultipleComponent]
    public class ScreenFadeController : MonoBehaviour
    {
        private const string RuntimeObjectName = "RuntimeScreenFade";
        private const int FadeSortingOrder = 32767;

        private static ScreenFadeController _runtimeInstance;

        [SerializeField] private Canvas _canvas;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Graphic _fadeGraphic;

        private Coroutine _fadeRoutine;

        public static ScreenFadeController RuntimeInstance
        {
            get
            {
                if (_runtimeInstance != null)
                {
                    return _runtimeInstance;
                }

                return CreateRuntimeInstance();
            }
        }

        private void Awake()
        {
            if (_runtimeInstance != null && _runtimeInstance != this)
            {
                Destroy(gameObject);
                return;
            }

            _runtimeInstance = this;
            ResolveReferences();
            SetAlpha(0f);
            SetBlocking(false);
        }

        private void OnDestroy()
        {
            if (_runtimeInstance == this)
            {
                _runtimeInstance = null;
            }
        }

        public void PlayFadeThroughBlack(
            float fadeInDuration,
            float holdDuration,
            float fadeOutDuration,
            Action onBlack,
            Action onComplete = null)
        {
            ResolveReferences();

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            _fadeRoutine = StartCoroutine(RunFadeThroughBlack(
                fadeInDuration,
                holdDuration,
                fadeOutDuration,
                onBlack,
                onComplete));
        }

        private IEnumerator RunFadeThroughBlack(
            float fadeInDuration,
            float holdDuration,
            float fadeOutDuration,
            Action onBlack,
            Action onComplete)
        {
            SetBlocking(true);
            yield return FadeAlpha(0f, 1f, fadeInDuration);

            SetAlpha(1f);
            onBlack?.Invoke();

            if (holdDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(holdDuration);
            }

            yield return FadeAlpha(1f, 0f, fadeOutDuration);
            SetAlpha(0f);
            SetBlocking(false);

            _fadeRoutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator FadeAlpha(float startAlpha, float targetAlpha, float duration)
        {
            if (duration <= Mathf.Epsilon)
            {
                SetAlpha(targetAlpha);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, EaseInOut(t)));
                yield return null;
            }

            SetAlpha(targetAlpha);
        }

        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        private void SetBlocking(bool isBlocking)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.interactable = isBlocking;
            _canvasGroup.blocksRaycasts = isBlocking;
        }

        private void ResolveReferences()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_fadeGraphic == null)
            {
                _fadeGraphic = GetComponent<Graphic>();
            }

            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.overrideSorting = true;
                _canvas.sortingOrder = FadeSortingOrder;
            }

            if (_fadeGraphic != null)
            {
                _fadeGraphic.color = Color.black;
                _fadeGraphic.raycastTarget = true;
            }
        }

        private static ScreenFadeController CreateRuntimeInstance()
        {
            GameObject fadeObject = new GameObject(
                RuntimeObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup),
                typeof(Image));

            RectTransform rectTransform = fadeObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            ScreenFadeController controller = fadeObject.AddComponent<ScreenFadeController>();
            controller.ResolveReferences();
            controller.SetAlpha(0f);
            controller.SetBlocking(false);
            return controller;
        }

        private static float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
