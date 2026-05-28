using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warblade.Data;
using Warblade.Data.Events;

namespace Warblade.UI
{
    public class BossHud : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField] private BossDataEventChannel _bossSpawned;
        [SerializeField] private BossHealthEventChannel _bossHealthChanged;
        [SerializeField] private BossDataEventChannel _bossDefeated;

        [Header("View")]
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _slideRoot;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _healthBarFrame;
        [SerializeField] private Image _healthFill;

        [Header("Behavior")]
        [SerializeField] private bool _hideWhenInactive = true;
        [SerializeField, Min(0f)] private float _slideDuration = 0.35f;
        [SerializeField] private Vector2 _hiddenOffset = new Vector2(0f, 160f);

        private Sprite _defaultHealthBarFrameSprite;
        private Sprite _defaultHealthBarFillSprite;
        private Vector2 _shownAnchoredPosition;
        private Coroutine _slideRoutine;

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            if (_slideRoot == null)
            {
                _slideRoot = _root == null ? null : _root.GetComponent<RectTransform>();
            }

            if (_slideRoot != null)
            {
                _shownAnchoredPosition = _slideRoot.anchoredPosition;
                _slideRoot.anchoredPosition = GetHiddenAnchoredPosition();
            }

            if (_healthBarFrame != null)
            {
                _defaultHealthBarFrameSprite = _healthBarFrame.sprite;
            }

            if (_healthFill != null)
            {
                _defaultHealthBarFillSprite = _healthFill.sprite;
            }

            SetVisible(!_hideWhenInactive);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            if (_slideRoutine != null)
            {
                StopCoroutine(_slideRoutine);
                _slideRoutine = null;
            }

            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_bossSpawned != null) _bossSpawned.OnEventRaised += HandleBossSpawned;
            if (_bossHealthChanged != null) _bossHealthChanged.OnEventRaised += HandleBossHealthChanged;
            if (_bossDefeated != null) _bossDefeated.OnEventRaised += HandleBossDefeated;
        }

        private void Unsubscribe()
        {
            if (_bossSpawned != null) _bossSpawned.OnEventRaised -= HandleBossSpawned;
            if (_bossHealthChanged != null) _bossHealthChanged.OnEventRaised -= HandleBossHealthChanged;
            if (_bossDefeated != null) _bossDefeated.OnEventRaised -= HandleBossDefeated;
        }

        private void HandleBossSpawned(BossData bossData)
        {
            SetNameText(bossData);
            SetHealthBarSprites(bossData);
            ResetHealthFill();
            SetSlidePosition(GetHiddenAnchoredPosition());
            SetVisible(true);
            StartSlideIn();
        }

        private void HandleBossHealthChanged(int currentHealth, int maxHealth)
        {
            if (_healthFill != null)
            {
                _healthFill.fillAmount = maxHealth <= 0
                    ? 0f
                    : Mathf.Clamp01(currentHealth / (float)maxHealth);
            }
        }

        private void HandleBossDefeated(BossData bossData)
        {
            if (_slideRoutine != null)
            {
                StopCoroutine(_slideRoutine);
                _slideRoutine = null;
            }

            if (_healthFill != null)
            {
                _healthFill.fillAmount = 0f;
            }

            if (_hideWhenInactive)
            {
                SetVisible(false);
                SetSlidePosition(GetHiddenAnchoredPosition());
            }
        }

        private void SetVisible(bool isVisible)
        {
            if (_root != null && _root != gameObject)
            {
                _root.SetActive(isVisible);
            }
        }

        private void StartSlideIn()
        {
            if (_slideRoutine != null)
            {
                StopCoroutine(_slideRoutine);
            }

            if (_slideRoot == null || _slideDuration <= Mathf.Epsilon)
            {
                SetSlidePosition(_shownAnchoredPosition);
                return;
            }

            _slideRoutine = StartCoroutine(RunSlideIn());
        }

        private IEnumerator RunSlideIn()
        {
            Vector2 startPosition = _slideRoot.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < _slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _slideDuration);
                float easedT = Mathf.SmoothStep(0f, 1f, t);
                _slideRoot.anchoredPosition = Vector2.Lerp(startPosition, _shownAnchoredPosition, easedT);
                yield return null;
            }

            _slideRoot.anchoredPosition = _shownAnchoredPosition;
            _slideRoutine = null;
        }

        private Vector2 GetHiddenAnchoredPosition()
        {
            return _shownAnchoredPosition + _hiddenOffset;
        }

        private void SetSlidePosition(Vector2 anchoredPosition)
        {
            if (_slideRoot != null)
            {
                _slideRoot.anchoredPosition = anchoredPosition;
            }
        }

        private void SetNameText(BossData bossData)
        {
            if (_nameText != null)
            {
                _nameText.text = bossData != null ? bossData.DisplayName : string.Empty;
            }
        }

        private void SetHealthBarSprites(BossData bossData)
        {
            if (_healthBarFrame != null)
            {
                Sprite frameSprite = bossData != null && bossData.HealthBarFrameSprite != null
                    ? bossData.HealthBarFrameSprite
                    : _defaultHealthBarFrameSprite;

                _healthBarFrame.sprite = frameSprite;
            }

            if (_healthFill != null)
            {
                Sprite fillSprite = bossData != null && bossData.HealthBarFillSprite != null
                    ? bossData.HealthBarFillSprite
                    : _defaultHealthBarFillSprite;

                _healthFill.sprite = fillSprite;
            }
        }

        private void ResetHealthFill()
        {
            if (_healthFill != null)
            {
                _healthFill.fillAmount = 1f;
            }
        }
    }
}
