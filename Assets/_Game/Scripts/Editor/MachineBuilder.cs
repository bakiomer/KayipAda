// MachineBuilder.cs
// İşleme binalarının makineleri kodla, iri ve sade low-poly parçalardan kurulur (uzaktan okunur, ince detay yok).
// Her binanın kendine özgü silueti var (çizgili tente sadece Pazar'da):
// - Taş Atölyesi (Crane): taş döşeli kenarlı ocak çukuru, ahşap direkli vinç (tambur + halat + kanca), yontma tezgahı.
// - Halat Atölyesi (RopeWheel): oluktan akan suyla dönen büyük su çarkı, iki makara, aralarında burulan 3 kalın iplik,
//   iri halat bobini.
// - Dokuma Tezgahı (Loom): hasır çatılı açık çardak altında tıknaz tezgah (çerçeve, mekik, kırmızı-beyaz çizgili rulo),
//   yanında renkli iplik makaraları.
// - Demirci (Anvil): kiremit çatılı açık tezgah, tuğla ocak (kor), körük, iri örs, eksantrikli çekiç, su fıçısı.
// - Bıçkıhane (Saw): model + üretim hattı.
// Hepsinde üretim hattı: girişten makineye kızak (makaralı), makineden çıkışa kızak; malzeme/ürün kopyası kayar.
// Hareketi oyunda Machine verir (üretime senkron).

using System.Collections.Generic;
using UnityEngine;

namespace KayipAda.EditorTools
{
    public static class MachineBuilder
    {
        public enum Kind { None, RopeWheel, Loom, Anvil, Crane, Saw }

        private static Material Wood => ArtGenerator.GetLit("M_ShipWood", null, MapLayout.ShipWoodColor);
        private static Material Plank => ArtGenerator.GetLit("M_ShipPlank", null, MapLayout.ShipPlankColor);
        private static Material DarkWood => ArtGenerator.GetLit("M_DarkWood", null, new Color(0.38f, 0.24f, 0.13f));
        private static Material Rope => ArtGenerator.GetLit("M_MachineRope", null, new Color(0.78f, 0.6f, 0.36f));
        private static Material Fiber => ArtGenerator.GetLit("M_MachineFiber", null, MapLayout.ItemFiberColor);
        private static Material Iron => ArtGenerator.GetLit("M_MachineIron", null, new Color(0.3f, 0.3f, 0.32f));
        private static Material StoneLight => ArtGenerator.GetLit("M_QuarryStone", null, new Color(0.8f, 0.77f, 0.7f));
        private static Material StoneMid => ArtGenerator.GetLit("M_QuarryStoneMid", null, new Color(0.66f, 0.63f, 0.57f));
        private static Material Hole => ArtGenerator.GetLit("M_QuarryHole", null, new Color(0.2f, 0.17f, 0.14f));
        private static Material Brick => ArtGenerator.GetLit("M_ForgeBrick", null, new Color(0.74f, 0.34f, 0.2f));
        private static Material Tile => ArtGenerator.GetLit("M_RoofTile", null, new Color(0.56f, 0.2f, 0.2f));
        private static Material Thatch => ArtGenerator.GetLit("M_Thatch", null, new Color(0.84f, 0.7f, 0.4f));
        private static Material Water => ArtGenerator.GetLit("M_StreamWater", null, new Color(0.35f, 0.68f, 0.88f));
        private static Material Ember => ArtGenerator.GetLit("M_Ember", null, new Color(1f, 0.5f, 0.15f));
        private static Material Leather => ArtGenerator.GetLit("M_Leather", null, new Color(0.45f, 0.27f, 0.15f));

        // Gövdenin yerel uzayında giriş ve çıkış kızaklarının uçları (binanın giriş/çıkış alanlarının üstü).
        private static readonly Vector3 InSlot = new Vector3(-1.4f, 0.32f, -2.15f);
        private static readonly Vector3 OutSlot = new Vector3(1.4f, 0.32f, -2.15f);

        public class Result
        {
            public Vector3 ProductPos;   // ürünün belirdiği yer (çıkış kızağının ucu, gövdenin yerel uzayında)
            public Transform Intake;     // ham maddenin uçtuğu yer
        }

        public static Result Build(Kind kind, Transform body, ProcessingBuilding building, GameConfig config,
                                   ResourceType input, ResourceType output)
        {
            Machine machine = body.gameObject.AddComponent<Machine>();
            BuilderUtil.SetField(machine, "config", config);
            BuilderUtil.SetField(machine, "building", building);
            int kindIndex = kind == Kind.Loom ? 1 : kind == Kind.Anvil ? 2 : kind == Kind.Crane ? 3 : kind == Kind.Saw ? 4 : 0;
            BuilderUtil.SetEnum(machine, "kind", kindIndex);
            Vector3 intake, outFrom;
            switch (kind)
            {
                case Kind.Loom: BuildLoom(body, machine, out intake, out outFrom); break;
                case Kind.Anvil: BuildForge(body, machine, out intake, out outFrom); break;
                case Kind.Crane: BuildQuarry(body, machine, out intake, out outFrom); break;
                case Kind.Saw: intake = new Vector3(-0.9f, 0.5f, -1.15f); outFrom = new Vector3(0.9f, 0.5f, -1.15f); break;
                default: BuildRope(body, machine, out intake, out outFrom); break;
            }
            Result r = BuildLine(body, machine, input, output, intake, outFrom);
            BuilderUtil.SetShadows(body.gameObject, true);
            return r;
        }

        // ================================================================ Üretim hattı

