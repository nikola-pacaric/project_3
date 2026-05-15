using UnityEngine;

namespace Warblade.Data
{
    public enum EnemyBehaviorMode
    {
        Formation = 0,
        KamikazeReturn = 1,
        BonusSnake = 2,
        Mother = 3
    }

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
        [SerializeField] private EnemyBehaviorMode _behaviorMode = EnemyBehaviorMode.Formation;
        [SerializeField] private bool _diesAtDiveBottom;
        [SerializeField] private bool _canFire = true;
        [SerializeField] private bool _limitConcurrentDivesAboveFinalThreshold;
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

        [Header("Drops")]
        [SerializeField] private DropTable _dropTable;

        public int MaxHealth => _maxHealth;
        public int ScoreValue => _scoreValue;
        public float EntrySpeed => _entrySpeed;
        public float DiveSpeed => _diveSpeed;
        public float DiveBottomY => _diveBottomY;
        public EnemyBehaviorMode BehaviorMode => _behaviorMode;
        public bool DiesAtDiveBottom => _diesAtDiveBottom;
        public bool CanFire => _canFire;
        public bool LimitConcurrentDivesAboveFinalThreshold => _limitConcurrentDivesAboveFinalThreshold;
        public bool CountsForPerfectClearBonus =>
            _behaviorMode == EnemyBehaviorMode.KamikazeReturn ||
            _behaviorMode == EnemyBehaviorMode.BonusSnake;
        public float PassThroughChance => _passThroughChance;
        public float RespawnTopY => _respawnTopY;
        public float DiveCooldownMin => _diveCooldownMin;
        public float DiveCooldownMax => _diveCooldownMax;
        public float FireCooldownMin => _fireCooldownMin;
        public float FireCooldownMax => _fireCooldownMax;
        public float LingerDurationMin => _lingerDurationMin;
        public float LingerDurationMax => _lingerDurationMax;
        public Color SpriteColor => _spriteColor;
        public DropTable DropTable => _dropTable;
    }
}
