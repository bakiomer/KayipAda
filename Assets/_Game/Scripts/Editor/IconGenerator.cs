// IconGenerator.cs
// Kaynak ikonlarını kodla çizer (Art/Generated/Icon_<id>.png): basit, renkli, kalın koyu kenarlı, okunaklı.
// Bina giriş/çıkış dairelerinde ve gemi aşama göstergesinde kullanılır. Her ikon birkaç basit şeklin
// (daire, yuvarlatılmış dikdörtgen, halka) üst üste çizilmesiyle oluşur; her şeklin kendi kenar çizgisi vardır.

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class IconGenerator
    {
        private const int Size = 128;
        private const float Outline = 5f;
        private static readonly Color Ink = new Color(0.22f, 0.11f, 0.05f, 1f);

        private abstract class Shape
        {
            public Color Fill;
            public abstract float Distance(Vector2 p); // < 0 içeride
        }

        private class Circle : Shape
        {
            public Vector2 C; public float R;
            public override float Distance(Vector2 p) => (p - C).magnitude - R;
        }

        private class Ring : Shape
        {
            public Vector2 C; public float R, Thickness;
            public override float Distance(Vector2 p) => Mathf.Abs((p - C).magnitude - R) - Thickness * 0.5f;
        }

        private class Box : Shape
        {
            public Vector2 C, Half; public float Radius, Angle;
            public override float Distance(Vector2 p)
            {
                Vector2 d = p - C;
                float a = -Angle * Mathf.Deg2Rad;
                d = new Vector2(d.x * Mathf.Cos(a) - d.y * Mathf.Sin(a), d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a));
                Vector2 q = new Vector2(Mathf.Abs(d.x), Mathf.Abs(d.y)) - Half + Vector2.one * Radius;
                return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - Radius;
            }
        }

        // Kaynak kimliğine göre ikon şekilleri (64 merkezli, 128x128 alan).
        private static List<Shape> ShapesFor(string id)
        {
            List<Shape> s = new List<Shape>();
            Color wood = new Color(0.62f, 0.38f, 0.18f), woodLight = new Color(0.86f, 0.66f, 0.40f);
            switch (id)
            {
                case "wood": // kütük: yan yatmış silindir + kesit halkaları
                    s.Add(new Box { C = new Vector2(56, 64), Half = new Vector2(40, 22), Radius = 18, Fill = wood });
                    s.Add(new Circle { C = new Vector2(92, 64), R = 24, Fill = woodLight });
                    s.Add(new Ring { C = new Vector2(92, 64), R = 12, Thickness = 4, Fill = wood });
                    break;
                case "stone": // gri taş yığını
                    s.Add(new Circle { C = new Vector2(48, 56), R = 28, Fill = new Color(0.55f, 0.56f, 0.6f) });
                    s.Add(new Circle { C = new Vector2(82, 52), R = 24, Fill = new Color(0.68f, 0.69f, 0.73f) });
                    s.Add(new Circle { C = new Vector2(66, 82), R = 26, Fill = new Color(0.78f, 0.79f, 0.82f) });
                    break;
                case "fruit": // kırmızı meyve + yeşil yaprak
                    s.Add(new Box { C = new Vector2(70, 104), Half = new Vector2(16, 8), Radius = 8, Angle = -25, Fill = new Color(0.35f, 0.7f, 0.25f) });
                    s.Add(new Circle { C = new Vector2(62, 62), R = 38, Fill = new Color(0.9f, 0.2f, 0.15f) });
                    s.Add(new Circle { C = new Vector2(50, 76), R = 9, Fill = new Color(1f, 0.6f, 0.55f) });
                    break;
                case "fiber": // sarı lif demeti
                    for (int i = -2; i <= 2; i++)
                        s.Add(new Box { C = new Vector2(64 + i * 11, 64), Half = new Vector2(7, 44), Radius = 7, Angle = i * 9, Fill = new Color(0.95f, 0.82f, 0.35f) });
                    s.Add(new Box { C = new Vector2(64, 64), Half = new Vector2(34, 8), Radius = 4, Fill = new Color(0.7f, 0.45f, 0.2f) });
                    break;
                case "plank": // iki kalas
                    s.Add(new Box { C = new Vector2(64, 50), Half = new Vector2(50, 14), Radius = 4, Angle = 8, Fill = woodLight });
                    s.Add(new Box { C = new Vector2(64, 82), Half = new Vector2(50, 14), Radius = 4, Angle = -6, Fill = new Color(0.8f, 0.58f, 0.32f) });
                    break;
                case "brick": // turuncu tuğlalar
                    Color brick = new Color(0.86f, 0.38f, 0.22f), brick2 = new Color(0.94f, 0.5f, 0.3f);
                    s.Add(new Box { C = new Vector2(42, 46), Half = new Vector2(26, 16), Radius = 3, Fill = brick });
                    s.Add(new Box { C = new Vector2(88, 46), Half = new Vector2(26, 16), Radius = 3, Fill = brick2 });
                    s.Add(new Box { C = new Vector2(64, 82), Half = new Vector2(30, 16), Radius = 3, Fill = brick2 });
                    break;
                case "rope": // kahverengi halat halkası
                    s.Add(new Ring { C = new Vector2(64, 64), R = 34, Thickness = 20, Fill = new Color(0.82f, 0.6f, 0.34f) });
                    s.Add(new Ring { C = new Vector2(64, 64), R = 34, Thickness = 4, Fill = new Color(0.62f, 0.42f, 0.2f) });
                    break;
                case "cloth": // krem bez topu (rulo)
                    s.Add(new Box { C = new Vector2(64, 64), Half = new Vector2(46, 30), Radius = 24, Fill = new Color(0.97f, 0.94f, 0.84f) });
                    s.Add(new Box { C = new Vector2(64, 64), Half = new Vector2(6, 30), Radius = 3, Fill = new Color(0.85f, 0.35f, 0.3f) });
                    break;
                case "scrap": // paslı metal levhalar + cıvata
                    s.Add(new Box { C = new Vector2(54, 58), Half = new Vector2(36, 18), Radius = 4, Angle = 18, Fill = new Color(0.62f, 0.4f, 0.28f) });
                    s.Add(new Box { C = new Vector2(74, 78), Half = new Vector2(34, 16), Radius = 4, Angle = -14, Fill = new Color(0.55f, 0.56f, 0.6f) });
                    s.Add(new Circle { C = new Vector2(46, 86), R = 10, Fill = new Color(0.4f, 0.4f, 0.44f) });
                    break;
                case "nail": // üç çivi
                    for (int i = -1; i <= 1; i++)
                    {
                        s.Add(new Box { C = new Vector2(64 + i * 24, 58), Half = new Vector2(5, 40), Radius = 3, Angle = i * 10, Fill = new Color(0.7f, 0.72f, 0.76f) });
                        s.Add(new Box { C = new Vector2(64 + i * 24 - i * 7, 100), Half = new Vector2(14, 6), Radius = 3, Angle = i * 10, Fill = new Color(0.5f, 0.52f, 0.56f) });
                    }
                    break;
                case "crystal": // mor kristal (elmas)
                    s.Add(new Box { C = new Vector2(64, 64), Half = new Vector2(34, 34), Radius = 6, Angle = 45, Fill = new Color(0.62f, 0.38f, 0.95f) });
                    s.Add(new Box { C = new Vector2(52, 76), Half = new Vector2(10, 22), Radius = 4, Angle = 45, Fill = new Color(0.86f, 0.75f, 1f) });
                    break;
                default:
                    s.Add(new Circle { C = new Vector2(64, 64), R = 40, Fill = Color.white });
                    break;
            }
            return s;
        }

        // İkonu üretir (yoksa ya da yeniden çizilmesi gerekiyorsa) ve sprite olarak döndürür.
        public static Sprite Create(string id)
        {
            string path = ArtGenerator.GeneratedFolder + "/Icon_" + id + ".png";
            List<Shape> shapes = ShapesFor(id);
            Texture2D tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Color c = new Color(0f, 0f, 0f, 0f);
                    foreach (Shape shape in shapes)
                    {
                        float d = shape.Distance(p);
                        // Önce kenar çizgisi, sonra dolgu (her şekil alttakinin üstüne).
                        float outline = Mathf.Clamp01(Outline + 0.5f - d);
                        float fill = Mathf.Clamp01(0.5f - d);
                        c = Over(c, new Color(Ink.r, Ink.g, Ink.b, outline));
                        c = Over(c, new Color(shape.Fill.r, shape.Fill.g, shape.Fill.b, fill));
                    }
                    pixels[y * Size + x] = c;
                }
            }
            tex.SetPixels(pixels);
            BuilderUtil.WritePng(tex, path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Color Over(Color under, Color over)
        {
            float a = over.a + under.a * (1f - over.a);
            if (a <= 0.0001f) return new Color(0f, 0f, 0f, 0f);
            Color c = (over * over.a + under * under.a * (1f - over.a)) / a;
            c.a = a;
            return c;
        }
    }
}
