using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Managers;
using Warblade.Systems;

namespace Warblade.UI
{
    public class ShopController : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private ShopItemView _itemViewPrefab;
        [SerializeField] private ShopItem[] _items;
        [SerializeField] private TMP_Text _cashText;
        [SerializeField] private InputReader _input;
        [SerializeField] private string _cashFormat = "Cash: ${0}";
        [SerializeField] private bool _buildItemViewsOnAwake = true;
        [SerializeField, Range(0.1f, 1f)] private float _navigationThreshold = 0.5f;
        [SerializeField] private bool _wrapSelection = true;

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

            if (_buildItemViewsOnAwake)
            {
                BuildItemViews();
            }

            SetVisible(false);
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
            _selectedIndex = FindFirstAvailableIndex();
            RefreshSelection();
        }

        private int FindFirstAvailableIndex()
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

        private void RefreshSelection()
        {
            for (int i = 0; i < _itemViews.Count; i++)
            {
                if (_itemViews[i] != null)
                {
                    _itemViews[i].SetSelected(i == _selectedIndex && CanSelectItem(i));
                }
            }
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

        private void SetVisible(bool isVisible)
        {
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
