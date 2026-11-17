// Faded.cs
// Bir nesneyi soluk gösterir (daha önce bulunmuş sandık ve koleksiyon eşyaları): her malzemenin rengi verilen tonla
// çarpılır. Paylaşımlı malzemeye dokunmaz (MaterialPropertyBlock), geri almak için Clear.

using UnityEngine;

namespace KayipAda
{
    public static class Faded
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static MaterialPropertyBlock block;

        public static void Apply(GameObject target, Color tint)
        {
            if (target == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].HasProperty(BaseColor)) continue;
                    Color c = materials[i].GetColor(BaseColor) * tint;
                    r.GetPropertyBlock(block, i);
                    block.SetColor(BaseColor, c);
                    r.SetPropertyBlock(block, i);
                }
            }
        }

        public static void Clear(GameObject target)
        {
            if (target == null) return;
            foreach (Renderer r in target.GetComponentsInChildren<Renderer>(true))
                for (int i = 0; i < r.sharedMaterials.Length; i++) r.SetPropertyBlock(null, i);
        }
    }
}
