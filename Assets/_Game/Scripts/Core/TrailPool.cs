// TrailPool.cs
// Karakter izlerinin ortak havuzu: kumda birkaç saniyede silinen ayak izleri, kıyıda genişleyip kaybolan su halkaları,
// çimende ara sıra uçuşan yaprak/ot parçaları. Aynı anda en fazla GameConfig.TrailMaxCount iz (en eskisi yeniden
// kullanılır); yeni nesne oluşturulmaz. GameConfig.TrailsEnabled kapalıysa hiç iz çıkmaz.

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class TrailPool : MonoBehaviour
    {
        public static TrailPool Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [Tooltip("Ayak izi (yere yatık yarı saydam dörtgen).")]
        [SerializeField] private Renderer footprintPrefab;
        [Tooltip("Su halkası (yere yatık halka).")]
        [SerializeField] private Renderer ripplePrefab;
        [Tooltip("Çimende uçuşan yaprak parçaları (Emit ile).")]
        [SerializeField] private ParticleSystem leaves;
        [Tooltip("Kum şeridinin kıyıdan genişliği (metre): bunun içinde ayak izi çıkar.")]
        [SerializeField] private float sandWidth = 3.4f;

        private class Mark
        {
            public Renderer Renderer;
            public bool Ripple;
            public float Age;
            public float Life;
            public float Alpha;
            public Color Color;
        }

        private readonly List<Mark> marks = new List<Mark>();
        private readonly Queue<Mark> footprints = new Queue<Mark>();
        private readonly Queue<Mark> ripples = new Queue<Mark>();
        private MaterialPropertyBlock block;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public float SandWidth => sandWidth;
        public int ActiveCount { get; private set; }

        private void Awake()
        {
            Instance = this;
            block = new MaterialPropertyBlock();
        }

        // Zemin türüne göre uygun izi bırakır. side: sol/sağ ayak (-1/+1).
        public void Step(Vector3 position, Vector3 forward, bool running)
        {
            if (!config.TrailsEnabled || IslandShape.Instance == null) return;
            float s = IslandShape.Instance.SignedDistance(new Vector2(position.x, position.z));
            if (s > -config.RippleCoastDistance) Spawn(ripples, ripplePrefab, true, position, forward, 1.1f);
            else if (s > -sandWidth) Spawn(footprints, footprintPrefab, false, position, forward, config.FootprintLifetime);
            else if (leaves != null && Random.value < config.GrassLeafChance * (running ? 1.5f : 1f))
            {
                ParticleSystem.EmitParams p = new ParticleSystem.EmitParams { position = position + Vector3.up * 0.1f, applyShapeToPosition = true };
                leaves.Emit(p, 2);
            }
        }

        private void Spawn(Queue<Mark> free, Renderer prefab, bool ripple, Vector3 position, Vector3 forward, float life)
        {
            if (prefab == null || config.TrailMaxCount <= 0) return;
            Mark mark;
            if (free.Count > 0) mark = free.Dequeue();
            else if (marks.Count < config.TrailMaxCount)
            {
                mark = new Mark { Renderer = Instantiate(prefab, transform), Ripple = ripple };
                mark.Color = prefab.sharedMaterial.GetColor(BaseColor);
                marks.Add(mark);
            }
            else mark = Oldest(ripple);
            if (mark == null) return;

            mark.Age = 0f;
            mark.Life = life;
            mark.Alpha = mark.Color.a;
            Transform t = mark.Renderer.transform;
            t.position = new Vector3(position.x, 0.03f, position.z);
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            t.rotation = Quaternion.Euler(90f, yaw, 0f);
            t.localScale = prefab.transform.localScale * (ripple ? 0.4f : 1f);
            mark.Renderer.gameObject.SetActive(true);
            Apply(mark, mark.Alpha);
        }

        // Havuz doluysa aynı türden en eski iz yeniden kullanılır.
        private Mark Oldest(bool ripple)
        {
            Mark oldest = null;
            foreach (Mark m in marks)
            {
                if (m.Ripple != ripple || !m.Renderer.gameObject.activeSelf) continue;
                if (oldest == null || m.Age > oldest.Age) oldest = m;
            }
            return oldest;
        }

        private void Update()
        {
            int active = 0;
            float dt = Time.deltaTime;
            foreach (Mark m in marks)
            {
                if (!m.Renderer.gameObject.activeSelf) continue;
                m.Age += dt;
                float k = m.Age / m.Life;
                if (k >= 1f)
                {
                    m.Renderer.gameObject.SetActive(false);
                    (m.Ripple ? ripples : footprints).Enqueue(m);
                    continue;
                }
                active++;
                if (m.Ripple)
                {
                    m.Renderer.transform.localScale = ripplePrefab.transform.localScale * Mathf.Lerp(0.4f, 1.4f, Mathf.Sqrt(k));
                    Apply(m, m.Color.a * (1f - k));
                }
                else Apply(m, m.Color.a * (k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f)); // son kısımda silinir
            }
            ActiveCount = active;
        }

        private void Apply(Mark m, float alpha)
        {
            m.Renderer.GetPropertyBlock(block);
            block.SetColor(BaseColor, new Color(m.Color.r, m.Color.g, m.Color.b, alpha));
            m.Renderer.SetPropertyBlock(block);
        }
    }
}
