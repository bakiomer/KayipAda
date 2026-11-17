// SeaSettings.cs
// Deniz ve kıyı shader'larının yoğunluklarını GameConfig'ten global değişken olarak ayarlar (kıyıya vuran dalganın hızı
// ve ulaştığı mesafe, köpük, ıslak kum, yüzey dalgalanması, güneş parıltıları). Play sırasında değişiklik hemen etkiler.

using UnityEngine;

namespace KayipAda
{
    [DefaultExecutionOrder(-90)]
    public class SeaSettings : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private static readonly int ShoreSpeed = Shader.PropertyToID("_KA_ShoreSpeed");
        private static readonly int ShoreReach = Shader.PropertyToID("_KA_ShoreReach");
        private static readonly int ShoreFoam = Shader.PropertyToID("_KA_ShoreFoam");
        private static readonly int WetSand = Shader.PropertyToID("_KA_WetSand");
        private static readonly int SeaGlint = Shader.PropertyToID("_KA_SeaGlint");
        private static readonly int SeaGlintDensity = Shader.PropertyToID("_KA_SeaGlintDensity");
        private static readonly int SeaRipple = Shader.PropertyToID("_KA_SeaRipple");

        private void Awake() => Apply();

        private void Update() => Apply();

        public void Apply()
        {
            if (config == null) return;
            Shader.SetGlobalFloat(ShoreSpeed, config.ShoreWaveSpeed);
            Shader.SetGlobalFloat(ShoreReach, config.ShoreWaveReach);
            Shader.SetGlobalFloat(ShoreFoam, config.ShoreFoamStrength);
            Shader.SetGlobalFloat(WetSand, config.WetSandStrength);
            Shader.SetGlobalFloat(SeaGlint, config.SeaGlintStrength);
            Shader.SetGlobalFloat(SeaGlintDensity, config.SeaGlintDensity);
            Shader.SetGlobalFloat(SeaRipple, config.SeaRippleStrength);
        }
    }
}