        private static Result BuildLine(Transform body, Machine machine, ResourceType input, ResourceType output, Vector3 intake, Vector3 outFrom)
        {
            List<Transform> rollers = new List<Transform>();
            Chute(body, "FeedChute", InSlot, intake, rollers);
            Chute(body, "OutChute", outFrom, OutSlot, null);
            Transform feedFrom = Point(body, "FeedFrom", InSlot + Vector3.up * 0.18f);
            Transform feedTo = Point(body, "FeedTo", intake + Vector3.up * 0.18f);
            Transform outFromT = Point(body, "OutFrom", outFrom + Vector3.up * 0.18f);
            Transform outTo = Point(body, "OutTo", OutSlot + Vector3.up * 0.18f);
            BuilderUtil.SetField(machine, "feedProxy", Proxy(body, "FeedItem", input));
            BuilderUtil.SetField(machine, "feedFrom", feedFrom);
            BuilderUtil.SetField(machine, "feedTo", feedTo);
            BuilderUtil.SetField(machine, "outProxy", Proxy(body, "OutItem", output));
            BuilderUtil.SetField(machine, "outFrom", outFromT);
            BuilderUtil.SetField(machine, "outTo", outTo);
            BuilderUtil.SetArray(machine, "rollers", rollers.ToArray());
            return new Result { ProductPos = outTo.localPosition, Intake = feedTo };
        }

        private static Transform Point(Transform parent, string name, Vector3 localPos)
        {
            Transform t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            return t;
        }

        // Kaynağın görünüşünün bir kopyası (bileşensiz), başta gizli.
        private static Transform Proxy(Transform parent, string name, ResourceType type)
        {
            GameObject go;
            if (type != null && type.ItemPrefab != null)
            {
                go = Object.Instantiate(type.ItemPrefab.gameObject);
                foreach (Component c in go.GetComponentsInChildren<Component>(true))
                    if (!(c is Transform) && !(c is Renderer) && !(c is MeshFilter)) Object.DestroyImmediate(c);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(go.GetComponent<Collider>());
                go.transform.localScale = Vector3.one * 0.3f;
            }
            go.name = name;
            go.transform.SetParent(parent, false);
            go.SetActive(false);
            BuilderUtil.SetShadows(go, false);
            return go.transform;
        }

