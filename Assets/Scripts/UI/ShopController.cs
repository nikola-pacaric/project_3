using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.UI
{
    public class ShopController : MonoBehaviour
    {
        private enum ShopItemSortMode
        {
            Manual,
            PriceAscending,
            PriceDescending
        }

        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private ScrollRect _itemScrollRect;
        [SerializeField] private ShopItemView _itemViewPrefab;
        [SerializeField] private ShopItem[] _items;
        [SerializeField] private ShopItemSortMode _sortMode = ShopItemSortMode.PriceAscending;
        [SerializeField] private TMP_Text _cashText;
        [SerializeField] private InputReader _input;
        [SerializeField] private UiPanelTransition _transition;
        [SerializeField] private string _cashFormat = "Cash: ${0}";
        [SerializeField] private bool _buildItemViewsOnAwake = true;
        [SerializeField, Range(0.1f, 1f)] private float _navigationThreshold = 0.5f;
        [SerializeField] private bool _wrapSelection = true;

        [Header("Preview")]
        [SerializeField] private Image _previewImage;
        [SerializeField] private TMP_Text _descriptionText;

        [Header("Event Channels")]
        [SerializeField] private GameStateEventChannel _gameStateChanged;
        [SerializeField] private IntEventChannel _cashChanged;

        private readonly List<ShopItemView> _itemViews = new List<ShopItemView>();
        private bool _isSubscribedToGameManager;
        private int _selectedIndex;
        private int _previousNavigationDirection;

        private void Awake()
        {
            if (_root == null)
            {
                _root = gameObject;
            }

            ResolveCanvasGroup();
            ResolveTransition();
            ResolveItemScrollRect();

            if (_buildItemViewsOnAwake)
            {
                SortItems();
                BuildItemViews();
            }

            SetVisible(false, true);
        }

        private void Start()
        {
            TrySubscribeToGameManager();
            RefreshVisibilityFromGameState();
        }

        private void OnEnable()
        {
            if (_gameStateChanged != null)
            {
                _gameStateChanged.OnEventRaised += HandleGameStateChanged;
            }

            if (_cashChanged != null)
            {
                _cashChanged.OnEventRaised += HandleCashChanged;
            }

            TrySubscribeToGameManager();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (_gameStateChanged != null)
            {
                _gameStateChanged.OnEventRaised -= HandleGameStateChanged;
            }

            if (_cashChanged != null)
            {
                _cashChanged.OnEventRaised -= HandleCashChanged;
            }

            UnsubscribeFromGameManager();
        }

        private void Update()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsShopOpen) return;

            HandleNavigationInput();

            if (_input != null && _input.FirePressedThisFrame)
            {
                TryBuySelectedItem();
            }
        }

        /// <summary>
        /// Attempts to buy a shop item using the current run cash.
        /// </summary>
        public bool TryBuy(ShopItem item)
        {
            string unavailableReason = GetUnavailableReason(item);
            if (!string.IsNullOrEmpty(unavailableReason))
            {
                return false;
            }

            RunStatsManager runStats = RunStatsManager.Instance;
            if (runStats == null || !runStats.TrySpendCash(item.Price))
            {
                RefreshAll();
                return false;
            }

            bool applied = ApplyItem(item, runStats);
            if (!applied)
            {
                runStats.AddCash(item.Price);
            }

            if (applied)
            {
                AudioManager.Instance?.PlayOneShot(AudioCue.ShopBuySuccess);
            }

            RefreshAll();
            CloseShopIfNoPurchasableItems();
            return applied;
        }

        /// <summary>
        /// Returns an empty string when the item can be bought, otherwise a short disabled reason.
        /// </summary>
        public string GetUnavailableReason(ShopItem item)
        {
            if (item == null)
            {
                return "Unavailable";
            }

            RunStatsManager runStats = RunStatsManager.Instance;
            if (runStats == null)
            {
                return "Needs RunStatsManager";
            }

            string itemStateReason = GetItemStateReason(item, runStats);
            if (!string.IsNullOrEmpty(itemStateReason))
            {
                return itemStateReason;
            }

            if (runStats.Cash < item.Price)
            {
                return "Not enough cash";
            }

            return "";
        }

        public void LeaveShop()
        {
            GameManager.Instance?.LeaveShop();
        }

        public bool TryBuySelectedItem()
        {
            if (_items == null || _items.Length == 0) return false;

            int clampedIndex = Mathf.Clamp(_selectedIndex, 0, _items.Length - 1);
            if (!CanSelectItem(clampedIndex))
            {
                SelectFirstAvailableItem();
                return false;
            }

            return TryBuy(_items[clampedIndex]);
        }

        [ContextMenu("Open Shop")]
        private void OpenShopFromContextMenu()
        {
            GameManager.Instance?.EnterShop();
        }

        [ContextMenu("Refresh Shop")]
        private void RefreshAll()
        {
            RefreshCash();
            RefreshItemViews();
            RefreshSelection();
        }

        private void BuildItemViews()
        {
            if (_itemViewPrefab == null || _itemContainer == null || _items == null)
            {
                return;
            }

            _itemViews.Clear();
            for (int i = 0; i < _items.Length; i++)
            {
                ShopItem item = _items[i];
                if (item == null) continue;

                ShopItemView view = Instantiate(_itemViewPrefab, _itemContainer);
                view.Bind(this, item);
                _itemViews.Add(view);
            }

            SelectFirstAvailableItem();
            RefreshSelection();
        }

        private bool ApplyItem(ShopItem item, RunStatsManager runStats)
        {
            switch (item.ItemType)
            {
                case ShopItemType.SpeedUpgrade:
                    runStats.IncreaseSpeed(item.Amount);
                    return true;
                case ShopItemType.BulletsUpgrade:
                    runStats.IncreaseBullets(item.Amount);
                    return true;
                case ShopItemType.TimeUpgrade:
                    runStats.IncreaseTime(item.Amount);
                    return true;
                case ShopItemType.Armour:
                    runStats.AddArmour(item.Amount);
                    return true;
                case ShopItemType.ExtraLife:
                    runStats.AddLife(item.Amount);
                    return true;
                case ShopItemType.WeaponTier:
                    runStats.SetWeaponTier(item.WeaponTier);
                    return true;
                case ShopItemType.TimedBuff:
                    BuffManager.Instance?.ActivateBuff(item.BuffType);
                    return BuffManager.Instance != null;
                default:
                    Debug.LogWarning($"[{nameof(ShopController)}] Unsupported shop item type: {item.ItemType}.");
                    return false;
            }
        }

        private string GetItemStateReason(ShopItem item, RunStatsManager runStats)
        {
            switch (item.ItemType)
            {
                case ShopItemType.SpeedUpgrade:
                    return runStats.SpeedLevel >= runStats.MaxSpeedLevel ? "Max Speed" : "";
                case ShopItemType.BulletsUpgrade:
                    return runStats.BulletsLevel >= runStats.MaxBulletsLevel ? "Max Bullets" : "";
                case ShopItemType.TimeUpgrade:
                    return runStats.TimeLevel >= runStats.MaxTimeLevel ? "Max Time" : "";
                case ShopItemType.Armour:
                    return runStats.Armour >= runStats.MaxArmour ? "Max Armour" : "";
                case ShopItemType.ExtraLife:
                    return runStats.Lives >= runStats.MaxLives ? "Max Lives" : "";
                case ShopItemType.WeaponTier:
                    return runStats.WeaponTier >= item.WeaponTier ? "Owned" : "";
                case ShopItemType.TimedBuff:
                    return BuffManager.Instance == null ? "Needs BuffManager" : "";
                default:
                    return "Unavailable";
            }
        }

        private void HandleNavigationInput()
        {
            if (_input == null || _items == null || _items.Length == 0) return;

            float axis = _input.ShopNavigateAxis;
            int direction = 0;
            if (axis >= _navigationThreshold)
            {
                direction = -1;
            }
            else if (axis <= -_navigationThreshold)
            {
                direction = 1;
            }

            if (direction != 0 && _previousNavigationDirection == 0)
            {
                MoveSelection(direction);
            }

            _previousNavigationDirection = direction;
        }

        private void MoveSelection(int direction)
        {
            if (_items == null || _items.Length == 0) return;

            int nextIndex = FindNextAvailableIndex(_selectedIndex, direction);
            if (nextIndex < 0) return;

            _selectedIndex = nextIndex;
            RefreshSelection();
        }

        private void SelectFirstAvailableItem()
        {
            int purchasableIndex = FindFirstPurchasableIndex();
            _selectedIndex = purchasableIndex >= 0
                ? purchasableIndex
                : FindFirstBrowseableIndex();

            RefreshSelection();
        }

        private void SortItems()
        {
            if (_items == null || _items.Length <= 1 || _sortMode == ShopItemSortMode.Manual)
            {
                return;
            }

            System.Array.Sort(_items, CompareItemsByPrice);
        }

        private int CompareItemsByPrice(ShopItem left, ShopItem right)
        {
            if (left == right) return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            int priceComparison = left.Price.CompareTo(right.Price);
            if (_sortMode == ShopItemSortMode.PriceDescending)
            {
                priceComparison *= -1;
            }

            if (priceComparison != 0)
            {
                return priceComparison;
            }

            return string.Compare(left.DisplayName, right.DisplayName, System.StringComparison.Ordinal);
        }

        private int FindFirstPurchasableIndex()
        {
            if (_items == null) return 0;

            for (int i = 0; i < _items.Length; i++)
            {
                if (CanSelectItem(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindFirstBrowseableIndex()
        {
            if (_items == null) return 0;

            for (int i = 0; i < _items.Length; i++)
            {
                if (CanBrowseItem(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindNextAvailableIndex(int startIndex, int direction)
        {
            if (_items == null || _items.Length == 0 || direction == 0) return -1;

            int clampedStart = Mathf.Clamp(startIndex, 0, _items.Length - 1);
            for (int step = 1; step <= _items.Length; step++)
            {
                int candidate = clampedStart + direction * step;
                if (_wrapSelection)
                {
                    candidate = (candidate + _items.Length) % _items.Length;
                }
                else if (candidate < 0 || candidate >= _items.Length)
                {
                    return -1;
                }

                if (CanSelectItem(candidate))
                {
                    return candidate;
                }
            }

            return -1;
        }

        private bool CanSelectItem(int index)
        {
            if (_items == null || index < 0 || index >= _items.Length) return false;
            return string.IsNullOrEmpty(GetUnavailableReason(_items[index]));
        }

        private bool CanBrowseItem(int index)
        {
            return _items != null && index >= 0 && index < _items.Length && _items[index] != null;
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < _itemViews.Count; i++)
            {
                if (_itemViews[i] != null)
                {
                    _itemViews[i].SetSelected(i == _selectedIndex && CanSelectItem(i));
                }
            }

            RefreshPreview();
            ScrollSelectedItemIntoView();
        }

        private void HandleGameStateChanged(GameState gameState)
        {
            SetVisible(gameState == GameState.Shop);
            RefreshAll();
            if (gameState == GameState.Shop)
            {
                CloseShopIfNoPurchasableItems();
            }
        }

        private void HandleCashChanged(int cash)
        {
            if (!CanSelectItem(_selectedIndex))
            {
                SelectFirstAvailableItem();
            }

            RefreshAll();
            CloseShopIfNoPurchasableItems();
        }

        private void RefreshVisibilityFromGameState()
        {
            GameState state = GameManager.Instance != null
                ? GameManager.Instance.CurrentState
                : GameState.Playing;

            HandleGameStateChanged(state);
        }

        private void RefreshCash()
        {
            if (_cashText == null) return;

            int cash = RunStatsManager.Instance != null
                ? RunStatsManager.Instance.Cash
                : 0;

            _cashText.text = string.Format(_cashFormat, cash);
        }

        private void RefreshItemViews()
        {
            for (int i = 0; i < _itemViews.Count; i++)
            {
                if (_itemViews[i] != null)
                {
                    _itemViews[i].Refresh();
                }
            }
        }

        private void SetVisible(bool isVisible, bool immediate = false)
        {
            if (_transition != null)
            {
                if (immediate)
                {
                    if (isVisible)
                    {
                        _transition.ShowImmediate();
                    }
                    else
                    {
                        _transition.HideImmediate();
                    }
                }
                else if (isVisible)
                {
                    _transition.Show();
                }
                else
                {
                    _transition.Hide();
                }

                return;
            }

            if (_root != null && _root != gameObject)
            {
                _root.SetActive(isVisible);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = isVisible ? 1f : 0f;
                _canvasGroup.interactable = isVisible;
                _canvasGroup.blocksRaycasts = isVisible;
            }
        }

        private void ResolveCanvasGroup()
        {
            if (_canvasGroup != null) return;

            if (_root != null)
            {
                _canvasGroup = _root.GetComponent<CanvasGroup>();
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void ResolveTransition()
        {
            if (_transition == null && _root != null)
            {
                _transition = _root.GetComponent<UiPanelTransition>();
            }

            if (_transition == null)
            {
                _transition = GetComponentInChildren<UiPanelTransition>(true);
            }

            if (_transition == null && _root != null)
            {
                _transition = _root.AddComponent<UiPanelTransition>();
            }

            if (_transition == null) return;

            RectTransform panel = _root != null
                ? _root.transform as RectTransform
                : transform as RectTransform;

            _transition.Configure(_root, _canvasGroup, panel);
        }

        private void ResolveItemScrollRect()
        {
            if (_itemScrollRect == null && _itemContainer != null)
            {
                _itemScrollRect = _itemContainer.GetComponentInParent<ScrollRect>(true);
            }

            if (_itemScrollRect == null) return;

            if (_itemScrollRect.content == null && _itemContainer is RectTransform content)
            {
                _itemScrollRect.content = content;
            }

            _itemScrollRect.horizontal = false;
            _itemScrollRect.vertical = true;
        }

        private void ScrollSelectedItemIntoView()
        {
            if (_itemScrollRect == null || _itemScrollRect.content == null || _itemScrollRect.viewport == null)
            {
                return;
            }

            if (_selectedIndex < 0 || _selectedIndex >= _itemViews.Count || _itemViews[_selectedIndex] == null)
            {
                return;
            }

            RectTransform selectedTransform = _itemViews[_selectedIndex].transform as RectTransform;
            if (selectedTransform == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            RectTransform viewport = _itemScrollRect.viewport;
            RectTransform content = _itemScrollRect.content;
            Bounds selectedBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, selectedTransform);

            float viewportTop = viewport.rect.yMax;
            float viewportBottom = viewport.rect.yMin;
            Vector2 anchoredPosition = content.anchoredPosition;

            if (selectedBounds.max.y > viewportTop)
            {
                anchoredPosition.y -= selectedBounds.max.y - viewportTop;
            }
            else if (selectedBounds.min.y < viewportBottom)
            {
                anchoredPosition.y += viewportBottom - selectedBounds.min.y;
            }
            else
            {
                return;
            }

            content.anchoredPosition = anchoredPosition;
            Canvas.ForceUpdateCanvases();
        }

        private void RefreshPreview()
        {
            ShopItem selectedItem = CanBrowseItem(_selectedIndex)
                ? _items[_selectedIndex]
                : null;

            if (selectedItem == null)
            {
                SetText(_descriptionText, "");
                SetPreviewImage(null, Color.clear);
                return;
            }

            SetText(_descriptionText, selectedItem.Description);
            SetPreviewImage(selectedItem.PreviewSprite, selectedItem.PreviewTint);
        }

        private void SetPreviewImage(Sprite sprite, Color tint)
        {
            if (_previewImage == null) return;

            _previewImage.sprite = sprite;
            _previewImage.color = sprite != null ? tint : Color.clear;
            _previewImage.enabled = sprite != null;
            _previewImage.preserveAspect = true;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }

        private void CloseShopIfNoPurchasableItems()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsShopOpen) return;
            if (HasPurchasableItem()) return;

            LeaveShop();
        }

        private bool HasPurchasableItem()
        {
            if (_items == null) return false;

            for (int i = 0; i < _items.Length; i++)
            {
                if (CanSelectItem(i))
                {
                    return true;
                }
            }

            return false;
        }

        private void TrySubscribeToGameManager()
        {
            if (_gameStateChanged != null || _isSubscribedToGameManager || GameManager.Instance == null)
            {
                return;
            }

            GameManager.Instance.StateChanged += HandleGameStateChanged;
            _isSubscribedToGameManager = true;
        }

        private void UnsubscribeFromGameManager()
        {
            if (!_isSubscribedToGameManager || GameManager.Instance == null)
            {
                _isSubscribedToGameManager = false;
                return;
            }

            GameManager.Instance.StateChanged -= HandleGameStateChanged;
            _isSubscribedToGameManager = false;
        }
    }
}
