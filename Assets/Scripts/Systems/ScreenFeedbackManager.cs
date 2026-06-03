using System;
using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using Warblade.Data;
using Warblade.Data.Events;

namespace Warblade.Systems
{
    public class ScreenFeedbackManager : MonoBehaviour
    {
        [Serializable]
        private class ScreenFeedbackCue
        {
            [SerializeField] private Color _flashColor = Color.white;
            [SerializeField, Range(0f, 1f)] private float _peakAlpha = 0.28f;
            [SerializeField, Min(0f)] private float _fadeInDuration = 0.03f;
            [SerializeField, Min(0f)] private float _holdDuration = 0.04f;
            [SerializeField, Min(0f)] private float _fadeOutDuration = 0.18f;
            [SerializeField] private bool _sendCameraImpulse = true;
            [SerializeField, Min(0f)] private float _impulseForce = 0.7f;

            public Color FlashColor => _flashColor;
            public float PeakAlpha => _peakAlpha;
            public float FadeInDuration => _fadeInDuration;
            public float HoldDuration => _holdDuration;
            public float FadeOutDuration => _fadeOutDuration;
            public bool SendCameraImpulse => _sendCameraImpulse;
            public float ImpulseForce => _impulseForce;

            public ScreenFeedbackCue()
            {
            }

            public ScreenFeedbackCue(
                Color flashColor,
                float peakAlpha,
                float fadeInDuration,
                float holdDuration,
                float fadeOutDuration,
                bool sendCameraImpulse,
                float impulseForce)
            {
                _flashColor = flashColor;
                _peakAlpha = peakAlpha;
                _fadeInDuration = fadeInDuration;
                _holdDuration = holdDuration;
                _fadeOutDuration = fadeOutDuration;
                _sendCameraImpulse = sendCameraImpulse;
                _impulseForce = impulseForce;
            }

            public void Validate()
            {
                _peakAlpha = Mathf.Clamp01(_peakAlpha);
                _fadeInDuration = Mathf.Max(0f, _fadeInDuration);
                _holdDuration = Mathf.Max(0f, _holdDuration);
                _fadeOutDuration = Mathf.Max(0f, _fadeOutDuration);
                _impulseForce = Mathf.Max(0f, _impulseForce);
            }
        }

        [Header("Events")]
        [SerializeField] private VoidEventChannel _playerDeathFeedbackRequested;
        [SerializeField] private VoidEventChannel _motherDeathFeedbackRequested;
        [SerializeField] private BossDataEventChannel _bossDefeated;

        [Header("Flash Overlay")]
        [SerializeField] private Graphic _flashGraphic;
        [SerializeField] private CanvasGroup _flashCanvasGroup;

        [Header("Camera Impulse")]
        [SerializeField] private CinemachineImpulseSource _impulseSource;

        [Header("Cue Tuning")]
        [SerializeField] private ScreenFeedbackCue _playerDeath =
            new ScreenFeedbackCue(new Color(1f, 0.18f, 0.08f, 1f), 0.26f, 0.025f, 0.035f, 0.16f, true, 0.65f);
        [SerializeField] private ScreenFeedbackCue _motherDeath =
            new ScreenFeedbackCue(new Color(0.48f, 1f, 0.12f, 1f), 0.22f, 0.025f, 0.035f, 0.18f, true, 0.55f);
        [SerializeField] private ScreenFeedbackCue _bossDeath =
            new ScreenFeedbackCue(new Color(1f, 0.92f, 0.74f, 1f), 0.34f, 0.035f, 0.06f, 0.24f, true, 0.9f);

        private Coroutine _flashRoutine;

        private void Awake()
        {
            ResolveReferences();
            SetFlashAlpha(0f);
        }

