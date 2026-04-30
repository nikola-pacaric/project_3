using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _speed = 8f;
    [SerializeField] private float _minX = -8.5f;
    [SerializeField] private float _maxX = 8.5f;

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");

        Vector3 position = transform.position;
        position.x += horizontal * _speed * Time.deltaTime;
        position.x = Mathf.Clamp(position.x, _minX, _maxX);
        transform.position = position;
    }
}
