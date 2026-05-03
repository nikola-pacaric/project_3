using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [Header("Core Stats")]
        [Min(1f)] [SerializeField] private int _maxHealth = 1;
        [SerializeField] private int _scoreValue = 100;
        [SerializeField] private float _entrySpeed = 3f;
        [SerializeField] private float _diveSpeed = 6f;
        [SerializeField] private float _diveBottomY = -6f;

        [Header("Behavior")]
        [SerializeField] private bool _sitsInFormation = true;
        [SerializeField] private bool _diesAtDiveBottom;
        [SerializeField] private bool _canFire = true;
        [SerializeField, Range(0f, 1f)] private float _passThroughChance = 0.5f;
        [SerializeField] private float _respawnTopY = 6f;

        [Header("Cooldowns")]
        [SerializeField] private float _diveCooldownMin = 2f;
        [SerializeField] private float _diveCooldownMax = 5f;
        [SerializeField] private float _fireCooldownMin = 1.5f;
        [SerializeField] private float _fireCooldownMax = 4f;
        [SerializeField] private float _lingerDurationMin = 0.5f;
        [SerializeField] private float _lingerDurationMax = 1.5f;

        [Header("Visual")]
        [SerializeField] private Color _spriteColor = Color.white;

        public int MaxHealth => _maxHealth;
        public int ScoreValue => _scoreValue;
        public float EntrySpeed => _entrySpeed;
        public float DiveSpeed => _diveSpeed;
        public float DiveBottomY => _diveBottomY;
        public bool SitsInFormation => _sitsInFormation;
        public bool DiesAtDiveBottom => _diesAtDiveBottom;
        public bool CanFire => _canFire;
        public float PassThroughChance => _passThroughChance;
        public float RespawnTopY => _respawnTopY;
        public float DiveCooldownMin => _diveCooldownMin;
        public float DiveCooldownMax => _diveCooldownMax;
        public float FireCooldownMin => _fireCooldownMin;
        public float FireCooldownMax => _fireCooldownMax;
        public float LingerDurationMin => _lingerDurationMin;
        public float LingerDurationMax => _lingerDurationMax;
        public Color SpriteColor => _spriteColor;
    }
}
