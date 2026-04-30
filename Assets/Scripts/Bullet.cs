using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float _speed = 15f;
    [SerializeField] private float _despawnY = 6f;

    private void Update()
    {
        transform.Translate(Vector3.up * _speed * Time.deltaTime);

        if (transform.position.y > _despawnY)
        {
            Destroy(gameObject);
        }
    }
}
