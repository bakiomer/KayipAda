// CameraFollow.cs
// Popüler idle arcade oyunlarındaki kamera: yukarıdan ~50 derece eğik, dar açılı perspektif,
// oyuncuyu yumuşakça takip eder. Oyuncunun biraz önüne baktığı için oyuncu ekranın biraz altında durur.
// Tüm ayarlar (açı, uzaklık, görüş açısı, hız) GameConfig'tedir ve Play sırasında değiştirilebilir.

using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private Transform target;

        private Camera cam;

        public Quaternion Rotation => Quaternion.Euler(config.CameraPitch, config.CameraYaw, 0f);

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            // İlk karede kayarak gelmesin, doğrudan yerine otursun.
            if (target != null) transform.position = GoalPosition();
        }

        // Kamerayı başka bir nesneye bağlar (kaçışta gemi).
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            cam.fieldOfView = config.CameraFov;
            transform.rotation = Rotation;
            if (target == null) return;

            // Kare hızından bağımsız yumuşak takip.
            float blend = 1f - Mathf.Exp(-config.CameraFollowSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, GoalPosition(), blend);
        }

        private Vector3 GoalPosition()
        {
            Quaternion yaw = Quaternion.Euler(0f, config.CameraYaw, 0f);
            Vector3 focus = target.position + yaw * Vector3.forward * config.CameraFocusOffset;
            return focus - Rotation * Vector3.forward * config.CameraDistance;
        }
    }
}
