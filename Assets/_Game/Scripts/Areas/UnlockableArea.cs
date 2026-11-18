// UnlockableArea.cs
// Açılabilir bir bölgenin/binanın sahnedeki kökü. Kapalıyken içeriği (kaynak noktaları, bina, makineler) gizlidir;
// yerde sadece küçük doğal ipuçları ("Locked": taş parçaları, yabani ot, kırık sandık...) vardır.
// Açılınca (2-3 sn): bölgede kamera alana odaklanır → rüzgârla yaprak girdabı → ipuçları toz bulutuyla kaybolur →
// zemin yayılarak yenilenir ("Ground"), ağaçlar yerden fidan olarak çıkıp büyür, kayalar topraktan yükselir, süsler
// zıplar; binada parçalar alttan üste kurulur (temel → gövde → çatı), sonra makine ilk kez çalışır → kamera döner.
// Kayıttan yüklenince açık alanlar efektsiz, hazır gelir. Süreler GameConfig > "Bölge açılma efekti".

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public class UnlockableArea : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private AreaDefinition area;
        [Tooltip("Bölge açılınca görünecek nesneler (kaynak noktaları, bina vb.).")]
        [SerializeField] private GameObject content;
        [Tooltip("Bölge kapalıyken görünen nesneler (küçük doğal ipuçları).")]
        [SerializeField] private GameObject lockedVisual;
        [Tooltip("Açılınca kamera bu alana odaklansın mı (bölge, adacık).")]
        [SerializeField] private bool focusCamera;

        public AreaDefinition Area => area;
        public bool IsRevealing { get; private set; }

        // Bu kök bir bina mı (içeriğinde doğrudan bir işleme binası var) yoksa bölge mi?
        public bool HoldsBuilding
        {
            get
            {
                if (content == null) return false;
                foreach (Transform child in content.transform)
                {
                    if (child.GetComponent<ProcessingBuilding>() != null) return true;
                }
                return false;
            }
        }

        private void Awake()
        {
            if (area == null || content == null)
            {
                Debug.LogError(name + ": Area veya Content atanmamış!", this);
                return;
            }

            AreaManager.Instance.AreaUnlocked += OnAreaUnlocked;
            bool open = AreaManager.Instance.IsUnlocked(area);
            content.SetActive(open);
            if (lockedVisual != null) lockedVisual.SetActive(!open);
        }

        private void OnDestroy()
        {
            if (AreaManager.Instance != null)
            {
                AreaManager.Instance.AreaUnlocked -= OnAreaUnlocked;
            }
        }

        private void OnAreaUnlocked(AreaDefinition unlocked)
        {
            if (unlocked != area || content.activeSelf) return;
            if (isActiveAndEnabled) StartCoroutine(Reveal());
            else { content.SetActive(true); if (lockedVisual != null) lockedVisual.SetActive(false); }
        }

        private IEnumerator Reveal()
        {
            IsRevealing = true;
            float total = config.RevealTime;
            Vector3 center = CenterOf();
            CameraFollow cam = CameraFollow.Main;
            bool shot = false;
            if (focusCamera && cam != null && !cam.HasShot)
            {
                Vector3 pos = center + new Vector3(0f, config.RevealShotHeight, -config.RevealShotDistance);
                cam.SetShot(pos, Quaternion.LookRotation(center - pos), 3f);
                shot = true;
            }
            // 1) Rüzgârla yaprak girdabı.
            if (Effects.Instance != null) Effects.Instance.LeafSwirl(center, focusCamera ? 3.2f : 1.8f);
            AudioManager.Play(SoundId.Whoosh, 0.8f);
            yield return new WaitForSeconds(total * 0.18f);

            // 2) İpuçları toz bulutuyla kaybolur.
            if (lockedVisual != null && lockedVisual.activeSelf)
            {
                foreach (Transform t in lockedVisual.transform)
                    if (Effects.Instance != null && t.gameObject.activeSelf) Effects.Instance.Dust(t.position);
                Tween.PopOut(lockedVisual.transform, 0.25f, () => lockedVisual.SetActive(false));
            }
            yield return new WaitForSeconds(total * 0.08f);

            // 3) İçerik: önce zemin yayılır, sonra ağaçlar/kayalar yerden çıkar, süsler zıplar, binalar kurulur.
            content.SetActive(true);
            Transform root = content.transform;
            float step = total * 0.45f / Mathf.Max(1, root.childCount);
            int k = 0;
            foreach (Transform child in root)
            {
                float delay = child.name == "Ground" ? 0f : 0.15f + k * step;
                if (child.name != "Ground") k++;
                ResourceNode node = child.GetComponent<ResourceNode>();
                if (node != null) { node.PlayGrowIn(delay); continue; }
                if (child.GetComponent<ProcessingBuilding>() != null) { StartCoroutine(BuildUp(child, delay)); continue; }
                Tween.PopIn(child, child.localScale, child.name == "Ground" ? total * 0.3f : config.PopInDuration, delay);
            }
            yield return new WaitForSeconds(total * 0.74f);
            if (shot && cam != null) cam.ClearShot();
            IsRevealing = false;
        }

        // Bina parça parça kurulur: renderer'lar alttan üste sırayla belirir (temel → gövde → çatı).
        private IEnumerator BuildUp(Transform building, float delay)
        {
            List<Renderer> parts = new List<Renderer>();
            foreach (Renderer r in building.GetComponentsInChildren<Renderer>())
                if (!(r is ParticleSystemRenderer) && r.GetComponentInParent<Canvas>() == null && r.gameObject.layer != 5) parts.Add(r);
            parts.Sort((a, b) => a.bounds.min.y.CompareTo(b.bounds.min.y));
            Vector3[] scales = new Vector3[parts.Count];
            for (int i = 0; i < parts.Count; i++)
            {
                scales[i] = parts[i].transform.localScale;
                parts[i].transform.localScale = Vector3.zero;
            }
            yield return new WaitForSeconds(delay);
            float span = config.RevealTime * 0.5f;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] == null) continue;
                Tween.PopIn(parts[i].transform, scales[i], 0.25f, span * i / Mathf.Max(1, parts.Count));
            }
            yield return new WaitForSeconds(span + 0.3f);
            if (Effects.Instance != null) Effects.Instance.Sparkle(building.position + Vector3.up * 1.5f);
            Tween.PunchScale(building, 0.08f, 0.3f);
        }

        private Vector3 CenterOf()
        {
            Bounds b = new Bounds(transform.position, Vector3.zero);
            bool any = false;
            foreach (Transform t in content.transform)
            {
                if (!any) { b = new Bounds(t.position, Vector3.zero); any = true; }
                else b.Encapsulate(t.position);
            }
            return any ? new Vector3(b.center.x, 0f, b.center.z) : transform.position;
        }

#if UNITY_EDITOR
        public GameObject EditorContent => content;
        public GameObject EditorLocked => lockedVisual;
#endif
    }
}
