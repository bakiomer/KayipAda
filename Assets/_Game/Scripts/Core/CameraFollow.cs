// CameraFollow.cs
// Kamerayı oyuncunun peşinden yumuşakça kaydırır.
// Kamera oyuncunun biraz yukarısına bakar; böylece dikey ekranda oyuncu ekranın biraz altında durur
// ve önündeki (yukarıdaki) alan daha çok görünür. Tüm değerler GameConfig'ten okunur.

using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform target;

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
            if (target == null) return;

            cam.orthographicSize = config.CameraSize;
            transform.position = Vector3.Lerp(transform.position, GoalPosition(), config.CameraFollowSpeed * Time.deltaTime);
        }

        private Vector3 GoalPosition()
        {
            // Z konumu korunur, yoksa 2D sahne görünmez olur.
            return new Vector3(target.position.x, target.position.y + config.CameraOffsetY, transform.position.z);
        }
    }
}
