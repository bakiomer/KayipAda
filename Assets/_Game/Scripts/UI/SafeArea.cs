// SafeArea.cs
// Arayüzü telefonun çentik/kamera deliği gibi kapalı bölgelerinden uzak tutar.
// Bu bileşenin olduğu RectTransform, ekranın "güvenli alanına" (Screen.safeArea) oturtulur.

using UnityEngine;

namespace KayipAda
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private Rect applied;

        private void Update()
        {
            Rect safe = Screen.safeArea;
            if (safe == applied || Screen.width <= 0 || Screen.height <= 0) return;
            applied = safe;

            RectTransform rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            rt.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
