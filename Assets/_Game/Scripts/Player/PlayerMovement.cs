// PlayerMovement.cs
// Oyuncuyu sanal joystick'e göre hareket ettirir.
// Editörde test kolaylığı için joystick boştaysa WASD / ok tuşları da çalışır.
// Oyuncu adanın sınırları (moveBounds) dışına çıkamaz.

using UnityEngine;
using UnityEngine.InputSystem;

namespace KayipAda
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("Ada merkezinden yatay ve dikey en fazla ne kadar uzaklaşılabilir.")]
        [SerializeField] private Vector2 moveBounds = new Vector2(7f, 11f);

        private Rigidbody2D body;
        private Vector2 input;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            input = joystick != null ? joystick.Direction : Vector2.zero;

            if (input == Vector2.zero)
            {
                input = ReadKeyboard();
            }
        }

        private void FixedUpdate()
        {
            Vector2 next = body.position + input * moveSpeed * Time.fixedDeltaTime;
            next.x = Mathf.Clamp(next.x, -moveBounds.x, moveBounds.x);
            next.y = Mathf.Clamp(next.y, -moveBounds.y, moveBounds.y);
            body.MovePosition(next);
        }

        private Vector2 ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return Vector2.zero; // Telefonda klavye yok

            Vector2 dir = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) dir.y += 1;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) dir.y -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) dir.x += 1;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) dir.x -= 1;
            return dir.normalized;
        }
    }
}