        private void OnEnable()
        {
            if (_playerDeathFeedbackRequested != null)
            {
                _playerDeathFeedbackRequested.OnEventRaised += PlayPlayerDeathFeedback;
            }

            if (_motherDeathFeedbackRequested != null)
            {
                _motherDeathFeedbackRequested.OnEventRaised += PlayMotherDeathFeedback;
            }

            if (_bossDefeated != null)
            {
                _bossDefeated.OnEventRaised += PlayBossDeathFeedback;
            }
        }

        private void OnDisable()
        {
            if (_playerDeathFeedbackRequested != null)
            {
                _playerDeathFeedbackRequested.OnEventRaised -= PlayPlayerDeathFeedback;
            }

            if (_motherDeathFeedbackRequested != null)
            {
                _motherDeathFeedbackRequested.OnEventRaised -= PlayMotherDeathFeedback;
            }

            if (_bossDefeated != null)
            {
                _bossDefeated.OnEventRaised -= PlayBossDeathFeedback;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            SetFlashAlpha(0f);
        }

        private void OnValidate()
        {
            _playerDeath?.Validate();
            _motherDeath?.Validate();
            _bossDeath?.Validate();
        }

        /// <summary>
        /// Plays the tuned screen feedback for a player ship death.
        /// </summary>
        public void PlayPlayerDeathFeedback()
        {
            PlayFeedback(_playerDeath);
        }

        /// <summary>
        /// Plays the tuned screen feedback for killing a Mother enemy.
        /// </summary>
        public void PlayMotherDeathFeedback()
        {
            PlayFeedback(_motherDeath);
        }

        /// <summary>
        /// Plays the tuned screen feedback for a completed boss defeat.
        /// </summary>
        public void PlayBossDeathFeedback(BossData bossData)
        {
            PlayFeedback(_bossDeath);
        }

        private void PlayFeedback(ScreenFeedbackCue cue)
        {
            if (cue == null)
            {
                return;
            }

            if (cue.SendCameraImpulse && _impulseSource != null && cue.ImpulseForce > 0f)
            {
                _impulseSource.GenerateImpulseWithForce(cue.ImpulseForce);
            }

            if (_flashGraphic == null || _flashCanvasGroup == null || cue.PeakAlpha <= 0f)
            {
                return;
            }

            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
            }

            _flashRoutine = StartCoroutine(RunFlash(cue));
        }

        private IEnumerator RunFlash(ScreenFeedbackCue cue)
        {
            _flashGraphic.color = Color.white;

            if (cue.FadeInDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < cue.FadeInDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    SetFlashAlpha(Mathf.Lerp(0f, cue.PeakAlpha, Mathf.Clamp01(elapsed / cue.FadeInDuration)));
                    yield return null;
                }
            }

            SetFlashAlpha(cue.PeakAlpha);

            if (cue.HoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(cue.HoldDuration);
            }

            if (cue.FadeOutDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < cue.FadeOutDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    SetFlashAlpha(Mathf.Lerp(cue.PeakAlpha, 0f, Mathf.Clamp01(elapsed / cue.FadeOutDuration)));
                    yield return null;
                }
            }

            SetFlashAlpha(0f);
            _flashRoutine = null;
        }

        private void SetFlashAlpha(float alpha)
        {
            if (_flashCanvasGroup != null)
            {
                _flashCanvasGroup.alpha = Mathf.Clamp01(alpha);
            }
        }

        private void ResolveReferences()
        {
            if (_flashGraphic == null)
            {
                _flashGraphic = GetComponentInChildren<Graphic>(true);
            }

            if (_flashCanvasGroup == null && _flashGraphic != null)
            {
                _flashCanvasGroup = _flashGraphic.GetComponent<CanvasGroup>();
                if (_flashCanvasGroup == null)
                {
                    _flashCanvasGroup = _flashGraphic.GetComponentInParent<CanvasGroup>(true);
                }
            }

            if (_impulseSource == null)
            {
                _impulseSource = GetComponent<CinemachineImpulseSource>();
            }
        }
    }
}
