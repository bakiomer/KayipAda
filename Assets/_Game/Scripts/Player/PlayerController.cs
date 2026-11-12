// PlayerController.cs
// Oyuncuyu sanal joystick'e göre hareket ettirir ve oyuncu ayarlarını (hız, taşıma kapasitesi)
// GameConfig'ten uygular. Editörde test kolaylığı için joystick boştaysa WASD / ok tuşları da çalışır.
// Oyuncu yürünebilir alanın (moveArea, sahne kurma komutu adaya göre ayarlar) dışına çıkamaz.

using UnityEngine;
using UnityEngine.InputSystem;

namespace KayipAda
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CarryStack))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private VirtualJoystick joystick;
        [Tooltip("Oyuncunun yürüyebileceği dikdörtgen (dünya koordinatı). Harita yerleşimine aittir.")]
        [SerializeField] private Rect moveArea = new Rect(-5f, -5f, 10f, 10f);

        private Rigidbody2D body;
        private CarryStack stack;
        private Vector2 input;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            stack = GetComponent<CarryStack>();
            ApplyStats();
        }

        private void Update()
        {
            // Her karede uygulanır ki Play sırasında GameConfig'te yapılan değişiklik hemen etkilesin.
            ApplyStats();

            input = joystick != null ? joystick.Direction : Vector2.zero;
            if (input == Vector2.zero)
            {
                input = ReadKeyboard();
            }
        }

        private void FixedUpdate()
        {
            Vector2 next = body.position + input * config.PlayerSpeed * Time.fixedDeltaTime;
            next.x = Mathf.Clamp(next.x, moveArea.xMin, moveArea.xMax);
            next.y = Mathf.Clamp(next.y, moveArea.yMin, moveArea.yMax);
            body.MovePosition(next);
        }

        private void ApplyStats()
        {
            stack.Capacity = config.CarryCapacity;
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
