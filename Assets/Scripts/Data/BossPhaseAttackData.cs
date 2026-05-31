using System;
using UnityEngine;

namespace Warblade.Data
{
    [Serializable]
    public class BossPhaseAttackData
    {
        [SerializeField] private BossAttackPatternData _pattern;
        [SerializeField] private GameObject _bulletPrefab;
        [Tooltip("0 means this attack is picked alone. Positive matching IDs make attacks fire together as one random option.")]
        [SerializeField, Min(0)] private int _simultaneousGroupId;

        public BossAttackPatternData Pattern => _pattern;
        public GameObject BulletPrefab => _bulletPrefab;
        public int SimultaneousGroupId => _simultaneousGroupId;

        internal bool HasPattern => _pattern != null;

        public BossPhaseAttackData()
        {
        }

        internal BossPhaseAttackData(BossAttackPatternData pattern)
        {
            _pattern = pattern;
        }
    }
}
