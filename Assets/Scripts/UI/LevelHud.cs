using TMPro;
using UnityEngine;
using Warblade.Data.Events;
using Warblade.Managers;

namespace Warblade.UI
{
    public class LevelHud : MonoBehaviour
    {
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private IntEventChannel _levelStarted;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private string _format = "Level {0}";

        private void Awake()
        {
            if (_levelManager == null)
            {
                _levelManager = LevelManager.Instance;
            }

            Refresh();
        }

        private void OnEnable()
        {
            if (_levelManager == null)
            {
                _levelManager = LevelManager.Instance;
            }

            if (_levelStarted != null)
            {
                _levelStarted.OnEventRaised += HandleLevelChanged;
            }
            else if (_levelManager != null)
            {
                _levelManager.OnLevelStarted.AddListener(HandleLevelChanged);
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_levelStarted != null)
            {
                _levelStarted.OnEventRaised -= HandleLevelChanged;
            }
            else if (_levelManager != null)
            {
                _levelManager.OnLevelStarted.RemoveListener(HandleLevelChanged);
            }
        }

        private void HandleLevelChanged(int levelNumber)
        {
            SetLevelText(levelNumber);
        }

        private void Refresh()
        {
            if (_levelManager != null)
            {
                SetLevelText(_levelManager.CurrentLevel);
            }
        }

        private void SetLevelText(int levelNumber)
        {
            if (_levelText != null)
            {
                _levelText.text = string.Format(_format, levelNumber);
            }
        }
    }
}
