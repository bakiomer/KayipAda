// DayNightController.cs
// Gündüz ↔ gece geçişi: güneş rengi/gücü/açısı, ortam ışığı, sis ve kamera arka planı yumuşakça değişir.
// Gündüz değerleri oyun başındaki sahne ayarlarından alınır; gece değerleri ShipConfig'tedir.

using UnityEngine;

namespace KayipAda
{
    public class DayNightController : MonoBehaviour
    {
        public static DayNightController Instance { get; private set; }

        [SerializeField] private ShipConfig ship;
        [SerializeField] private Light sun;
        [SerializeField] private Camera cam;

        private Color daySun, dayAmbientSky, dayAmbientEquator, dayAmbientGround, dayFog, dayBackground;
        private float dayIntensity;
        private Quaternion dayRotation;
        private float blend; // 0 = gündüz, 1 = gece

        public bool IsNight => blend > 0.5f;

        private void Awake()
        {
            Instance = this;
            daySun = sun.color;
            dayIntensity = sun.intensity;
            dayRotation = sun.transform.rotation;
            dayAmbientSky = RenderSettings.ambientSkyColor;
            dayAmbientEquator = RenderSettings.ambientEquatorColor;
            dayAmbientGround = RenderSettings.ambientGroundColor;
            dayFog = RenderSettings.fogColor;
            dayBackground = cam != null ? cam.backgroundColor : Color.black;
        }

        public void ToNight() => Transition(1f);
        public void ToDay() => Transition(0f);

        private void Transition(float target)
        {
            float from = blend;
            Tween.Value(this, ship.DayNightTransition, t => Apply(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t))));
        }

        private void Apply(float k)
        {
            blend = k;
            sun.color = Color.Lerp(daySun, ship.NightSunColor, k);
            sun.intensity = Mathf.Lerp(dayIntensity, ship.NightSunIntensity, k);
            sun.transform.rotation = Quaternion.Slerp(dayRotation, dayRotation * Quaternion.Euler(-18f, 40f, 0f), k);
            RenderSettings.ambientSkyColor = Color.Lerp(dayAmbientSky, ship.NightAmbientSky, k);
            RenderSettings.ambientEquatorColor = Color.Lerp(dayAmbientEquator, ship.NightAmbientEquator, k);
            RenderSettings.ambientGroundColor = Color.Lerp(dayAmbientGround, ship.NightAmbientGround, k);
            RenderSettings.fogColor = Color.Lerp(dayFog, ship.NightFog, k);
            if (cam != null) cam.backgroundColor = Color.Lerp(dayBackground, ship.NightFog * 0.8f, k);
        }
    }
}
