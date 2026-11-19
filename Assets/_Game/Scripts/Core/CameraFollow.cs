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

        public static CameraFollow Main { get; private set; }
        public GameConfig Config => config;
        // Kameranın şu an takip ettiği nokta (oyuncu; törende gemi). Uzaktaki yazılar buna göre kaybolur.
        public Vector3 FocusPoint => target != null ? target.position : transform.position;

        public Quaternion Rotation => Quaternion.Euler(config.CameraPitch, config.CameraYaw, 0f);

        private void Awake()
        {
            cam = GetComponent<Camera>();
            Main = this;
        }

        private void Start()
        {
            // İlk karede kayarak gelmesin, doğrudan yerine otursun.
            transform.rotation = Rotation;
            if (target != null) transform.position = GoalPosition();
        }

        private bool hasShot;
        private Vector3 shotPosition;
        private Quaternion shotRotation;
        private float shotBlend;

        // Sinematik çekim: kamera bu konum ve açıya yumuşakça gider (aşama kutlaması, final töreni).
        // blendSpeed büyüdükçe daha hızlı yaklaşır. ClearShot ile normal takibe döner.
        public void SetShot(Vector3 position, Quaternion rotation, float blendSpeed)
        {
            hasShot = true;
            shotPosition = position;
            shotRotation = rotation;
            shotBlend = blendSpeed;
        }

        public void ClearShot()
        {
            hasShot = false;
        }

        public bool HasShot => hasShot;

        // Kamerayı başka bir nesneye bağlar (kaçışta gemi).
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            cam.fieldOfView = config.CameraFov;
            if (hasShot)
            {
                float k = 1f - Mathf.Exp(-shotBlend * Time.unscaledDeltaTime);
                transform.position = Vector3.Lerp(transform.position, shotPosition, k);
                transform.rotation = Quaternion.Slerp(transform.rotation, shotRotation, k);
                return;
            }
            // Çekimden normal takibe yumuşak dönüş.
            transform.rotation = Quaternion.Slerp(transform.rotation, Rotation, 1f - Mathf.Exp(-config.CameraFollowSpeed * Time.unscaledDeltaTime));
            if (target == null) return;

            // Kare hızından bağımsız yumuşak takip.
            float blend = 1f - Mathf.Exp(-config.CameraFollowSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, GoalPosition(), blend);
        }

        private Vector3 GoalPosition()
        {
            Quaternion yaw = Quaternion.Euler(0f, config.CameraYaw, 0f);
            Vector3 focus = target.position + yaw * Vector3.forward * config.CameraFocusOffset;
            return focus - Rotation * Vector3.forward * Distance;
        }

        // ---------------------------------------------------------------- Mesafe ön ayarları (Yakın / Normal / Uzak)

        public static readonly string[] ZoomNames = { "Yakın", "Normal", "Uzak" };
        private const string ZoomPref = "kayipada_camera_zoom";

        // Seçili ön ayar (cihazda hatırlanır; yoksa GameConfig.CameraZoomDefault).
        public int ZoomIndex
        {
            get
            {
                int n = config.CameraZoomPresets != null ? config.CameraZoomPresets.Length : 0;
                if (n == 0) return 0;
                return Mathf.Clamp(PlayerPrefs.GetInt(ZoomPref, config.CameraZoomDefault), 0, n - 1);
            }
        }

        public string ZoomName => ZoomIndex < ZoomNames.Length ? ZoomNames[ZoomIndex] : (ZoomIndex + 1).ToString();

        // Takip mesafesi = temel mesafe × ön ayar çarpanı.
        public float Distance
        {
            get
            {
                float[] p = config.CameraZoomPresets;
                return config.CameraDistance * (p != null && p.Length > 0 ? p[ZoomIndex] : 1f);
            }
        }

        // Sıradaki ön ayara geç (Yakın → Normal → Uzak → Yakın).
        public void CycleZoom()
        {
            int n = config.CameraZoomPresets != null ? config.CameraZoomPresets.Length : 0;
            if (n == 0) return;
            PlayerPrefs.SetInt(ZoomPref, (ZoomIndex + 1) % n);
        }

        public void SetZoom(int index)
        {
            PlayerPrefs.SetInt(ZoomPref, index);
        }
    }
}
