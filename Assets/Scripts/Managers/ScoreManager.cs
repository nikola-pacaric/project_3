using TMPro;
using UnityEngine;

namespace Warblade.Managers
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private string _format = "Score: {0}";

        private int _score;

        public int Score => _score;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            UpdateText();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void AddScore(int amount)
        {
            _score += amount;
            UpdateText();
        }

        public void ResetScore()
        {
            _score = 0;
            UpdateText();
        }

        private void UpdateText()
        {
            if (_scoreText != null)
            {
                _scoreText.text = string.Format(_format, _score);
            }
        }
    }
}
