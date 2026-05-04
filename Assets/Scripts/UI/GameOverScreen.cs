using UnityEngine;
using UnityEngine.SceneManagement;
using Warblade.Managers;

namespace Warblade.UI
{
    public class GameOverScreen : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;

        private void Awake()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        public void Show()
        {
            if (_panel != null)
            {
                _panel.SetActive(true);
            }
            Time.timeScale = 0f;
        }

        public void Restart()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
