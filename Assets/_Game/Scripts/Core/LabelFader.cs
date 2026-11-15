// LabelFader.cs
// Dünyadaki yazı ve işaretleri (bina adı, tabela, gemi göstergesi) duruma göre yumuşakça gösterir/gizler:
// kameranın takip ettiği noktadan (oyuncu) uzaktaysa ya da ekranın kenarına yaklaştıysa kaybolur,
// yaklaşınca belirir. Böylece yazılar ekran kenarında yarım kesik görünmez ve ekran kalabalıklaşmaz.
// Uzaklıklar GameConfig'te (LabelFadeNear / LabelFadeFar / LabelEdgeFade).

using TMPro;
using UnityEngine;

namespace KayipAda
{
    public class LabelFader : MonoBehaviour
    {
        private TMP_Text[] texts;
        private float[] textAlphas;
        private SpriteRenderer[] sprites;
        private float[] spriteAlphas;
        private CanvasGroup[] groups;
        private float shown = -1f;
        private float current = 1f;

        private void Awake()
        {
            texts = GetComponentsInChildren<TMP_Text>(true);
            textAlphas = new float[texts.Length];
            for (int i = 0; i < texts.Length; i++) textAlphas[i] = texts[i].alpha;
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
            spriteAlphas = new float[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) spriteAlphas[i] = sprites[i].color.a;
            Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
            groups = new CanvasGroup[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                groups[i] = canvases[i].GetComponent<CanvasGroup>();
                if (groups[i] == null) groups[i] = canvases[i].gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void LateUpdate()
        {
            CameraFollow follow = CameraFollow.Main;
            Camera cam = Camera.main;
            if (follow == null || cam == null) return;
            GameConfig config = follow.Config;

            // Uzaklık: takip edilen noktaya (oyuncuya) yatay uzaklık.
            Vector3 p = transform.position;
            Vector3 focus = follow.FocusPoint;
            float distance = new Vector2(p.x - focus.x, p.z - focus.z).magnitude;
            float target = 1f - Mathf.InverseLerp(config.LabelFadeNear, config.LabelFadeFar, distance);

            // Ekran kenarı: kenara yaklaştıkça kaybolur (yarım kesik görünmesin).
            Vector3 vp = cam.WorldToViewportPoint(p);
            if (vp.z <= 0f) target = 0f;
            else
            {
                float edge = Mathf.Min(Mathf.Min(vp.x, 1f - vp.x), Mathf.Min(vp.y, 1f - vp.y));
                target *= Mathf.SmoothStep(0f, 1f, edge / Mathf.Max(0.001f, config.LabelEdgeFade));
            }

            current = Mathf.MoveTowards(current, target, Time.unscaledDeltaTime * config.LabelFadeSpeed);
            if (Mathf.Abs(current - shown) < 0.01f) return;
            shown = current;
            Apply(current);
        }

        private void Apply(float a)
        {
            for (int i = 0; i < texts.Length; i++) if (texts[i] != null) texts[i].alpha = textAlphas[i] * a;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Color c = sprites[i].color;
                c.a = spriteAlphas[i] * a;
                sprites[i].color = c;
            }
            foreach (CanvasGroup g in groups) if (g != null) g.alpha = a;
        }
    }
}
