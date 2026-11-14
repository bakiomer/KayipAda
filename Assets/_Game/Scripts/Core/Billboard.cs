// Billboard.cs
// Nesneyi her karede kameraya döndürür (dünyadaki yazılar, bina üstündeki halka).
// Kamera sabit açılı olduğu için yazılar her zaman düz ve okunaklı görünür.

using UnityEngine;

namespace KayipAda
{
    public class Billboard : MonoBehaviour
    {
        private Transform cam;

        private void LateUpdate()
        {
            if (cam == null)
            {
                if (Camera.main == null) return;
                cam = Camera.main.transform;
            }
            transform.rotation = cam.rotation;
        }
    }
}
