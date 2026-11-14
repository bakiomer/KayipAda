// PlayerController.cs
// Oyuncuyu sanal joystick'e göre yerde (XZ düzleminde) hareket ettirir ve oyuncu ayarlarını
// (hız, taşıma kapasitesi) GameConfig'ten uygular. Editörde joystick boştaysa WASD / ok tuşları da çalışır.
// Oyuncu binaların içinden geçemez (NavObstacle) ve adadan denize çıkamaz (IslandShape).
// Görünüş (animasyon, dönme, toz, adım sesi) CharacterVisual'dadır.

using UnityEngine;
using UnityEngine.InputSystem;

namespace KayipAda
{
    [RequireComponent(typeof(Rigidbody), typeof(CarryStack))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private VirtualJoystick joystick;
        [Tooltip("Oyuncunun gövde yarıçapı (engellere bu kadar yaklaşır).")]
        [SerializeField] private float radius = 0.4f;
        [Tooltip("Kıyıdan ne kadar içeride kalır (metre). Harita yerleşimine aittir.")]
        [SerializeField] private float coastMargin = 1.2f;

        // Açık bir panel (geliştirmeler, kaçış ekranı) varken oyuncu yürümez.
        public static bool InputBlocked { get; set; }

        private Rigidbody body;
        private CarryStack stack;
        private Vector3 move;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            stack = GetComponent<CarryStack>();
            stack.IsPlayer = true;
            ApplyStats();
        }

        private void Update()
        {
            // Her karede uygulanır ki Play sırasında GameConfig'te yapılan değişiklik hemen etkilesin.
            ApplyStats();

            Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
            if (input == Vector2.zero) input = ReadKeyboard();
            if (InputBlocked) input = Vector2.zero; // geliştirme paneli açıkken yürümez

            // Joystick yukarısı = kameranın baktığı yön.
            Quaternion yaw = Quaternion.Euler(0f, config.CameraYaw, 0f);
            move = yaw * new Vector3(input.x, 0f, input.y);
        }

        private void FixedUpdate()
        {
            float speed = config.PlayerSpeed * UpgradeManager.Multiplier(UpgradeStat.PlayerSpeed);
            Vector3 next = body.position + move * (speed * Time.fixedDeltaTime);
            Vector2 flat = new Vector2(next.x, next.z);
            flat = NavObstacle.PushOut(flat, radius);
            if (IslandShape.Instance != null) flat = IslandShape.Instance.ClampInside(flat, coastMargin);
            body.MovePosition(new Vector3(flat.x, 0f, flat.y));
        }

        private void ApplyStats()
        {
            stack.Capacity = config.CarryCapacity + UpgradeManager.Extra(UpgradeStat.PlayerCapacity, config.CarryCapacity);
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
