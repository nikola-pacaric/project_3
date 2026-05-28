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
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _healthBarFrame;
        [SerializeField] private Image _healthFill;

        [Header("Behavior")]
        [SerializeField] private bool _hideWhenInactive = true;

        private Sprite _defaultHealthBarFrameSprite;
        private Sprite _defaultHealthBarFillSprite;

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
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
            SetVisible(true);
            SetNameText(bossData);
            SetHealthBarSprites(bossData);
            ResetHealthFill();
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
            if (_healthFill != null)
            {
                _healthFill.fillAmount = 0f;
            }

            if (_hideWhenInactive)
            {
                SetVisible(false);
            }
        }

        private void SetVisible(bool isVisible)
        {
            if (_root != null && _root != gameObject)
            {
                _root.SetActive(isVisible);
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
