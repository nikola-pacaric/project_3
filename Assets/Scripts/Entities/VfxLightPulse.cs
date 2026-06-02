using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Warblade.Entities
{
    [DisallowMultipleComponent]
    public class VfxLightPulse : MonoBehaviour
    {
        [SerializeField] private Light2D _light;
        [SerializeField, Min(0f)] private float _peakIntensity = 2f;
        [SerializeField, Min(0f)] private float _peakOuterRadius = 1f;
        [SerializeField, Min(0f)] private float _peakInnerRadius = 0.1f;
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.18f;
        [SerializeField] private AnimationCurve _intensityCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [SerializeField] private AnimationCurve _radiusCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        private Coroutine _pulseRoutine;

        private void OnEnable()
        {
            ResolveLight();

            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
            }

            _pulseRoutine = StartCoroutine(Pulse());
        }

        private void OnDisable()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }

            ApplyLight(0f, 0f);
        }

        private void OnValidate()
        {
            _peakIntensity = Mathf.Max(0f, _peakIntensity);
            _peakOuterRadius = Mathf.Max(0f, _peakOuterRadius);
            _peakInnerRadius = Mathf.Max(0f, Mathf.Min(_peakInnerRadius, _peakOuterRadius));
            _fadeDuration = Mathf.Max(0.01f, _fadeDuration);
            ResolveLight();
        }

        private IEnumerator Pulse()
        {
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                float t = elapsed / _fadeDuration;
                float intensity = _peakIntensity * Evaluate(_intensityCurve, t);
                float radius = _peakOuterRadius * Evaluate(_radiusCurve, t);

                ApplyLight(intensity, radius);

                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyLight(0f, 0f);
            _pulseRoutine = null;
        }

        private void ApplyLight(float intensity, float outerRadius)
        {
            if (_light == null)
            {
                return;
            }

            _light.intensity = Mathf.Max(0f, intensity);
            _light.pointLightOuterRadius = Mathf.Max(0f, outerRadius);
            _light.pointLightInnerRadius = Mathf.Min(_peakInnerRadius, _light.pointLightOuterRadius);
        }

        private void ResolveLight()
        {
            if (_light == null)
            {
                _light = GetComponent<Light2D>();
            }
        }

        private static float Evaluate(AnimationCurve curve, float t)
        {
            return curve == null ? 1f - t : Mathf.Max(0f, curve.Evaluate(t));
        }
    }
}
