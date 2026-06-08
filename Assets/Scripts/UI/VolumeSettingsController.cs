using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warblade.Managers;

namespace Warblade.UI
{
    [DisallowMultipleComponent]
    public class VolumeSettingsController : MonoBehaviour
    {
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _uiSlider;
        [SerializeField] private TMP_Text _masterValueText;
        [SerializeField] private TMP_Text _musicValueText;
        [SerializeField] private TMP_Text _sfxValueText;
        [SerializeField] private TMP_Text _uiValueText;
        [SerializeField] private string _valueFormat = "{0:0%}";

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        public void SetMasterVolume(float volume)
        {
            AudioManager.Instance?.SetMasterVolume(volume);
            SetValueText(_masterValueText, volume);
        }

        public void SetMusicVolume(float volume)
        {
            AudioManager.Instance?.SetMusicVolume(volume);
            SetValueText(_musicValueText, volume);
        }

        public void SetSfxVolume(float volume)
        {
            AudioManager.Instance?.SetSfxVolume(volume);
            SetValueText(_sfxValueText, volume);
        }

        public void SetUiVolume(float volume)
        {
            AudioManager.Instance?.SetUiVolume(volume);
            SetValueText(_uiValueText, volume);
        }

        public void Refresh()
        {
            AudioManager audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            SetSliderValue(_masterSlider, audioManager.MasterVolume);
            SetSliderValue(_musicSlider, audioManager.MusicVolume);
            SetSliderValue(_sfxSlider, audioManager.SfxVolume);
            SetSliderValue(_uiSlider, audioManager.UiVolume);

            SetValueText(_masterValueText, audioManager.MasterVolume);
            SetValueText(_musicValueText, audioManager.MusicVolume);
            SetValueText(_sfxValueText, audioManager.SfxVolume);
            SetValueText(_uiValueText, audioManager.UiVolume);
        }

        private static void SetSliderValue(Slider slider, float value)
        {
            if (slider != null)
            {
                slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            }
        }

        private void SetValueText(TMP_Text valueText, float value)
        {
            if (valueText != null)
            {
                valueText.text = string.Format(_valueFormat, Mathf.Clamp01(value));
            }
        }
    }
}
