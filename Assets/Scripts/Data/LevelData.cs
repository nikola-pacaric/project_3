using System.Collections.Generic;
using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Level Data", fileName = "LevelData")]
    public class LevelData : ScriptableObject
    {
        [SerializeField, Min(1)] private int _levelNumber = 1;
        [SerializeField] private List<WaveData> _waves = new List<WaveData>();

        public int LevelNumber => _levelNumber;
        public IReadOnlyList<WaveData> Waves => _waves;
    }
}
