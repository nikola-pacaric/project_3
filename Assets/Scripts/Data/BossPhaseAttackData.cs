using System;
using UnityEngine;

namespace Warblade.Data
{
    [Serializable]
    public class BossPhaseAttackData
    {
        [SerializeField] private BossAttackPatternData _pattern;
        [SerializeField] private GameObject _bulletPrefab;

        public BossAttackPatternData Pattern => _pattern;
        public GameObject BulletPrefab => _bulletPrefab;

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
