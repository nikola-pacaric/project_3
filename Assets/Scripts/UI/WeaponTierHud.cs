using TMPro;
using UnityEngine;
using Warblade.Data;
using Warblade.Data.Events;
using Warblade.Managers;

namespace Warblade.UI
{
    public class WeaponTierHud : MonoBehaviour
    {
        [SerializeField] private WeaponTierEventChannel _weaponTierChanged;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private string _format = "Weapon: {0}";

        private void Awake()
        {
            Refresh();
        }

        private void OnEnable()
        {
            if (_weaponTierChanged != null)
            {
                _weaponTierChanged.OnEventRaised += HandleWeaponTierChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_weaponTierChanged != null)
            {
                _weaponTierChanged.OnEventRaised -= HandleWeaponTierChanged;
            }
        }

        private void HandleWeaponTierChanged(WeaponTier weaponTier)
        {
            SetText(weaponTier);
        }

        private void Refresh()
        {
            WeaponTier weaponTier = RunStatsManager.Instance != null
                ? RunStatsManager.Instance.WeaponTier
                : WeaponTier.Single;

            SetText(weaponTier);
        }

        private void SetText(WeaponTier weaponTier)
        {
            if (_text != null)
            {
                _text.text = string.Format(_format, weaponTier);
            }
        }
    }
}
