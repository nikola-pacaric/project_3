using System.Collections;
using UnityEngine;
using Warblade.Data;
using Warblade.Entities;
using Warblade.Managers;

namespace Warblade.Systems
{
    /// <summary>
    /// Coordinates the between-sector warp presentation after enemy-set and boss milestones.
    /// </summary>
    public class SectorTransitionController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpaceBackgroundController _backgroundController;
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private PlayerShooting _playerShooting;
        [SerializeField] private ParticleSystem[] _thrusterParticles;

        [Header("Timing")]
        [SerializeField, Min(0.1f)] private float _durationSeconds = 2.4f;
        [SerializeField] private AnimationCurve _warpCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Range(0.05f, 0.45f)] private float _travelOutRatio = 0.3f;
        [SerializeField, Range(0.05f, 0.45f)] private float _travelBackRatio = 0.3f;

        [Header("Player Travel")]
        [SerializeField] private float _midpointY = -0.5f;

        [Header("Thruster Warp")]
        [SerializeField, Min(1f)] private float _thrusterEmissionMultiplier = 3f;
        [SerializeField, Min(1f)] private float _thrusterSimulationSpeedMultiplier = 2.4f;
        [SerializeField, Min(1f)] private float _thrusterStartSizeMultiplier = 1.8f;

        private ParticleSystem.EmissionModule[] _thrusterEmissionModules;
        private ParticleSystem.MainModule[] _thrusterMainModules;
        private ParticleSystem.MinMaxCurve[] _baseThrusterEmissionRates;
        private float[] _baseThrusterSimulationSpeeds;
        private ParticleSystem.MinMaxCurve[] _baseThrusterStartSizes;
        private Coroutine _transitionRoutine;

        public bool IsRunning => _transitionRoutine != null;

        private void OnValidate()
        {
            _durationSeconds = Mathf.Max(0.1f, _durationSeconds);
            _travelOutRatio = Mathf.Clamp(_travelOutRatio, 0.05f, 0.45f);
            _travelBackRatio = Mathf.Clamp(_travelBackRatio, 0.05f, 0.45f);
            _thrusterEmissionMultiplier = Mathf.Max(1f, _thrusterEmissionMultiplier);
            _thrusterSimulationSpeedMultiplier = Mathf.Max(1f, _thrusterSimulationSpeedMultiplier);
            _thrusterStartSizeMultiplier = Mathf.Max(1f, _thrusterStartSizeMultiplier);
        }

        /// <summary>
        /// Plays the sector travel sequence. Horizontal player movement remains active.
        /// </summary>
        public IEnumerator PlayTransitionRoutine()
        {
            if (_transitionRoutine != null)
            {
                StopCoroutine(_transitionRoutine);
                RestorePresentation();
            }

            _transitionRoutine = StartCoroutine(RunTransitionRoutine());
            yield return _transitionRoutine;
        }

        private IEnumerator RunTransitionRoutine()
        {
            CaptureThrusterDefaults();
            _playerShooting?.SetShootingEnabled(false);

            float warpAudioFadeOutDuration = _durationSeconds * _travelBackRatio;
            float warpAudioFadeOutDelay = Mathf.Max(0f, _durationSeconds - warpAudioFadeOutDuration);
            AudioManager.Instance?.PlayOneShotWithFadeOut(
                AudioCue.SectionWarp,
                warpAudioFadeOutDelay,
                warpAudioFadeOutDuration);

            float elapsed = 0f;
            float startY = _playerTransform == null ? 0f : _playerTransform.position.y;

            while (elapsed < _durationSeconds)
            {
                float normalizedTime = elapsed / _durationSeconds;
                float warpIntensity = EvaluateWarpIntensity(normalizedTime);
                ApplyPresentation(warpIntensity, startY);

                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyPresentation(0f, startY);
            RestorePresentation();
            _transitionRoutine = null;
        }

        private float EvaluateWarpIntensity(float normalizedTime)
        {
            float holdStart = _travelOutRatio;
            float holdEnd = 1f - _travelBackRatio;
            float triangularTime;

            if (normalizedTime < holdStart)
            {
                triangularTime = normalizedTime / Mathf.Max(0.01f, _travelOutRatio);
            }
            else if (normalizedTime > holdEnd)
            {
                triangularTime = (1f - normalizedTime) / Mathf.Max(0.01f, _travelBackRatio);
            }
            else
            {
                triangularTime = 1f;
            }

            return Mathf.Clamp01(_warpCurve == null
                ? triangularTime
                : _warpCurve.Evaluate(Mathf.Clamp01(triangularTime)));
        }

        private void ApplyPresentation(float warpIntensity, float startY)
        {
            _backgroundController?.SetWarpIntensity(warpIntensity);
            ApplyPlayerTravel(warpIntensity, startY);
            ApplyThrusterWarp(warpIntensity);
        }

        private void ApplyPlayerTravel(float warpIntensity, float startY)
        {
            if (_playerTransform == null)
            {
                return;
            }

            Vector3 position = _playerTransform.position;
            position.y = Mathf.Lerp(startY, _midpointY, EvaluateTravelProgress(warpIntensity));
            _playerTransform.position = position;
        }

        private float EvaluateTravelProgress(float warpIntensity)
        {
            return warpIntensity;
        }

        private void CaptureThrusterDefaults()
        {
            if (_thrusterParticles == null)
            {
                _thrusterParticles = new ParticleSystem[0];
            }

            int count = _thrusterParticles.Length;
            _thrusterEmissionModules = new ParticleSystem.EmissionModule[count];
            _thrusterMainModules = new ParticleSystem.MainModule[count];
            _baseThrusterEmissionRates = new ParticleSystem.MinMaxCurve[count];
            _baseThrusterSimulationSpeeds = new float[count];
            _baseThrusterStartSizes = new ParticleSystem.MinMaxCurve[count];

            for (int i = 0; i < count; i++)
            {
                ParticleSystem thruster = _thrusterParticles[i];
                if (thruster == null)
                {
                    continue;
                }

                ParticleSystem.EmissionModule emission = thruster.emission;
                ParticleSystem.MainModule main = thruster.main;

                _thrusterEmissionModules[i] = emission;
                _thrusterMainModules[i] = main;
                _baseThrusterEmissionRates[i] = emission.rateOverTime;
                _baseThrusterSimulationSpeeds[i] = main.simulationSpeed;
                _baseThrusterStartSizes[i] = main.startSize;
            }
        }

        private void ApplyThrusterWarp(float warpIntensity)
        {
            if (_thrusterParticles == null)
            {
                return;
            }

            for (int i = 0; i < _thrusterParticles.Length; i++)
            {
                ParticleSystem thruster = _thrusterParticles[i];
                if (thruster == null)
                {
                    continue;
                }

                ParticleSystem.EmissionModule emission = _thrusterEmissionModules[i];
                ParticleSystem.MainModule main = _thrusterMainModules[i];

                float emissionMultiplier = Mathf.Lerp(1f, _thrusterEmissionMultiplier, warpIntensity);
                float simulationSpeedMultiplier = Mathf.Lerp(1f, _thrusterSimulationSpeedMultiplier, warpIntensity);
                float startSizeMultiplier = Mathf.Lerp(1f, _thrusterStartSizeMultiplier, warpIntensity);

                emission.rateOverTime = ScaleCurve(_baseThrusterEmissionRates[i], emissionMultiplier);
                main.simulationSpeed = _baseThrusterSimulationSpeeds[i] * simulationSpeedMultiplier;
                main.startSize = ScaleCurve(_baseThrusterStartSizes[i], startSizeMultiplier);
            }
        }

        private void RestorePresentation()
        {
            _backgroundController?.SetWarpIntensity(0f);
            ApplyThrusterWarp(0f);
        }

        private static ParticleSystem.MinMaxCurve ScaleCurve(ParticleSystem.MinMaxCurve curve, float multiplier)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return new ParticleSystem.MinMaxCurve(curve.constant * multiplier);

                case ParticleSystemCurveMode.TwoConstants:
                    return new ParticleSystem.MinMaxCurve(curve.constantMin * multiplier, curve.constantMax * multiplier);

                case ParticleSystemCurveMode.Curve:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * multiplier, curve.curve);

                case ParticleSystemCurveMode.TwoCurves:
                    return new ParticleSystem.MinMaxCurve(curve.curveMultiplier * multiplier, curve.curveMin, curve.curveMax);

                default:
                    return curve;
            }
        }
    }
}
