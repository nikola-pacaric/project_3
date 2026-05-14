using System.Collections.Generic;
using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Boss Data", fileName = "BossData")]
    public class BossData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _displayName = "Boss";
        [SerializeField, Min(1)] private int _maxHealth = 250;
        [SerializeField, Min(0)] private int _scoreValue = 5000;

        [Header("Entry")]
        [SerializeField] private Vector2 _entryStartPosition = new Vector2(0f, 7f);
        [SerializeField] private Vector2 _entryTargetPosition = new Vector2(0f, 2.75f);
        [SerializeField, Min(0f)] private float _entrySpeed = 3f;

        [Header("Combat")]
        [SerializeField, Min(0)] private int _contactDamage = 1;
        [SerializeField] private List<BossPhaseData> _phases = new List<BossPhaseData>();

        [Header("Rewards")]
        [SerializeField] private DropTable _rewardDropTable;

        public string DisplayName => _displayName;
        public int MaxHealth => _maxHealth;
        public int ScoreValue => _scoreValue;
        public Vector2 EntryStartPosition => _entryStartPosition;
        public Vector2 EntryTargetPosition => _entryTargetPosition;
        public float EntrySpeed => _entrySpeed;
        public int ContactDamage => _contactDamage;
        public IReadOnlyList<BossPhaseData> Phases => _phases;
        public DropTable RewardDropTable => _rewardDropTable;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_displayName))
            {
                _displayName = name;
            }

            _maxHealth = Mathf.Max(1, _maxHealth);
            _scoreValue = Mathf.Max(0, _scoreValue);
            _entrySpeed = Mathf.Max(0f, _entrySpeed);
            _contactDamage = Mathf.Max(0, _contactDamage);

            if (_phases == null)
            {
                return;
            }

            for (int i = 0; i < _phases.Count; i++)
            {
                _phases[i]?.OnValidate();
            }
        }
    }
}
