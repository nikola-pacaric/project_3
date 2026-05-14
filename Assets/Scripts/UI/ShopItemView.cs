using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warblade.Data;

namespace Warblade.UI
{
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private string _priceFormat = "${0}";
        [SerializeField] private Color _normalColor = new Color(0.12f, 0.12f, 0.16f, 0.85f);
        [SerializeField] private Color _selectedColor = new Color(0.95f, 0.78f, 0.22f, 0.95f);
        [SerializeField] private Color _unavailableColor = new Color(0.18f, 0.18f, 0.18f, 0.65f);

        private ShopController _controller;
        private ShopItem _item;
        private bool _isSelected;

        public void Bind(ShopController controller, ShopItem item)
        {
            _controller = controller;
            _item = item;
            Refresh();
        }

        public void SetSelected(bool isSelected)
        {
            _isSelected = isSelected;
            Refresh();
        }

        public void Refresh()
        {
            if (_item == null)
            {
                SetText(_nameText, "");
                SetText(_priceText, "");
                RefreshBackground(false);
                return;
            }

            SetText(_nameText, _item.DisplayName);
            SetText(_priceText, string.Format(_priceFormat, _item.Price));

            string unavailableReason = _controller != null
                ? _controller.GetUnavailableReason(_item)
                : "Unavailable";

            bool canPurchase = string.IsNullOrEmpty(unavailableReason);
            RefreshBackground(canPurchase);
        }

        private void RefreshBackground(bool canPurchase)
        {
            if (_backgroundImage == null) return;

            _backgroundImage.color = !canPurchase
                ? _unavailableColor
                : _isSelected ? _selectedColor : _normalColor;
        }

        private void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
