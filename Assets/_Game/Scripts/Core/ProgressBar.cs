// ProgressBar.cs
// Dünyada duran küçük ilerleme çubuğu (örn. binanın üstünde işleme ilerlemesi).
// İki kutudan oluşur: arka plan ve soldan sağa dolan "fill". SetProgress(0..1) ile güncellenir.

using UnityEngine;

namespace KayipAda
{
    public class ProgressBar : MonoBehaviour
    {
        [Tooltip("Dolan kısım (ortalanmış, 1 birim genişlikte kare sprite).")]
        [SerializeField] private Transform fill;
        [Tooltip("Çubuğun tam dolu genişliği (dünya birimi).")]
        [SerializeField] private float width = 1.8f;

        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            float filled = width * progress;

            // Sol kenar sabit kalsın diye: genişliği ayarla, ortasını sola kaydır.
            fill.localScale = new Vector3(filled, fill.localScale.y, 1f);
            fill.localPosition = new Vector3(-width / 2f + filled / 2f, 0f, 0f);
        }
    }
}
