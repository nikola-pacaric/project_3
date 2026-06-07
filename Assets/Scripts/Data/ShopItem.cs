using UnityEngine;

namespace Warblade.Data
{
    [CreateAssetMenu(menuName = "Warblade/Data/Shop Item", fileName = "ShopItem")]
    public class ShopItem : ScriptableObject
    {
        [SerializeField] private ShopItemType _itemType;
        [SerializeField] private string _displayName = "Shop Item";
        [SerializeField, TextArea] private string _description;
        [SerializeField, Min(0)] private int _price = 100;
        [SerializeField, Min(1)] private int _amount = 1;
        [SerializeField] private WeaponTier _weaponTier = WeaponTier.Double;
        [SerializeField] private BuffType _buffType = BuffType.Autofire;
        [SerializeField] private Sprite _icon;

        [Header("Preview")]
        [SerializeField] private Sprite _previewSprite;
        [SerializeField] private Color _previewTint = Color.white;

        public ShopItemType ItemType => _itemType;
        public string DisplayName => _displayName;
        public string Description => _description;
        public int Price => _price;
        public int Amount => _amount;
        public WeaponTier WeaponTier => _weaponTier;
        public BuffType BuffType => _buffType;
        public Sprite Icon => _icon;
        public Sprite PreviewSprite => _previewSprite != null ? _previewSprite : _icon;
        public Color PreviewTint => _previewTint;

        private void OnValidate()
        {
            _price = Mathf.Max(0, _price);
            _amount = Mathf.Max(1, _amount);

            if (string.IsNullOrWhiteSpace(_displayName))
            {
                _displayName = _itemType.ToString();
            }
        }
    }
}
