// Haptics.cs
// Telefon titreşimi (kutlamalarda). Ayarlardan kapatılabilir: tercih cihazda saklanır (PlayerPrefs "titresim").
// Editörde ve titreşimsiz cihazlarda hiçbir şey yapmaz.

using UnityEngine;

namespace KayipAda
{
    public static class Haptics
    {
        private const string PrefKey = "titresim";

        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(PrefKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void Vibrate()
        {
            if (!Enabled) return;
#if UNITY_ANDROID || UNITY_IOS
            if (!Application.isEditor) Handheld.Vibrate();
#endif
        }
    }
}
