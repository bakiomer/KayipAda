// CameraFollow.cs
// Kamerayı oyuncunun peşinden yumuşakça kaydırır. Kamera oyuncunun biraz yukarısına bakar;
// böylece dikey ekranda oyuncu ekranın biraz altında durur ve önündeki alan daha çok görünür.
// Ayrıca kameranın görüntüsünü üstteki arayüz çubuğunun ALTINDA bitirir: böylece arayüz,
// kamera nereye giderse gitsin hiçbir dünya nesnesinin üstünü kapatmaz.

using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform target;
        [Tooltip("Ekranın üstündeki arayüz çubuğu. Kamera görüntüsü bunun altında kalır.")]
        [SerializeField] private RectTransform topBar;

        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            // İlk karede kayarak gelmesin, doğrudan yerine otursun.
            if (target != null) transform.position = GoalPosition();
        }

        private void LateUpdate()
        {
            FitBelowTopBar();
            cam.orthographicSize = config.CameraSize;

            if (target == null) return;
            transform.position = Vector3.Lerp(transform.position, GoalPosition(), config.CameraFollowSpeed * Time.deltaTime);
        }

        private Vector3 GoalPosition()
        {
            // Z konumu korunur, yoksa 2D sahne görünmez olur.
            return new Vector3(target.position.x, target.position.y + config.CameraOffsetY, transform.position.z);
        }

        // Kameranın ekranda kapladığı alanı, üst çubuğun yüksekliği kadar aşağıdan bitirir.
        private void FitBelowTopBar()
        {
            if (topBar == null || Screen.height <= 0) return;

            // Çubuğun ekrandaki gerçek piksel yüksekliği (Canvas ölçeği dahil).
            float barPixels = topBar.rect.height * topBar.lossyScale.y;
            float barFraction = Mathf.Clamp01(barPixels / Screen.height);
            cam.rect = new Rect(0f, 0f, 1f, 1f - barFraction);
        }
    }
}
