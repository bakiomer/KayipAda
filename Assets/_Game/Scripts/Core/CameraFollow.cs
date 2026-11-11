// CameraFollow.cs
// Kamerayı hedefin (oyuncunun) peşinden yumuşakça kaydırır.
// Kameranın Z konumu korunur, yoksa 2D sahne görünmez olur.

using UnityEngine;

namespace KayipAda
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 6f;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, goal, smoothSpeed * Time.deltaTime);
        }
    }
}
