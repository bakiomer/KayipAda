// FlyingVisual.cs
// Bir noktadan diğerine uçup kaybolan basit görsel (örn. açma karesine akan para parçaları).
// Prefab'a eklenir; Instantiate edildikten sonra Launch çağrılır.

using System.Collections;
using UnityEngine;

namespace KayipAda
{
    public class FlyingVisual : MonoBehaviour
    {
        public void Launch(Vector3 from, Vector3 to, float duration)
        {
            transform.position = from;
            StartCoroutine(FlyRoutine(from, to, duration));
        }

        private IEnumerator FlyRoutine(Vector3 from, Vector3 to, float duration)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                transform.position = Vector3.Lerp(from, to, t);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
