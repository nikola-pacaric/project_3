using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private float _spawnYOffset = 0.5f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 spawnPosition = transform.position + Vector3.up * _spawnYOffset;
            Instantiate(_bulletPrefab, spawnPosition, Quaternion.identity);
        }
    }
}
