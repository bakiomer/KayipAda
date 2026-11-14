// AppSettings.cs
// Oyun açılırken uygulama ayarlarını yapar: telefonlarda varsayılan 30 FPS yerine hedef kare hızı (60),
// ekranın kararmaması. Sahnede Managers nesnesinin üstündedir.

using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-200)]
    public class AppSettings : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;

        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }
}
