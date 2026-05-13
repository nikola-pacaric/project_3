using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Pickup Data", fileName = "PickupData")]
    public class PickupData : ScriptableObject
    {
        [SerializeField] private PickupEffectType _effectType;
        [SerializeField] private string _displayName = "Pickup";
        [SerializeField, Min(1)] private int _amount = 1;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Color _spriteColor = Color.white;

        public PickupEffectType EffectType => _effectType;
        public string DisplayName => _displayName;
        public int Amount => _amount;
        public Sprite Sprite => _sprite;
        public Color SpriteColor => _spriteColor;

        private void OnValidate()
        {
            _amount = Mathf.Max(1, _amount);

            if (string.IsNullOrWhiteSpace(_displayName))
            {
                _displayName = _effectType.ToString();
            }
        }
    }
}
