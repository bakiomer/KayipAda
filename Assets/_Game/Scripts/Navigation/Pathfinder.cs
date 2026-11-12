// Pathfinder.cs
// İşçiler için basit yol bulucu. Hedefe düz çizgi engele çarpmıyorsa doğrudan gidilir; çarpıyorsa
// engellerin köşelerinden geçen en kısa yol bulunur (köşeler arası Dijkstra). Engeller az olduğu için hızlıdır.
// Başlangıç ya da hedef bir engelin içindeyse o engel yok sayılır (örn. kamptan çıkarken).

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda
{
    public static class Pathfinder
    {
        private const float CornerMargin = 0.05f;   // köşe noktaları engelin biraz dışında durur
        private const float EdgeTolerance = 0.01f;  // engelin kenarına sürtünerek geçmek çarpma sayılmaz

        private static readonly List<Rect> obstacles = new List<Rect>();
        private static readonly List<Vector2> points = new List<Vector2>();

        // from → to yolunu "path" listesine yazar (başlangıç hariç, hedef dahil).
        // clearance: engellerden ne kadar uzak durulacağı. bounds: yürünebilir alan.
        public static void FindPath(Vector2 from, Vector2 to, float clearance, Rect bounds, List<Vector2> path)
        {
            path.Clear();

            obstacles.Clear();
            foreach (NavObstacle obstacle in NavObstacle.Active)
            {
                Rect rect = obstacle.GetRect(clearance);
                if (rect.Contains(from) || rect.Contains(to)) continue;
                obstacles.Add(rect);
            }

            if (IsClear(from, to))
            {
                path.Add(to);
                return;
            }

            // Noktalar: 0 = başlangıç, 1 = hedef, sonra engel köşeleri.
            points.Clear();
            points.Add(from);
            points.Add(to);
            foreach (Rect rect in obstacles)
            {
                AddCorner(new Vector2(rect.xMin - CornerMargin, rect.yMin - CornerMargin), bounds);
                AddCorner(new Vector2(rect.xMax + CornerMargin, rect.yMin - CornerMargin), bounds);
                AddCorner(new Vector2(rect.xMin - CornerMargin, rect.yMax + CornerMargin), bounds);
                AddCorner(new Vector2(rect.xMax + CornerMargin, rect.yMax + CornerMargin), bounds);
            }

            // Dijkstra: başlangıçtan her noktaya en kısa mesafe.
            int count = points.Count;
            float[] distance = new float[count];
            int[] previous = new int[count];
            bool[] done = new bool[count];
            for (int i = 0; i < count; i++)
            {
                distance[i] = float.MaxValue;
                previous[i] = -1;
            }
            distance[0] = 0f;

            for (int step = 0; step < count; step++)
            {
                int current = -1;
                for (int i = 0; i < count; i++)
                {
                    if (!done[i] && distance[i] < float.MaxValue && (current < 0 || distance[i] < distance[current])) current = i;
                }
                if (current < 0 || current == 1) break; // ulaşılacak nokta kalmadı ya da hedefe varıldı
                done[current] = true;

                for (int next = 0; next < count; next++)
                {
                    if (done[next] || !IsClear(points[current], points[next])) continue;

                    float d = distance[current] + Vector2.Distance(points[current], points[next]);
                    if (d < distance[next])
                    {
                        distance[next] = d;
                        previous[next] = current;
                    }
                }
            }

            if (previous[1] < 0)
            {
                path.Add(to); // yol bulunamadı: düz git (takılıp kalmasın)
                return;
            }

            for (int i = 1; i != 0; i = previous[i]) path.Add(points[i]);
            path.Reverse();
        }

        private static void AddCorner(Vector2 corner, Rect bounds)
        {
            if (!bounds.Contains(corner)) return;
            foreach (Rect rect in obstacles)
            {
                if (rect.Contains(corner)) return; // başka bir engelin içinde
            }
            points.Add(corner);
        }

        // a → b doğrusu hiçbir engelin içinden geçmiyor mu?
        private static bool IsClear(Vector2 a, Vector2 b)
        {
            foreach (Rect rect in obstacles)
            {
                Rect inner = new Rect(rect.xMin + EdgeTolerance, rect.yMin + EdgeTolerance,
                                      rect.width - EdgeTolerance * 2f, rect.height - EdgeTolerance * 2f);
                if (SegmentHitsRect(a, b, inner)) return false;
            }
            return true;
        }

        // Doğru parçası dikdörtgene değiyor mu? (Liang-Barsky kırpma yöntemi)
        private static bool SegmentHitsRect(Vector2 a, Vector2 b, Rect rect)
        {
            Vector2 d = b - a;
            float tMin = 0f;
            float tMax = 1f;
            return Clip(-d.x, a.x - rect.xMin, ref tMin, ref tMax)
                && Clip(d.x, rect.xMax - a.x, ref tMin, ref tMax)
                && Clip(-d.y, a.y - rect.yMin, ref tMin, ref tMax)
                && Clip(d.y, rect.yMax - a.y, ref tMin, ref tMax);
        }

        private static bool Clip(float p, float q, ref float tMin, ref float tMax)
        {
            if (p == 0f) return q >= 0f; // kenara paralel: içeride mi?

            float t = q / p;
            if (p < 0f)
            {
                if (t > tMax) return false;
                if (t > tMin) tMin = t;
            }
            else
            {
                if (t < tMin) return false;
                if (t < tMax) tMax = t;
            }
            return true;
        }
    }
}
