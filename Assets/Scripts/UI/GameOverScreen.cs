using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Warblade.Data;
using Warblade.Managers;

namespace Warblade.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private GameObject _defaultSelected;
        [SerializeField] private TMP_Text _finalScoreText;
        [SerializeField] private string _finalScoreFormat = "Final Score: {0}";

        private void Awake()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        public void Show()
        {
            RefreshFinalScore();

            if (_panel != null)
            {
                _panel.SetActive(true);
                UiSelectionHelper.ApplyPanelNavigation(_panel);
            }

            UiSelectionHelper.ClearSelectionStack();
            UiSelectionHelper.SelectNextFrame(this, _defaultSelected);
            Time.timeScale = 0f;
        }

        public void RestartRun()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            UiSelectionHelper.ClearSelectionStack();
            Hide();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartRun();
                return;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ReturnToMainMenu()
        {
            AudioManager.Instance?.PlayOneShot(AudioCue.UiButton);
            UiSelectionHelper.ClearSelectionStack();
            Hide();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartToMainMenu();
                return;
            }

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void RefreshFinalScore()
        {
            if (_finalScoreText == null)
            {
                return;
            }

            int score = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
            _finalScoreText.text = string.Format(_finalScoreFormat, score);
        }
    }
}
