using UnityEngine;
using Warblade.Systems;

namespace Warblade.Entities
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [SerializeField] private float _speed = 8f;
        [SerializeField] private float _minX = -8.5f;
        [SerializeField] private float _maxX = 8.5f;

        private void Update()
        {
            if (_input == null) return;

            Vector3 position = transform.position;
            position.x += _input.MoveAxis * _speed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, _minX, _maxX);
            transform.position = position;
        }
    }
}