        // Eğimli ahşap kızak: tahta taban, iki yan çıta, ayaklar; istenirse üstünde dönen makaralar.
        private static void Chute(Transform parent, string name, Vector3 from, Vector3 to, List<Transform> rollers)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(parent, false);
            Vector3 d = to - from;
            float length = d.magnitude;
            if (length < 0.2f) return;
            Quaternion rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            Vector3 mid = (from + to) * 0.5f;
            Part(root, PrimitiveType.Cube, "Bed", mid, new Vector3(0.62f, 0.06f, length), rot, Plank);
            for (int s = -1; s <= 1; s += 2)
                Part(root, PrimitiveType.Cube, "Rail", mid + rot * new Vector3(s * 0.33f, 0.08f, 0f), new Vector3(0.07f, 0.14f, length), rot, DarkWood);
            int legs = Mathf.Max(2, Mathf.CeilToInt(length / 0.9f));
            for (int i = 0; i < legs; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, (i + 0.5f) / legs);
                if (p.y < 0.12f) continue;
                Part(root, PrimitiveType.Cube, "Leg", new Vector3(p.x, p.y * 0.5f, p.z), new Vector3(0.08f, p.y, 0.08f), Quaternion.identity, DarkWood);
            }
            if (rollers == null) return;
            int count = Mathf.Max(2, Mathf.RoundToInt(length / 0.32f));
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, (i + 0.5f) / count) + Vector3.up * 0.06f;
                Transform r = new GameObject("Roller").transform;
                r.SetParent(root, false);
                r.localPosition = p;
                r.localRotation = rot;
                Part(r, PrimitiveType.Cylinder, "Drum", Vector3.zero, new Vector3(0.09f, 0.29f, 0.09f), Quaternion.Euler(0f, 0f, 90f), Wood);
                Part(r, PrimitiveType.Cube, "Mark", new Vector3(0f, 0.045f, 0f), new Vector3(0.56f, 0.012f, 0.03f), Quaternion.identity, DarkWood);
                rollers.Add(r);
            }
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

        private static ParticleSystem Particles(Transform parent, string name, Vector3 pos, Color a, Color b, float speedMin, float speedMax,
                                                float size, float gravity, float lifetime, int max, float rate)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity;
            main.maxParticles = max;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.12f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
            pr.sharedMaterial = ArtGenerator.GetTransparent("M_Spark", ArtGenerator.LoadSprite(ArtGenerator.SoftCirclePath)?.texture, Color.white);
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return ps;
        }

        // ================================================================ Taş Atölyesi: ocak çukuru + vinç + yontma tezgahı

        private static void BuildQuarry(Transform body, Machine machine, out Vector3 intake, out Vector3 outFrom)
        {
            Vector3 pit = new Vector3(-0.85f, 0f, 0.2f);
            System.Random r = new System.Random(5);
            for (int x = -2; x <= 2; x++)
                for (int z = -1; z <= 2; z++)
                {
                    Vector3 p = new Vector3(x * 0.78f, 0.03f, z * 0.66f - 0.15f);
                    if (Mathf.Abs(p.x - pit.x) < 0.75f && Mathf.Abs(p.z - pit.z) < 0.75f) continue;
                    Part(body, PrimitiveType.Cube, "Paving", p, new Vector3(0.72f, 0.06f + (float)r.NextDouble() * 0.03f, 0.6f),
                         Quaternion.Euler(0f, (float)r.NextDouble() * 6f - 3f, 0f), (x + z) % 2 == 0 ? StoneLight : StoneMid);
                }
            Part(body, PrimitiveType.Cube, "Hole", pit + Vector3.up * 0.015f, new Vector3(1.3f, 0.02f, 1.3f), Quaternion.identity, Hole);
            for (int s = 0; s < 4; s++)
            {
                Quaternion q = Quaternion.Euler(0f, s * 90f, 0f);
                Part(body, PrimitiveType.Cube, "Rim", pit + q * new Vector3(0f, 0.12f, 0.72f), new Vector3(1.6f, 0.24f, 0.24f), q, StoneLight);
            }
            // Vinç direği (sağ arkada), tambur ve dönen kol.
            Vector3 post = new Vector3(0.35f, 0f, 0.95f);
            Part(body, PrimitiveType.Cube, "CraneBase", post + Vector3.up * 0.15f, new Vector3(0.9f, 0.3f, 0.9f), Quaternion.identity, StoneMid);
            Part(body, PrimitiveType.Cube, "Mast", post + Vector3.up * 1.7f, new Vector3(0.32f, 3.1f, 0.32f), Quaternion.identity, Wood);
            for (int s = -1; s <= 1; s += 2)
                Part(body, PrimitiveType.Cube, "Brace", post + new Vector3(s * 0.45f, 0.75f, 0f), new Vector3(0.14f, 1.6f, 0.14f), Quaternion.Euler(0f, 0f, s * 22f), Wood);
            Transform drum = new GameObject("Drum").transform;
            drum.SetParent(body, false);
            drum.localPosition = post + new Vector3(0f, 0.75f, -0.38f);
            Part(drum, PrimitiveType.Cylinder, "Core", Vector3.zero, new Vector3(0.42f, 0.32f, 0.42f), Quaternion.Euler(0f, 0f, 90f), Rope);
            for (int s = -1; s <= 1; s += 2)
                Part(drum, PrimitiveType.Cylinder, "Flange", new Vector3(s * 0.33f, 0f, 0f), new Vector3(0.62f, 0.03f, 0.62f), Quaternion.Euler(0f, 0f, 90f), DarkWood);
            Part(drum, PrimitiveType.Cube, "Crank", new Vector3(0.42f, 0.2f, 0f), new Vector3(0.06f, 0.42f, 0.06f), Quaternion.identity, Iron);

            Vector3 toPit = new Vector3(pit.x - post.x, 0f, pit.z - post.z);
            float reach = toPit.magnitude;
            Transform boom = new GameObject("Boom").transform;
            boom.SetParent(body, false);
            boom.localPosition = post + Vector3.up * 3.15f;
            boom.localRotation = Quaternion.LookRotation(toPit.normalized);
            Part(boom, PrimitiveType.Cube, "Arm", new Vector3(0f, 0.12f, reach * 0.45f), new Vector3(0.26f, 0.26f, reach + 1.1f), Quaternion.Euler(-8f, 0f, 0f), Wood);
            Part(boom, PrimitiveType.Cube, "Counter", new Vector3(0f, 0f, -0.75f), new Vector3(0.5f, 0.45f, 0.45f), Quaternion.identity, StoneMid);
            Part(boom, PrimitiveType.Cylinder, "Pulley", new Vector3(0f, -0.05f, reach), new Vector3(0.36f, 0.08f, 0.36f), Quaternion.Euler(0f, 0f, 90f), DarkWood);
            Transform hook = new GameObject("Hook").transform;
            hook.SetParent(boom, false);
            hook.localPosition = new Vector3(0f, -1.6f, reach);
            Part(hook, PrimitiveType.Cylinder, "Rope", new Vector3(0f, 0.8f, 0f), new Vector3(0.07f, 0.8f, 0.07f), Quaternion.identity, Rope);
            Part(hook, PrimitiveType.Cube, "HookBlock", Vector3.zero, new Vector3(0.22f, 0.22f, 0.22f), Quaternion.identity, Iron);
            // Taş blok: çukurun dibinden kalkar (zeminin altındayken görünmez), tezgaha iner, yontulur.
            Transform block = new GameObject("Block").transform;
            block.SetParent(body, false);
            Part(block, PrimitiveType.Cube, "Stone", Vector3.zero, new Vector3(0.62f, 0.5f, 0.62f), Quaternion.Euler(0f, 12f, 0f), StoneLight);
            for (int s = -1; s <= 1; s += 2)
                Part(block, PrimitiveType.Cube, "Strap", new Vector3(s * 0.16f, 0f, 0f), new Vector3(0.05f, 0.52f, 0.64f), Quaternion.Euler(0f, 12f, 0f), Rope);
            block.gameObject.SetActive(false);
            Transform blockPit = Point(body, "BlockPit", pit + Vector3.down * 0.4f);
            // Tezgah: kolun döndüğü noktada (vincin direği etrafında aynı yarıçap).
            float swing = -100f;
            Vector3 bench = post + Quaternion.Euler(0f, swing, 0f) * toPit;
            bench.y = 0f;
            Part(body, PrimitiveType.Cube, "Bench", bench + Vector3.up * 0.32f, new Vector3(1.0f, 0.12f, 0.85f), Quaternion.identity, Plank);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Part(body, PrimitiveType.Cube, "BenchLeg", bench + new Vector3(x * 0.4f, 0.14f, z * 0.32f), new Vector3(0.1f, 0.28f, 0.1f), Quaternion.identity, DarkWood);
            Transform blockBench = Point(body, "BlockBench", bench + Vector3.up * 0.62f);
            Part(body, PrimitiveType.Cube, "ChiselPost", bench + new Vector3(0.55f, 0.85f, 0.3f), new Vector3(0.12f, 1.7f, 0.12f), Quaternion.identity, Wood);
            Part(body, PrimitiveType.Cube, "ChiselBeam", bench + new Vector3(0.27f, 1.65f, 0.3f), new Vector3(0.66f, 0.1f, 0.1f), Quaternion.identity, Wood);
            Transform chisel = new GameObject("Chisel").transform;
            chisel.SetParent(body, false);
            chisel.localPosition = bench + new Vector3(0f, 1.0f, 0.05f);
            Part(chisel, PrimitiveType.Cube, "Shaft", new Vector3(0f, 0.25f, 0f), new Vector3(0.07f, 0.5f, 0.07f), Quaternion.identity, Iron);
            Part(chisel, PrimitiveType.Cube, "Head", Vector3.zero, new Vector3(0.28f, 0.2f, 0.2f), Quaternion.identity, DarkWood);
            ParticleSystem dust = Particles(body, "StoneDust", bench + Vector3.up * 0.7f, new Color(0.9f, 0.86f, 0.78f), new Color(0.75f, 0.72f, 0.66f), 0.6f, 1.6f, 0.14f, 0.6f, 0.7f, 30, 0f);
            Part(body, PrimitiveType.Cube, "Rubble", new Vector3(-1.75f, 0.12f, 1.0f), new Vector3(0.35f, 0.24f, 0.3f), Quaternion.Euler(0f, 30f, 0f), StoneMid);
            Part(body, PrimitiveType.Cube, "Rubble", new Vector3(-1.55f, 0.1f, 1.3f), new Vector3(0.28f, 0.2f, 0.26f), Quaternion.Euler(0f, 70f, 0f), StoneLight);

            BuilderUtil.SetField(machine, "boom", boom);
            BuilderUtil.SetFloat(machine, "boomSwing", swing);
            BuilderUtil.SetField(machine, "drum", drum);
            BuilderUtil.SetField(machine, "hook", hook);
            BuilderUtil.SetField(machine, "block", block);
            BuilderUtil.SetField(machine, "blockPit", blockPit);
            BuilderUtil.SetField(machine, "blockBench", blockBench);
            BuilderUtil.SetField(machine, "chisel", chisel);
            BuilderUtil.SetField(machine, "dust", dust);
            BuilderUtil.SetInt(machine, "beatsPerProduct", 3);
            intake = pit + new Vector3(-0.1f, 0.3f, -0.75f);
            outFrom = bench + new Vector3(0.1f, 0.4f, -0.45f);
        }

        // ================================================================ Halat Atölyesi: su çarkı + makaralar + bobin

        private static void BuildRope(Transform body, Machine machine, out Vector3 intake, out Vector3 outFrom)
        {
            Part(body, PrimitiveType.Cube, "Deck", new Vector3(0.1f, 0.08f, 0.1f), new Vector3(3.6f, 0.16f, 2.3f), Quaternion.identity, Plank);
            Part(body, PrimitiveType.Cube, "Stream", new Vector3(-1.15f, 0.02f, 1.35f), new Vector3(1.0f, 0.04f, 1.4f), Quaternion.identity, Water);
            Vector3 hub = new Vector3(-1.05f, 1.25f, 0.55f);
            float radius = 1.05f;
            Vector3 troughEnd = hub + new Vector3(0.1f, radius + 0.35f, 0f);
            Vector3 troughStart = troughEnd + new Vector3(-0.2f, 0.25f, 1.6f);
            Quaternion trot = Quaternion.LookRotation(troughEnd - troughStart);
            float tlen = Vector3.Distance(troughStart, troughEnd);
            Vector3 tmid = (troughStart + troughEnd) * 0.5f;
            Part(body, PrimitiveType.Cube, "TroughBed", tmid, new Vector3(0.42f, 0.06f, tlen), trot, Wood);
            for (int s = -1; s <= 1; s += 2)
                Part(body, PrimitiveType.Cube, "TroughSide", tmid + trot * new Vector3(s * 0.21f, 0.1f, 0f), new Vector3(0.06f, 0.22f, tlen), trot, Wood);
            Part(body, PrimitiveType.Cube, "TroughWater", tmid + Vector3.up * 0.05f, new Vector3(0.3f, 0.03f, tlen), trot, Water);
            Part(body, PrimitiveType.Cube, "TroughLeg", new Vector3(troughStart.x, troughStart.y * 0.5f, troughStart.z), new Vector3(0.16f, troughStart.y, 0.16f), Quaternion.identity, Wood);
            Part(body, PrimitiveType.Cube, "Fall", troughEnd + new Vector3(0.05f, -0.25f, -0.05f), new Vector3(0.22f, 0.5f, 0.1f), Quaternion.identity, Water);
            for (int s = -1; s <= 1; s += 2)
            {
                float z = hub.z + s * 0.42f;
                Part(body, PrimitiveType.Cube, "Leg", new Vector3(hub.x - 0.4f, hub.y * 0.5f, z), new Vector3(0.18f, hub.y + 0.3f, 0.18f), Quaternion.Euler(0f, 0f, -17f), Wood);
                Part(body, PrimitiveType.Cube, "Leg", new Vector3(hub.x + 0.4f, hub.y * 0.5f, z), new Vector3(0.18f, hub.y + 0.3f, 0.18f), Quaternion.Euler(0f, 0f, 17f), Wood);
            }
            Part(body, PrimitiveType.Cylinder, "Axle", hub, new Vector3(0.16f, 0.5f, 0.16f), Quaternion.Euler(90f, 0f, 0f), Iron);
            Transform wheel = new GameObject("WaterWheel").transform;
            wheel.SetParent(body, false);
            wheel.localPosition = hub;
            int paddles = 10;
            for (int i = 0; i < paddles; i++)
            {
                float a = i * 360f / paddles;
                Quaternion q = Quaternion.Euler(0f, 0f, a);
                Part(wheel, PrimitiveType.Cube, "Rim", q * new Vector3(0f, radius, 0f), new Vector3(0.7f, 0.12f, 0.5f), q, DarkWood);
                Part(wheel, PrimitiveType.Cube, "Paddle", q * new Vector3(0f, radius + 0.12f, 0f), new Vector3(0.12f, 0.36f, 0.5f), q, Wood);
            }
            for (int i = 0; i < 3; i++)
                Part(wheel, PrimitiveType.Cube, "Spoke", Vector3.zero, new Vector3(0.12f, radius * 2f, 0.12f), Quaternion.Euler(0f, 0f, i * 60f), Plank);
            Part(wheel, PrimitiveType.Cylinder, "Hub", Vector3.zero, new Vector3(0.4f, 0.16f, 0.4f), Quaternion.Euler(90f, 0f, 0f), Wood);
            ParticleSystem splash = Particles(body, "Splash", hub + new Vector3(0.25f, -radius + 0.1f, 0.2f), new Color(0.8f, 0.95f, 1f), new Color(0.55f, 0.82f, 1f), 0.8f, 1.6f, 0.12f, 1f, 0.5f, 24, 22f);

            Vector3 spoolA = new Vector3(-0.05f, 1.0f, -0.15f), spoolB = new Vector3(0.85f, 1.0f, -0.15f);
            List<Transform> spools = new List<Transform>();
            foreach (Vector3 sp in new[] { spoolA, spoolB })
            {
                for (int s = -1; s <= 1; s += 2)
                    Part(body, PrimitiveType.Cube, "SpoolStand", new Vector3(sp.x, sp.y * 0.5f, sp.z + s * 0.36f), new Vector3(0.14f, sp.y, 0.14f), Quaternion.identity, Wood);
                Transform spool = new GameObject("Spool").transform;
                spool.SetParent(body, false);
                spool.localPosition = sp;
                Part(spool, PrimitiveType.Cylinder, "Core", Vector3.zero, new Vector3(0.42f, 0.28f, 0.42f), Quaternion.Euler(90f, 0f, 0f), Fiber);
                Part(spool, PrimitiveType.Cylinder, "Flange", new Vector3(0f, 0f, -0.3f), new Vector3(0.62f, 0.03f, 0.62f), Quaternion.Euler(90f, 0f, 0f), DarkWood);
                Part(spool, PrimitiveType.Cylinder, "Flange", new Vector3(0f, 0f, 0.3f), new Vector3(0.62f, 0.03f, 0.62f), Quaternion.Euler(90f, 0f, 0f), DarkWood);
                Part(spool, PrimitiveType.Cube, "Peg", new Vector3(0f, 0.2f, -0.34f), new Vector3(0.06f, 0.16f, 0.06f), Quaternion.identity, Iron);
                spools.Add(spool);
            }
            Material twist = ArtGenerator.GetLit("M_TwistStrand", ArtGenerator.StripeTexture("Tex_Twist", new Color(1f, 0.88f, 0.5f), new Color(0.8f, 0.6f, 0.3f), 4, true), Color.white);
            List<Renderer> strands = new List<Renderer>();
            float len = spoolB.x - spoolA.x - 0.2f;
            for (int i = 0; i < 3; i++)
            {
                float z = spoolA.z + (i - 1) * 0.1f;
                GameObject strand = Part(body, PrimitiveType.Cylinder, "Strand", new Vector3((spoolA.x + spoolB.x) * 0.5f, spoolA.y + 0.05f + (i % 2) * 0.03f, z),
                                         new Vector3(0.09f, len * 0.5f, 0.09f), Quaternion.Euler(0f, 0f, 90f), twist);
                strands.Add(strand.GetComponent<Renderer>());
            }
            Vector3 bobinPos = new Vector3(1.55f, 0.78f, 0.45f);
            for (int s = -1; s <= 1; s += 2)
                Part(body, PrimitiveType.Cube, "BobinStand", new Vector3(bobinPos.x, bobinPos.y * 0.5f, bobinPos.z + s * 0.42f), new Vector3(0.16f, bobinPos.y + 0.1f, 0.16f), Quaternion.identity, Wood);
            Transform bobin = new GameObject("Bobin").transform;
            bobin.SetParent(body, false);
            bobin.localPosition = bobinPos;
            Part(bobin, PrimitiveType.Cylinder, "Coil", Vector3.zero, new Vector3(0.7f, 0.32f, 0.7f), Quaternion.Euler(90f, 0f, 0f), Rope);
            Part(bobin, PrimitiveType.Cube, "Stripe", new Vector3(0.36f, 0f, 0f), new Vector3(0.04f, 0.1f, 0.62f), Quaternion.identity, DarkWood);
            Part(body, PrimitiveType.Cylinder, "RopeOut", (spoolB + bobinPos) * 0.5f, new Vector3(0.12f, Vector3.Distance(spoolB, bobinPos) * 0.5f, 0.12f),
                 Quaternion.FromToRotation(Vector3.up, bobinPos - spoolB), Rope);
            Part(body, PrimitiveType.Cylinder, "Belt", (hub + spoolA) * 0.5f + Vector3.forward * 0.3f, new Vector3(0.05f, Vector3.Distance(hub, spoolA) * 0.5f, 0.05f),
                 Quaternion.FromToRotation(Vector3.up, spoolA - hub), Rope);

            BuilderUtil.SetField(machine, "waterWheel", wheel);
            BuilderUtil.SetArray(machine, "spools", spools.ToArray());
            BuilderUtil.SetArray(machine, "strands", strands.ToArray());
            BuilderUtil.SetField(machine, "bobin", bobin);
            BuilderUtil.SetField(machine, "water", splash);
            BuilderUtil.SetInt(machine, "beatsPerProduct", 2);
            intake = spoolA + new Vector3(-0.25f, -0.55f, -0.6f);
            outFrom = bobinPos + new Vector3(0f, -0.35f, -0.65f);
        }

        // ================================================================ Dokuma Tezgahı: hasır çatılı çardak + tıknaz tezgah

        private static void BuildLoom(Transform body, Machine machine, out Vector3 intake, out Vector3 outFrom)
        {
            Part(body, PrimitiveType.Cube, "Floor", new Vector3(0f, 0.07f, 0.1f), new Vector3(3.4f, 0.14f, 2.4f), Quaternion.identity, Plank);
            float px = 1.55f, pz = 1.05f, h = 2.5f;
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Part(body, PrimitiveType.Cube, "Post", new Vector3(x * px, h * 0.5f, 0.1f + z * pz), new Vector3(0.24f, h, 0.24f), Quaternion.identity, Wood);
            for (int z = -1; z <= 1; z += 2)
                Part(body, PrimitiveType.Cube, "Beam", new Vector3(0f, h, 0.1f + z * pz), new Vector3(px * 2f + 0.3f, 0.18f, 0.2f), Quaternion.identity, DarkWood);
            // Çatı: arka yarısı hasır, ön yarısı aralıklı çıtalar (yukarıdan bakan kamera tezgahı görür).
            Part(body, PrimitiveType.Cube, "Roof", new Vector3(0f, h + 0.2f, 0.1f + pz * 0.55f), new Vector3(px * 2f + 0.8f, 0.2f, pz + 0.5f), Quaternion.Euler(-12f, 0f, 0f), Thatch);
            for (int i = 0; i < 4; i++)
                Part(body, PrimitiveType.Cube, "Slat", new Vector3(0f, h + 0.06f, 0.1f - pz + 0.15f + i * 0.42f), new Vector3(px * 2f + 0.5f, 0.08f, 0.14f), Quaternion.identity, DarkWood);
            float half = 0.95f;
            for (int s = -1; s <= 1; s += 2)
            {
                Part(body, PrimitiveType.Cube, "Upright", new Vector3(s * half, 0.95f, 0.35f), new Vector3(0.26f, 1.7f, 0.26f), Quaternion.identity, Wood);
                Part(body, PrimitiveType.Cube, "Foot", new Vector3(s * half, 0.24f, 0f), new Vector3(0.24f, 0.24f, 1.5f), Quaternion.identity, Wood);
                Part(body, PrimitiveType.Cube, "FrontPost", new Vector3(s * half, 0.5f, -0.6f), new Vector3(0.24f, 0.75f, 0.24f), Quaternion.identity, Wood);
                Part(body, PrimitiveType.Cylinder, "Knob", new Vector3(s * (half + 0.16f), 1.55f, 0.35f), new Vector3(0.14f, 0.06f, 0.14f), Quaternion.Euler(0f, 0f, 90f), DarkWood);
            }
            Part(body, PrimitiveType.Cylinder, "TopBeam", new Vector3(0f, 1.75f, 0.35f), new Vector3(0.22f, half + 0.2f, 0.22f), Quaternion.Euler(0f, 0f, 90f), Wood);
            Material thread = ArtGenerator.GetLit("M_MachineThread", null, new Color(0.95f, 0.9f, 0.76f));
            Part(body, PrimitiveType.Cylinder, "WarpBeam", new Vector3(0f, 1.15f, 0.55f), new Vector3(0.3f, half - 0.05f, 0.3f), Quaternion.Euler(0f, 0f, 90f), thread);
            for (int i = 0; i < 8; i++)
                Part(body, PrimitiveType.Cube, "Warp", new Vector3(-0.7f + i * 0.2f, 1.25f, 0.33f), new Vector3(0.035f, 0.8f, 0.035f), Quaternion.identity, thread);
            Transform heddle = new GameObject("Heddle").transform;
            heddle.SetParent(body, false);
            heddle.localPosition = new Vector3(0f, 1.2f, 0.12f);
            Part(heddle, PrimitiveType.Cube, "Top", new Vector3(0f, 0.25f, 0f), new Vector3(half * 2f - 0.2f, 0.12f, 0.12f), Quaternion.identity, DarkWood);
            Part(heddle, PrimitiveType.Cube, "Bottom", new Vector3(0f, -0.25f, 0f), new Vector3(half * 2f - 0.2f, 0.12f, 0.12f), Quaternion.identity, DarkWood);
            for (int i = 0; i < 7; i++)
                Part(heddle, PrimitiveType.Cube, "Reed", new Vector3(-0.66f + i * 0.22f, 0f, 0f), new Vector3(0.03f, 0.5f, 0.03f), Quaternion.identity, Iron);
            Transform shuttle = new GameObject("Shuttle").transform;
            shuttle.SetParent(body, false);
            shuttle.localPosition = new Vector3(0f, 0.95f, -0.12f);
            Part(shuttle, PrimitiveType.Cube, "Body", Vector3.zero, new Vector3(0.6f, 0.11f, 0.17f), Quaternion.identity, Wood);
            Part(shuttle, PrimitiveType.Cube, "Tip", new Vector3(0.36f, 0f, 0f), new Vector3(0.16f, 0.08f, 0.1f), Quaternion.Euler(0f, 45f, 0f), Wood);
            Part(shuttle, PrimitiveType.Cube, "Tip", new Vector3(-0.36f, 0f, 0f), new Vector3(0.16f, 0.08f, 0.1f), Quaternion.Euler(0f, 45f, 0f), Wood);
            Material red = ArtGenerator.GetLit("M_SailRed", null, new Color(0.85f, 0.2f, 0.17f));
            Part(shuttle, PrimitiveType.Cube, "Yarn", new Vector3(0f, 0.07f, 0f), new Vector3(0.3f, 0.07f, 0.1f), Quaternion.identity, red);
            Material sail = ArtGenerator.GetLit("M_SailStripes", ArtGenerator.StripeTexture("Tex_SailStripes", new Color(0.86f, 0.2f, 0.17f), new Color(0.97f, 0.93f, 0.85f), 6, false), Color.white);
            GameObject sheet = Part(body, PrimitiveType.Cube, "Cloth", new Vector3(0f, 0.86f, -0.32f), new Vector3(half * 1.7f, 0.03f, 0.55f), Quaternion.Euler(18f, 0f, 0f), sail);
            Transform roll = new GameObject("ClothRoll").transform;
            roll.SetParent(body, false);
            roll.localPosition = new Vector3(0f, 0.72f, -0.62f);
            Part(roll, PrimitiveType.Cylinder, "Roll", Vector3.zero, new Vector3(0.32f, half * 0.85f, 0.32f), Quaternion.Euler(0f, 0f, 90f), sail);
            Part(body, PrimitiveType.Cylinder, "RollAxle", new Vector3(0f, 0.72f, -0.62f), new Vector3(0.1f, half + 0.12f, 0.1f), Quaternion.Euler(0f, 0f, 90f), DarkWood);
            Part(body, PrimitiveType.Cube, "Shelf", new Vector3(-1.45f, 0.55f, 0.75f), new Vector3(0.45f, 0.08f, 0.9f), Quaternion.identity, Plank);
            Color[] yarns = { new Color(0.85f, 0.2f, 0.17f), new Color(0.97f, 0.93f, 0.85f), new Color(0.95f, 0.62f, 0.2f), new Color(0.3f, 0.5f, 0.75f) };
            for (int i = 0; i < yarns.Length; i++)
                Part(body, PrimitiveType.Cylinder, "Yarn", new Vector3(-1.45f, 0.72f, 0.45f + i * 0.2f), new Vector3(0.16f, 0.12f, 0.16f), Quaternion.identity,
                     ArtGenerator.GetLit("M_Yarn" + i, null, yarns[i]));

            BuilderUtil.SetField(machine, "heddle", heddle);
            BuilderUtil.SetFloat(machine, "heddleTravel", 0.24f);
            BuilderUtil.SetField(machine, "shuttle", shuttle);
            BuilderUtil.SetFloat(machine, "shuttleTravel", 0.62f);
            BuilderUtil.SetField(machine, "clothRoll", roll);
            BuilderUtil.SetField(machine, "clothSheet", sheet.GetComponent<Renderer>());
            BuilderUtil.SetInt(machine, "beatsPerProduct", 4);
            intake = new Vector3(-0.95f, 0.65f, -0.75f);
            outFrom = new Vector3(0.55f, 0.6f, -0.95f);
        }

        // ================================================================ Demirci: kiremit çatılı açık tezgah

        private static void BuildForge(Transform body, Machine machine, out Vector3 intake, out Vector3 outFrom)
        {
            Material stone = ArtGenerator.GetLit("M_ForgeStone", null, new Color(0.5f, 0.5f, 0.56f));
            Part(body, PrimitiveType.Cube, "Slab", new Vector3(0f, 0.06f, 0.15f), new Vector3(3.4f, 0.12f, 2.4f), Quaternion.identity, stone);
            for (int s = -1; s <= 1; s += 2)
                Part(body, PrimitiveType.Cube, "Post", new Vector3(s * 1.35f, 1.3f, 0.95f), new Vector3(0.24f, 2.6f, 0.24f), Quaternion.Euler(0f, 0f, s * 4f), Wood);
            Part(body, PrimitiveType.Cube, "RoofBeam", new Vector3(0f, 2.6f, 0.95f), new Vector3(3.3f, 0.22f, 0.28f), Quaternion.identity, DarkWood);
            for (int i = 0; i < 5; i++)
            {
                float x = -1.3f + i * 0.65f;
                Part(body, PrimitiveType.Cube, "Tile", new Vector3(x, 2.72f - Mathf.Abs(x) * 0.08f, 0.85f), new Vector3(0.62f, 0.12f, 1.0f), Quaternion.Euler(-22f, 0f, x * 4f), Tile);
            }
            Part(body, PrimitiveType.Cube, "TileRidge", new Vector3(0f, 2.85f, 1.0f), new Vector3(3.4f, 0.14f, 0.3f), Quaternion.identity, Tile);
            Vector3 hearth = new Vector3(-0.95f, 0f, 0.3f);
            for (int i = 0; i < 8; i++)
            {
                Quaternion q = Quaternion.Euler(0f, i * 45f, 0f);
                Part(body, PrimitiveType.Cube, "Brick", hearth + q * new Vector3(0f, 0.42f, 0.45f), new Vector3(0.42f, 0.84f, 0.2f), q, Brick);
            }
            Part(body, PrimitiveType.Cylinder, "HearthFill", hearth + Vector3.up * 0.4f, new Vector3(0.85f, 0.4f, 0.85f), Quaternion.identity, Brick);
            Transform ember = new GameObject("Ember").transform;
            ember.SetParent(body, false);
            ember.localPosition = hearth + Vector3.up * 0.84f;
            for (int i = 0; i < 6; i++)
                Part(ember, PrimitiveType.Cube, "Coal", new Vector3(Mathf.Cos(i) * 0.22f, 0.02f * (i % 2), Mathf.Sin(i * 1.7f) * 0.2f), new Vector3(0.2f, 0.14f, 0.18f), Quaternion.Euler(0f, i * 37f, 15f), Ember);
            Part(body, PrimitiveType.Cube, "Poker", hearth + new Vector3(0.1f, 1.2f, 0.1f), new Vector3(0.05f, 0.9f, 0.05f), Quaternion.Euler(0f, 0f, -20f), Iron);
            Transform bellows = new GameObject("Bellows").transform;
            bellows.SetParent(body, false);
            bellows.localPosition = hearth + new Vector3(-0.85f, 0.35f, 0.1f);
            Part(bellows, PrimitiveType.Cube, "Bag", new Vector3(0f, 0.12f, 0f), new Vector3(0.5f, 0.25f, 0.65f), Quaternion.identity, Leather);
            Part(bellows, PrimitiveType.Cube, "Board", new Vector3(0f, 0.26f, 0f), new Vector3(0.55f, 0.05f, 0.7f), Quaternion.identity, Wood);
            Part(body, PrimitiveType.Cube, "BellowsBase", bellows.localPosition + new Vector3(0f, -0.02f, 0f), new Vector3(0.55f, 0.05f, 0.7f), Quaternion.identity, Wood);
            Vector3 anvilPos = new Vector3(0.45f, 0f, -0.1f);
            Part(body, PrimitiveType.Cylinder, "Stump", anvilPos + Vector3.up * 0.25f, new Vector3(0.7f, 0.25f, 0.7f), Quaternion.identity, DarkWood);
            Part(body, PrimitiveType.Cube, "AnvilBody", anvilPos + Vector3.up * 0.7f, new Vector3(0.55f, 0.4f, 0.42f), Quaternion.identity, Iron);
            Part(body, PrimitiveType.Cube, "AnvilTop", anvilPos + Vector3.up * 0.95f, new Vector3(0.95f, 0.16f, 0.48f), Quaternion.identity, Iron);
            Part(body, PrimitiveType.Cube, "AnvilHorn", anvilPos + new Vector3(0.6f, 0.95f, 0f), new Vector3(0.3f, 0.12f, 0.24f), Quaternion.Euler(0f, 0f, -12f), Iron);
            Transform cam = new GameObject("Cam").transform;
            cam.SetParent(body, false);
            cam.localPosition = anvilPos + new Vector3(0.75f, 0.9f, 0.85f);
            Part(cam, PrimitiveType.Cylinder, "Wheel", Vector3.zero, new Vector3(0.7f, 0.08f, 0.7f), Quaternion.Euler(90f, 0f, 0f), Wood);
            Part(cam, PrimitiveType.Cube, "Lobe", new Vector3(0.28f, 0f, 0f), new Vector3(0.22f, 0.22f, 0.12f), Quaternion.identity, Iron);
            for (int i = 0; i < 2; i++) Part(cam, PrimitiveType.Cube, "Spoke", Vector3.zero, new Vector3(0.06f, 0.66f, 0.1f), Quaternion.Euler(0f, 0f, i * 90f), DarkWood);
            Part(body, PrimitiveType.Cube, "CamPost", cam.localPosition + new Vector3(0f, -0.45f, 0.12f), new Vector3(0.14f, 0.9f, 0.14f), Quaternion.identity, Wood);
            Part(body, PrimitiveType.Cube, "HammerPost", anvilPos + new Vector3(0f, 0.8f, 0.8f), new Vector3(0.2f, 1.6f, 0.2f), Quaternion.identity, Wood);
            Transform pivot = new GameObject("HammerPivot").transform;
            pivot.SetParent(body, false);
            pivot.localPosition = anvilPos + new Vector3(0f, 1.55f, 0.8f);
            Part(pivot, PrimitiveType.Cube, "Arm", new Vector3(0f, 0f, -0.45f), new Vector3(0.12f, 0.12f, 0.95f), Quaternion.identity, Wood);
            Part(pivot, PrimitiveType.Cube, "Head", new Vector3(0f, -0.2f, -0.88f), new Vector3(0.34f, 0.3f, 0.3f), Quaternion.identity, Iron);
            pivot.localRotation = Quaternion.Euler(-4f, 0f, 0f);
            Part(body, PrimitiveType.Cylinder, "WaterBarrel", new Vector3(1.45f, 0.4f, 0.75f), new Vector3(0.6f, 0.4f, 0.6f), Quaternion.identity, Wood);
            Part(body, PrimitiveType.Cylinder, "WaterTop", new Vector3(1.45f, 0.81f, 0.75f), new Vector3(0.5f, 0.02f, 0.5f), Quaternion.identity, Water);
            Material rust = ArtGenerator.GetLit("M_Rust", null, new Color(0.55f, 0.32f, 0.2f));
            Part(body, PrimitiveType.Cube, "ScrapPile", new Vector3(-1.5f, 0.15f, -0.65f), new Vector3(0.4f, 0.22f, 0.3f), Quaternion.Euler(0f, 25f, 8f), rust);
            Part(body, PrimitiveType.Cube, "ScrapPile", new Vector3(-1.3f, 0.12f, -0.85f), new Vector3(0.3f, 0.16f, 0.26f), Quaternion.Euler(0f, 60f, -6f), Iron);
            for (int i = 0; i < 4; i++)
                Part(body, PrimitiveType.Cylinder, "Nail", new Vector3(1.1f + i * 0.06f, 0.18f, -0.45f), new Vector3(0.03f, 0.14f, 0.03f), Quaternion.Euler(90f, 0f, i * 10f), Iron);
            ParticleSystem sparks = Particles(body, "Sparks", anvilPos + Vector3.up * 1.1f, new Color(1f, 0.85f, 0.3f), new Color(1f, 0.5f, 0.1f), 2f, 4.5f, 0.08f, 1.2f, 0.45f, 40, 0f);
            ParticleSystem.MainModule sm = sparks.main;
            sm.loop = false;
            ParticleSystem smoke = Particles(body, "Smoke", hearth + Vector3.up * 1.0f, new Color(0.6f, 0.6f, 0.62f, 0.5f), new Color(0.75f, 0.75f, 0.78f, 0.4f), 0.3f, 0.7f, 0.35f, -0.05f, 1.6f, 12, 5f);

            BuilderUtil.SetField(machine, "hammer", pivot);
            BuilderUtil.SetField(machine, "cam", cam);
            BuilderUtil.SetField(machine, "bellows", bellows);
            BuilderUtil.SetField(machine, "ember", ember);
            BuilderUtil.SetField(machine, "sparks", sparks);
            BuilderUtil.SetField(machine, "dust", smoke);
            BuilderUtil.SetInt(machine, "beatsPerProduct", 3);
            intake = hearth + new Vector3(0.1f, 0.9f, -0.5f);
            outFrom = anvilPos + new Vector3(0.45f, 0.65f, -0.45f);
        }
    }
}
