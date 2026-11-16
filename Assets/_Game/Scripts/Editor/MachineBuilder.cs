// MachineBuilder.cs
// Halat Atölyesi ve Dokuma Tezgahı için kodla kurulan makineler (pakette uygun model yok). İkisi de ahşap zemin,
// dört direk ve çizgili bez tente altında; adı binanın üstündeki tabelada (PrefabFactory.BuildBuilding).
// - Halat çarkı: kollu büyük ahşap çark (döner), çarktan makaraya giden lif teli, dolan halat makarası.
// - Dokuma tezgahı: iki yan dikme, arka çözgü silindiri, gergin çözgü iplikleri, sağa-sola giden mekik,
//   inip kalkan tarak ve kumaşın sarıldığı ön silindir.
// Hareketi oyun içinde Machine bileşeni verir (sadece bina işlerken).

using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class MachineBuilder
    {
        public enum Kind { None, RopeWheel, Loom }

        private static Material Wood => ArtGenerator.GetLit("M_ShipWood", null, MapLayout.ShipWoodColor);
        private static Material Plank => ArtGenerator.GetLit("M_ShipPlank", null, MapLayout.ShipPlankColor);
        private static Material Rope => ArtGenerator.GetLit("M_MachineRope", null, new Color(0.78f, 0.6f, 0.36f));
        private static Material Fiber => ArtGenerator.GetLit("M_MachineFiber", null, MapLayout.ItemFiberColor);
        private static Material Cloth => ArtGenerator.GetLit("M_MachineCloth", null, MapLayout.ItemClothColor);
        private static Material Thread => ArtGenerator.GetLit("M_MachineThread", null, new Color(0.93f, 0.88f, 0.74f));
        private static Material Iron => ArtGenerator.GetLit("M_MachineIron", null, new Color(0.3f, 0.3f, 0.32f));

        // Makineyi "body" altına kurar ve hareket bileşenini ekler. Ürünün çıktığı yeri (body'nin yerel uzayında) döner.
        public static Vector3 Build(Kind kind, Transform body, ProcessingBuilding building, GameConfig config, float size)
        {
            Shelter(body, kind == Kind.Loom ? new Color(0.25f, 0.5f, 0.75f) : new Color(0.82f, 0.25f, 0.2f), size);
            Machine machine = body.gameObject.AddComponent<Machine>();
            BuilderUtil.SetField(machine, "config", config);
            BuilderUtil.SetField(machine, "building", building);
            BuilderUtil.SetEnum(machine, "kind", kind == Kind.Loom ? 1 : 0);
            Vector3 output = kind == Kind.Loom ? BuildLoom(body, machine) : BuildRopeWheel(body, machine);
            BuilderUtil.SetShadows(body.gameObject, true);
            return output;
        }

        // Ahşap zemin, dört direk ve önü alçak, çizgili bez tente.
        private static void Shelter(Transform body, Color stripe, float size)
        {
            float w = size * 0.42f, d = size * 0.33f, h = size * 0.74f;
            Part(body, PrimitiveType.Cube, "Floor", new Vector3(0f, 0.08f, 0f), new Vector3(w * 2f, 0.16f, d * 2f), Quaternion.identity, Plank);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Part(body, PrimitiveType.Cube, "Post", new Vector3(x * (w - 0.1f), h * 0.5f, z * (d - 0.1f)), new Vector3(0.16f, h, 0.16f), Quaternion.identity, Wood);

            Transform roof = new GameObject("Awning").transform;
            roof.SetParent(body, false);
            roof.localPosition = new Vector3(0f, h + 0.05f, 0f);
            roof.localRotation = Quaternion.Euler(-12f, 0f, 0f); // önü alçak
            Material a = ArtGenerator.GetLit("M_Awning_" + ColorUtility.ToHtmlStringRGB(stripe), null, stripe);
            Material b = ArtGenerator.GetLit("M_AwningCream", null, new Color(0.96f, 0.92f, 0.8f));
            int strips = 7;
            float stripW = (w * 2f + 0.5f) / strips;
            for (int i = 0; i < strips; i++)
            {
                float x = -w - 0.25f + stripW * (i + 0.5f);
                Part(roof, PrimitiveType.Cube, "Strip", new Vector3(x, 0f, 0f), new Vector3(stripW + 0.01f, 0.06f, d * 2f + 0.6f), Quaternion.identity, i % 2 == 0 ? a : b);
            }
            // Ön sarkan saçak (dalgalı bez kenarı gibi küçük üçgenler yerine kısa şerit).
            for (int i = 0; i < strips; i++)
            {
                float x = -w - 0.25f + stripW * (i + 0.5f);
                Part(roof, PrimitiveType.Cube, "Valance", new Vector3(x, -0.14f, -d - 0.3f), new Vector3(stripW + 0.01f, 0.28f, 0.04f), Quaternion.identity, i % 2 == 0 ? a : b);
            }
        }

        // Halat çarkı: solda büyük çark (kol takılı), sağda makara; aralarında lif teli. Lif demeti girişe yakın.
        private static Vector3 BuildRopeWheel(Transform body, Machine machine)
        {
            Vector3 hub = new Vector3(-0.45f, 1.25f, 0.05f);
            float radius = 0.95f;
            // Çarkın ayakları (iki yanda A şeklinde).
            for (int s = -1; s <= 1; s += 2)
            {
                float z = hub.z + s * 0.28f;
                Part(body, PrimitiveType.Cube, "Leg", new Vector3(hub.x - 0.35f, hub.y * 0.5f, z), new Vector3(0.12f, hub.y + 0.2f, 0.12f), Quaternion.Euler(0f, 0f, -16f), Wood);
                Part(body, PrimitiveType.Cube, "Leg", new Vector3(hub.x + 0.35f, hub.y * 0.5f, z), new Vector3(0.12f, hub.y + 0.2f, 0.12f), Quaternion.Euler(0f, 0f, 16f), Wood);
            }
            Part(body, PrimitiveType.Cylinder, "Axle", hub, new Vector3(0.12f, 0.36f, 0.12f), Quaternion.Euler(90f, 0f, 0f), Iron);

            Transform wheel = new GameObject("Wheel").transform;
            wheel.SetParent(body, false);
            wheel.localPosition = hub;
            int segments = 14;
            float seg = 2f * Mathf.PI * radius / segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i * 360f / segments;
                Vector3 p = Quaternion.Euler(0f, 0f, a) * new Vector3(0f, radius, 0f);
                Part(wheel, PrimitiveType.Cube, "Rim", p, new Vector3(seg * 1.08f, 0.14f, 0.16f), Quaternion.Euler(0f, 0f, a), Wood);
            }
            for (int i = 0; i < 4; i++)
                Part(wheel, PrimitiveType.Cube, "Spoke", Vector3.zero, new Vector3(0.09f, radius * 2f, 0.09f), Quaternion.Euler(0f, 0f, i * 45f), Plank);
            Part(wheel, PrimitiveType.Cylinder, "Hub", Vector3.zero, new Vector3(0.3f, 0.12f, 0.3f), Quaternion.Euler(90f, 0f, 0f), Wood);
            // Kol: göbekten dışarı bir kol ve tutamak (çarkla döner).
            Part(wheel, PrimitiveType.Cube, "CrankArm", new Vector3(0.22f, 0f, -0.22f), new Vector3(0.44f, 0.08f, 0.06f), Quaternion.identity, Iron);
            Part(wheel, PrimitiveType.Cylinder, "CrankHandle", new Vector3(0.42f, 0f, -0.36f), new Vector3(0.07f, 0.14f, 0.07f), Quaternion.Euler(90f, 0f, 0f), Wood);

            // Makara: dikey ayaklı tezgahta, Z ekseninde döner; üstündeki halat dolar (kalınlaşır).
            Vector3 spoolPos = new Vector3(0.95f, 0.95f, 0.05f);
            Part(body, PrimitiveType.Cube, "SpoolStand", new Vector3(spoolPos.x, 0.45f, spoolPos.z - 0.32f), new Vector3(0.12f, 0.9f, 0.1f), Quaternion.identity, Wood);
            Part(body, PrimitiveType.Cube, "SpoolStand", new Vector3(spoolPos.x, 0.45f, spoolPos.z + 0.32f), new Vector3(0.12f, 0.9f, 0.1f), Quaternion.identity, Wood);
            Transform spool = new GameObject("Spool").transform;
            spool.SetParent(body, false);
            spool.localPosition = spoolPos;
            Part(spool, PrimitiveType.Cylinder, "Rope", Vector3.zero, new Vector3(0.5f, 0.24f, 0.5f), Quaternion.Euler(90f, 0f, 0f), Rope);
            Part(spool, PrimitiveType.Cylinder, "FlangeA", new Vector3(0f, 0f, -0.26f), new Vector3(0.62f, 0.02f, 0.62f), Quaternion.Euler(90f, 0f, 0f), Wood);
            Part(spool, PrimitiveType.Cylinder, "FlangeB", new Vector3(0f, 0f, 0.26f), new Vector3(0.62f, 0.02f, 0.62f), Quaternion.Euler(90f, 0f, 0f), Wood);

            // Lif teli: çarkın kenarından makaraya.
            Vector3 from = hub + new Vector3(radius * 0.7f, -0.2f, 0f), to = spoolPos;
            Transform strand = new GameObject("Strand").transform;
            strand.SetParent(body, false);
            strand.localPosition = (from + to) * 0.5f;
            Vector3 dir = to - from;
            Part(strand, PrimitiveType.Cube, "Fiber", Vector3.zero, new Vector3(dir.magnitude, 0.05f, 0.05f),
                 Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg), Fiber);
            // Girişe yakın lif demeti (çarka akan lif).
            Part(body, PrimitiveType.Cylinder, "FiberBundle", new Vector3(-1.05f, 0.35f, -0.55f), new Vector3(0.45f, 0.18f, 0.45f), Quaternion.Euler(0f, 0f, 90f), Fiber);

            BuilderUtil.SetField(machine, "wheel", wheel);
            BuilderUtil.SetField(machine, "spool", spool);
            BuilderUtil.SetField(machine, "strand", strand);
            BuilderUtil.SetFloat(machine, "cycleTime", 1.1f);
            return spoolPos;
        }

        // El tezgahı: önü oyuncuya bakar; kumaş öndeki silindire sarılır.
        private static Vector3 BuildLoom(Transform body, Machine machine)
        {
            float half = 0.95f;
            // Yan dikmeler ve üst kiriş.
            for (int s = -1; s <= 1; s += 2)
            {
                Part(body, PrimitiveType.Cube, "Upright", new Vector3(s * half, 0.85f, 0.15f), new Vector3(0.14f, 1.7f, 0.14f), Quaternion.identity, Wood);
                Part(body, PrimitiveType.Cube, "SideRail", new Vector3(s * half, 0.6f, 0f), new Vector3(0.12f, 0.12f, 1.4f), Quaternion.identity, Wood);
                Part(body, PrimitiveType.Cube, "FootFront", new Vector3(s * half, 0.3f, -0.62f), new Vector3(0.12f, 0.6f, 0.12f), Quaternion.identity, Wood);
                Part(body, PrimitiveType.Cube, "FootBack", new Vector3(s * half, 0.45f, 0.65f), new Vector3(0.12f, 0.9f, 0.12f), Quaternion.identity, Wood);
            }
            Part(body, PrimitiveType.Cube, "TopBeam", new Vector3(0f, 1.7f, 0.15f), new Vector3(half * 2f + 0.2f, 0.14f, 0.16f), Quaternion.identity, Wood);
            // Arka çözgü silindiri (iplik sarılı).
            Part(body, PrimitiveType.Cylinder, "WarpBeam", new Vector3(0f, 0.92f, 0.65f), new Vector3(0.22f, half, 0.22f), Quaternion.Euler(0f, 0f, 90f), Thread);
            // Gergin çözgü iplikleri: arkadan öne ince şeritler.
            for (int i = 0; i < 9; i++)
            {
                float x = -0.7f + i * 0.175f;
                Part(body, PrimitiveType.Cube, "Warp", new Vector3(x, 0.86f, 0.1f), new Vector3(0.025f, 0.015f, 1.1f), Quaternion.Euler(-4f, 0f, 0f), Thread);
            }
            // Tarak (inip kalkan çerçeve).
            Transform beater = new GameObject("Beater").transform;
            beater.SetParent(body, false);
            beater.localPosition = new Vector3(0f, 1.15f, 0.05f);
            Part(beater, PrimitiveType.Cube, "Top", new Vector3(0f, 0.28f, 0f), new Vector3(half * 2f - 0.15f, 0.08f, 0.08f), Quaternion.identity, Plank);
            Part(beater, PrimitiveType.Cube, "Bottom", new Vector3(0f, -0.22f, 0f), new Vector3(half * 2f - 0.15f, 0.08f, 0.08f), Quaternion.identity, Plank);
            for (int i = 0; i < 7; i++)
                Part(beater, PrimitiveType.Cube, "Reed", new Vector3(-0.7f + i * 0.233f, 0.03f, 0f), new Vector3(0.02f, 0.5f, 0.02f), Quaternion.identity, Wood);
            // Mekik (küçük tekne şekli).
            Transform shuttle = new GameObject("Shuttle").transform;
            shuttle.SetParent(body, false);
            shuttle.localPosition = new Vector3(0f, 0.9f, -0.12f);
            Part(shuttle, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0.36f, 0.07f, 0.11f), Quaternion.identity, Wood);
            Part(shuttle, PrimitiveType.Cube, "Bobbin", new Vector3(0f, 0.045f, 0f), new Vector3(0.18f, 0.04f, 0.06f), Quaternion.identity, Cloth);
            // Ön silindir ve sarılan kumaş (işlem ilerledikçe uzar).
            Vector3 rollerPos = new Vector3(0f, 0.75f, -0.58f);
            Transform roller = new GameObject("Roller").transform;
            roller.SetParent(body, false);
            roller.localPosition = rollerPos;
            Part(roller, PrimitiveType.Cylinder, "Cylinder", Vector3.zero, new Vector3(0.2f, half, 0.2f), Quaternion.Euler(0f, 0f, 90f), Wood);
            Part(roller, PrimitiveType.Cylinder, "ClothRoll", Vector3.zero, new Vector3(0.28f, half * 0.8f, 0.28f), Quaternion.Euler(0f, 0f, 90f), Cloth);
            Transform cloth = new GameObject("Cloth").transform;
            cloth.SetParent(body, false);
            cloth.localPosition = rollerPos + new Vector3(0f, 0.12f, 0.05f);
            Part(cloth, PrimitiveType.Cube, "Sheet", new Vector3(0f, 0f, 0.3f), new Vector3(half * 1.55f, 0.02f, 0.6f), Quaternion.Euler(-3f, 0f, 0f), Cloth);
            // Dokumacının taburesi (arka tarafta, oyuncuyu kapatmasın).
            Part(body, PrimitiveType.Cube, "Stool", new Vector3(1.25f, 0.25f, 0.75f), new Vector3(0.4f, 0.5f, 0.4f), Quaternion.identity, Plank);

            BuilderUtil.SetField(machine, "shuttle", shuttle);
            BuilderUtil.SetFloat(machine, "shuttleTravel", 0.72f);
            BuilderUtil.SetField(machine, "beater", beater);
            BuilderUtil.SetField(machine, "roller", roller);
            BuilderUtil.SetField(machine, "cloth", cloth);
            BuilderUtil.SetFloat(machine, "cycleTime", 0.75f);
            return rollerPos + Vector3.up * 0.2f;
        }

        public static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Quaternion rot, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }
    }
}
