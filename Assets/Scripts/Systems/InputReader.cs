using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Warblade.Systems
{
    /// <summary>
    /// ScriptableObject that wraps the project's InputActionAsset and exposes
    /// a stable, decoupled API the rest of the game reads from.
    ///
    /// Movement and fire are polled (MoveAxis, FireHeld) so the shooting layer
    /// can switch between "tap to shoot" and "hold to autofire" via a single
    /// code path once the autofire buff exists. Pause is a one-shot event.
    /// </summary>
    [CreateAssetMenu(menuName = "Warblade/Systems/Input Reader", fileName = "InputReader")]
    public class InputReader : ScriptableObject
    {
        [SerializeField] private InputActionAsset _inputActions;

        private InputActionMap _playerMap;
        private InputActionMap _uiMap;
        private InputAction _moveAction;
        private InputAction _fireAction;
        private InputAction _pauseAction;
        private InputAction _navigateAction;

        public float MoveAxis => _moveAction?.ReadValue<float>() ?? 0f;
        public bool FireHeld => _fireAction?.IsPressed() ?? false;
        public bool FirePressedThisFrame => _fireAction?.WasPressedThisFrame() ?? false;
        public float ShopNavigateAxis => ReadShopNavigateAxis();

        public event Action PausePressed;

        private void OnEnable()
        {
            if (_inputActions == null)
            {
                Debug.LogError($"[{nameof(InputReader)}] No InputActionAsset assigned on '{name}'.");
                return;
            }

            _playerMap = _inputActions.FindActionMap("Player", throwIfNotFound: true);
            _uiMap = _inputActions.FindActionMap("UI", throwIfNotFound: false);
            _moveAction = _playerMap.FindAction("Move", throwIfNotFound: true);
            _fireAction = _playerMap.FindAction("Fire", throwIfNotFound: true);
            _pauseAction = _playerMap.FindAction("Pause", throwIfNotFound: true);
            _navigateAction = _uiMap?.FindAction("Navigate", throwIfNotFound: false);

            _pauseAction.performed += OnPausePerformed;
            _playerMap.Enable();
            _uiMap?.Enable();
        }

        private void OnDisable()
        {
            if (_pauseAction != null)
            {
                _pauseAction.performed -= OnPausePerformed;
            }
            _playerMap?.Disable();
            _uiMap?.Disable();
        }

        private void OnPausePerformed(InputAction.CallbackContext _) => PausePressed?.Invoke();

        private float ReadShopNavigateAxis()
        {
            return _navigateAction?.ReadValue<Vector2>().y ?? 0f;
        }
    }
}
