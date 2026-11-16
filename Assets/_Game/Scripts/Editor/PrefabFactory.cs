// PrefabFactory.cs
// Her oyun nesnesi için Assets/_Game/Prefabs altında prefab üretir. Her prefab'ın içinde "Model" adlı bir
// alt nesne vardır (ModelCatalog'daki model); modeli değiştirmek kolay olsun diye oyun mantığı kökte durur.
// - Modeller hedef boyuta ölçeklenir, tabanı yere oturur, malzemeleri paylaşımlı malzemelerle değiştirilir.
// - Karakterler: animasyon, taşıma yığını, yumuşak gölge, toz; binalar: giriş/çıkış alanları, halka, efekt.
// Komut her çalıştığında prefab'lar yeniden üretilir (GUID'leri korunur, sahnedeki bağlantılar bozulmaz).

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace KayipAda.EditorTools
{
    public static class PrefabFactory
    {
        public const string Folder = BuilderUtil.Root + "/Prefabs";
        private const string AtlasPath = BuilderUtil.ThirdParty + "/Quaternius_Pirate/Textures/Atlas_Pirate.png";
        private const string KenneyPirateColormap = BuilderUtil.ThirdParty + "/Kenny_Pirate/FBX format/Textures/colormap.png";
        private const string KenneySurvivalColormap = BuilderUtil.ThirdParty + "/Kenny_Survival/FBX format/Textures/colormap.png";

        // Bulunamayan modeller (Modeller.md ve konsol için).
        public static readonly List<string> MissingModels = new List<string>();

        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        // Kenney Nature malzeme adı → Quaternius tonlarına yakın renk.
        private static readonly Dictionary<string, Color> NaturePalette = new Dictionary<string, Color>
        {
            { "leafsGreen", new Color(0.36f, 0.68f, 0.22f) },
            { "leafsDark", new Color(0.22f, 0.50f, 0.20f) },
            { "grass", new Color(0.42f, 0.72f, 0.24f) },
            { "woodBark", new Color(0.50f, 0.32f, 0.18f) },
            { "woodInner", new Color(0.88f, 0.72f, 0.48f) },
            { "stone", new Color(0.62f, 0.62f, 0.64f) },
            { "dirt", new Color(0.56f, 0.40f, 0.24f) },
            { "colorRed", new Color(0.90f, 0.28f, 0.25f) },
            { "colorYellow", new Color(1.00f, 0.82f, 0.25f) },
            { "colorPurple", new Color(0.62f, 0.38f, 0.85f) }
        };

        public static void Reset()
        {
            MissingModels.Clear();
            materialCache.Clear();
        }

        // ================================================================ Model ve malzeme

        // Modeli parent'ın altına kurar: hedef boyuta ölçekler, tabanını parent'ın yerine oturtur.
        public static GameObject CreateModel(ModelSpec spec, Transform parent)
        {
            GameObject source = BuilderUtil.Load<GameObject>(spec.Path);
            GameObject model;
            if (source == null)
            {
                if (!MissingModels.Contains(spec.Key)) MissingModels.Add(spec.Key + " → " + spec.Path);
                model = GameObject.CreatePrimitive(PrimitiveType.Cube); // yer tutucu
                Object.DestroyImmediate(model.GetComponent<Collider>());
                model.GetComponent<Renderer>().sharedMaterial = ArtGenerator.GetLit("M_Missing", null, new Color(1f, 0.3f, 0.8f));
            }
            else
            {
                model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                ApplySharedMaterials(model, spec.Pack);
                if (spec.Upright) BakeSkinned(model, spec.Key);
            }

            model.name = "Model";
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            Normalize(model, spec);
            if (spec.Upright) Straighten(model, spec);
            BuilderUtil.SetShadows(model, spec.CastShadows);
            return model;
        }

        private static void Normalize(GameObject model, ModelSpec spec)
        {
            Bounds b = WorldBounds(model);
            float current = spec.Axis == SizeAxis.Height ? b.size.y : Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (current < 0.0001f) return;

            model.transform.localScale *= spec.Size / current;
            b = WorldBounds(model);
            Vector3 origin = model.transform.parent != null ? model.transform.parent.position : Vector3.zero;
            Vector3 offset = new Vector3(spec.CenterXZ ? b.center.x - origin.x : 0f, b.min.y - origin.y, spec.CenterXZ ? b.center.z - origin.z : 0f);
            model.transform.position -= offset;
        }

        // İskeletli (skinned) modeli sabit meshe çevirir: Quaternius palmiyesi iskeletli geliyor ve editörde
        // ölçülen duruşu ekranda çizilenden farklı. Sabit mesh hem doğru ölçülür hem de telefonda daha ucuz çizilir.
        // Mesh, Art/Generated/Baked_<ad>.asset olarak kaydedilir.
        private static void BakeSkinned(GameObject model, string key)
        {
            SkinnedMeshRenderer[] skinned = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (skinned.Length == 0) return;
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            for (int i = 0; i < skinned.Length; i++)
            {
                SkinnedMeshRenderer smr = skinned[i];
                Mesh baked = new Mesh { name = "Baked_" + key + "_" + i };
                smr.BakeMesh(baked, true);
                baked.RecalculateBounds();
                string path = ArtGenerator.GeneratedFolder + "/Baked_" + key + (i > 0 ? "_" + i : "") + ".asset";
                baked = BuilderUtil.SaveAsset(baked, path);

                GameObject go = new GameObject(smr.name + "_Static");
                go.transform.SetParent(model.transform, false);
                go.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                go.transform.localScale = Vector3.one;
                Vector3 lossy = go.transform.lossyScale; // model ölçeği zaten pişmiş meshte: dünyada 1 olsun
                go.transform.localScale = new Vector3(1f / lossy.x, 1f / lossy.y, 1f / lossy.z);
                go.AddComponent<MeshFilter>().sharedMesh = baked;
                go.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials;
                Object.DestroyImmediate(smr);
            }
            // Kemikler ve animasyon artık gereksiz.
            foreach (Animator animator in model.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(animator);
        }

        // Eğri gövdeli modeli (palmiye) dikleştirir: gövde dibinin ortası ile tepenin (yaprakların) ortası aynı
        // dikey çizgiye gelir. Sonra boyu yeniden ölçeklenir, gövde dibi köke oturur ve biraz zemine gömülür
        // (havada duruyormuş gibi görünmesin, gölge gövdeyle hizalı olsun).
        private static void Straighten(GameObject model, ModelSpec spec)
        {
            List<Vector3> points = WorldVertices(model);
            if (points.Count == 0) return;

            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (Vector3 v in points) { minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
            float h = maxY - minY;
            // Tepe: en üstteki noktaların ortası (yapraklar). Gövde dibi: tepeye yatayda en uzak noktalar (eğri gövdenin
            // ucu). En alttaki noktalar kullanılmaz: sarkan yaprak uçları gövde dibinden daha aşağı iniyor.
            Vector3 topPoint = Centroid(points, v => v.y > maxY - h * 0.3f);
            Vector2 topXZ = new Vector2(topPoint.x, topPoint.z);
            float farthest = 0f;
            foreach (Vector3 v in points) farthest = Mathf.Max(farthest, Vector2.Distance(new Vector2(v.x, v.z), topXZ));
            Vector3 basePoint = Centroid(points, v => v.y < topPoint.y &&
                                                       Vector2.Distance(new Vector2(v.x, v.z), topXZ) > farthest * 0.9f);
            if (Vector3.Angle(topPoint - basePoint, Vector3.up) < 3f) return; // zaten dik

            Transform t = model.transform;
            Vector3 baseLocal = t.InverseTransformPoint(basePoint);
            Quaternion fix = Quaternion.FromToRotation(topPoint - basePoint, Vector3.up);
            t.rotation = fix * t.rotation;
            t.position = basePoint + fix * (t.position - basePoint);

            Bounds b = WorldBounds(model);
            if (b.size.y > 0.0001f) t.localScale *= spec.Size / b.size.y;
            b = WorldBounds(model);
            Vector3 root = t.parent != null ? t.parent.position : Vector3.zero;
            Vector3 trunk = t.TransformPoint(baseLocal);
            t.position -= new Vector3(trunk.x - root.x, trunk.y - root.y + UprightSink, trunk.z - root.z);
        }

        // Modelin tüm köşe noktaları dünya koordinatında (normal ve iskeletli/skinned meshler; gizli nesnelerde de).
        public static List<Vector3> WorldVertices(GameObject root)
        {
            List<Vector3> points = new List<Vector3>();
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                Matrix4x4 m = mf.transform.localToWorldMatrix;
                foreach (Vector3 v in mf.sharedMesh.vertices) points.Add(m.MultiplyPoint3x4(v));
            }
            foreach (SkinnedMeshRenderer smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                Mesh baked = new Mesh();
                smr.BakeMesh(baked, true); // ölçek dahil, renderer'ın yerel uzayında
                Matrix4x4 m = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                foreach (Vector3 v in baked.vertices) points.Add(m.MultiplyPoint3x4(v));
                Object.DestroyImmediate(baked);
            }
            return points;
        }

        private const float UprightSink = 0.06f; // gövde dibi zemine bu kadar gömülür (metre)

        private static Vector3 Centroid(List<Vector3> points, System.Func<Vector3, bool> filter)
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            foreach (Vector3 v in points)
            {
                if (!filter(v)) continue;
                sum += v;
                n++;
            }
            return n > 0 ? sum / n : Vector3.zero;
        }

        public static Bounds WorldBounds(GameObject go)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        // Modelin içe aktarılmış malzemelerini, pakete göre paylaşımlı malzemelerle değiştirir.
        private static void ApplySharedMaterials(GameObject model, ModelPack pack)
        {
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = SharedMaterial(mats[i], pack);
                r.sharedMaterials = mats;
            }
        }

        private static Material SharedMaterial(Material source, ModelPack pack)
        {
            if (pack == ModelPack.Quaternius)
            {
                return Cached("M_Quaternius", () => ArtGenerator.GetLit("M_Quaternius", BuilderUtil.Load<Texture2D>(AtlasPath), Color.white));
            }

            Texture texture = null;
            Color color = Color.white;
            string sourceName = source != null ? source.name : "Default";
            if (source != null)
            {
                if (source.HasProperty("_BaseMap")) texture = source.GetTexture("_BaseMap");
                else if (source.HasProperty("_MainTex")) texture = source.GetTexture("_MainTex");
                if (source.HasProperty("_BaseColor")) color = source.GetColor("_BaseColor");
                else if (source.HasProperty("_Color")) color = source.GetColor("_Color");
            }

            // Kenney Pirate/Survival: tek renk paleti dokusu (colormap).
            if (pack == ModelPack.KenneyPirate || pack == ModelPack.KenneySurvival)
            {
                if (texture != null || sourceName.ToLowerInvariant().Contains("colormap"))
                {
                    string path = pack == ModelPack.KenneyPirate ? KenneyPirateColormap : KenneySurvivalColormap;
                    string name = "M_" + pack;
                    return Cached(name, () => ArtGenerator.GetLit(name, BuilderUtil.Load<Texture2D>(path), Color.white));
                }
            }

            // Kenney Nature renkleri içe aktarmada soluk/camgöbeği geliyor: Quaternius'la uyumlu sıcak bir palet kullan.
            if (pack == ModelPack.KenneyNature && NaturePalette.TryGetValue(sourceName, out Color palette)) color = palette;

            // Renkli (dokusuz) malzemeler: renk başına bir paylaşımlı malzeme.
            color.a = 1f;
            string key = "M_" + pack + "_" + Sanitize(sourceName);
            Texture tex = texture;
            Color col = color;
            return Cached(key, () => ArtGenerator.GetLit(key, tex, col));
        }

        private static Material Cached(string key, System.Func<Material> create)
        {
            if (!materialCache.TryGetValue(key, out Material mat) || mat == null)
            {
                mat = create();
                materialCache[key] = mat;
            }
            return mat;
        }

        private static string Sanitize(string value)
        {
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i])) chars[i] = '_';
            }
            return new string(chars);
        }

        private static GameObject Save(GameObject root, string path)
        {
            BuilderUtil.EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject Instance(GameObject prefab, Transform parent, Vector3 localPos)
        {
            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        // ================================================================ Ortak parçalar

        // Karakterin/eşyanın altında yumuşak, yuvarlak gölge.
        public static void AddBlobShadow(Transform parent, float size)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = "BlobShadow";
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(size, size, 1f);
            Renderer r = quad.GetComponent<Renderer>();
            r.sharedMaterial = ArtGenerator.GetTransparent("M_BlobShadow", ArtGenerator.LoadSprite(ArtGenerator.SoftCirclePath)?.texture,
                                                           new Color(0f, 0f, 0f, 0.35f));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // Yere yatık yarı saydam işaret (bina giriş/çıkış alanı, Pazar alanı, kapalı bina zemini).
        public static GameObject AddGroundDecal(string name, Transform parent, Vector3 localPos, Vector2 size, Color color)
        {
            return AddGroundDecal(name, parent, localPos, size, color, ArtGenerator.RoundedRectPath, 0f);
        }

        // Yere yatık, istenen dokuyla (daire, ok) yarı saydam işaret. yaw: dokunun yukarısının baktığı yön (derece).
        public static GameObject AddGroundDecal(string name, Transform parent, Vector3 localPos, Vector2 size, Color color,
                                                string texturePath, float yaw)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPos + Vector3.up * 0.025f;
            quad.transform.localRotation = Quaternion.Euler(90f, yaw, 0f);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            Renderer r = quad.GetComponent<Renderer>();
            string shape = texturePath == ArtGenerator.RoundedRectPath ? "" : System.IO.Path.GetFileNameWithoutExtension(texturePath) + "_";
            string key = "M_Decal_" + shape + ColorUtility.ToHtmlStringRGBA(color);
            r.sharedMaterial = Cached(key, () => ArtGenerator.GetTransparent(key, ArtGenerator.LoadSprite(texturePath)?.texture, color));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return quad;
        }

        // Dikey silindir şeklinde trigger alan (karakterler içine girince etkileşir).
        private static void AddRoundTrigger(GameObject go, float radius)
        {
            CapsuleCollider c = go.AddComponent<CapsuleCollider>();
            c.isTrigger = true;
            c.direction = 1;
            c.radius = radius;
            c.height = 3f;
            c.center = new Vector3(0f, 1.5f, 0f);
        }

        private static void AddBoxTrigger(GameObject go, Vector3 center, Vector3 size)
        {
            BoxCollider c = go.AddComponent<BoxCollider>();
            c.isTrigger = true;
            c.center = center;
            c.size = size;
        }

        // ================================================================ Kaynaklar ve para

        public static ResourceItem BuildItem(ModelSpec spec)
        {
            GameObject root = new GameObject(spec.Key);
            CreateModel(spec, root.transform);
            root.AddComponent<ResourceItem>();
            return Save(root, Folder + "/Items/" + spec.Key + ".prefab").GetComponent<ResourceItem>();
        }

        public static GameObject BuildCoin()
        {
            GameObject root = new GameObject("Coin");
            GameObject model = CreateModel(ModelCatalog.Coin, root.transform);
            model.transform.localPosition -= Vector3.up * ModelCatalog.Coin.Size * 0.5f; // ortası köke gelsin (dönerken)
            return Save(root, Folder + "/Items/Coin.prefab");
        }

        // ================================================================ Karakterler

        public static GameObject BuildCharacter(ModelSpec spec, RuntimeAnimatorController controller, GameConfig config,
                                                GameObject dustPrefab, bool isPlayer)
        {
            GameObject root = new GameObject(spec.Key);
            Transform body = new GameObject("Body").transform; // yürüme yönüne döner
            body.SetParent(root.transform, false);
            GameObject model = CreateModel(spec, body);

            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            // Taşınan yığın göğsün önünde başlar.
            Transform stackRoot = new GameObject("StackRoot").transform;
            stackRoot.SetParent(body, false);
            stackRoot.localPosition = new Vector3(0f, spec.Size * 0.5f, 0.5f);

            AddBlobShadow(root.transform, 1.2f);
            ParticleSystem dust = null;
            if (dustPrefab != null)
            {
                GameObject d = Instance(dustPrefab, root.transform, new Vector3(0f, 0.1f, 0f));
                dust = d.GetComponent<ParticleSystem>();
            }

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 0.35f;
            capsule.height = spec.Size;
            capsule.center = new Vector3(0f, spec.Size * 0.5f, 0f);

            CarryStack stack = root.AddComponent<CarryStack>();
            BuilderUtil.SetField(stack, "config", config);
            BuilderUtil.SetField(stack, "stackRoot", stackRoot);
            // Görünen sınır aşılınca yığının üstünde "x40" etiketi.
            TMPro.TextMeshPro count = UiFactory.CreateWorldText("StackCount", root.transform, Vector3.up * 2.4f, "x0", 4.2f, UiFactory.Gold);
            count.gameObject.AddComponent<Billboard>();
            count.gameObject.SetActive(false);
            BuilderUtil.SetField(stack, "countLabel", count);

            CharacterVisual visual = root.AddComponent<CharacterVisual>();
            BuilderUtil.SetField(visual, "config", config);
            BuilderUtil.SetField(visual, "animator", animator);
            BuilderUtil.SetField(visual, "model", body);
            BuilderUtil.SetField(visual, "stack", stack);
            BuilderUtil.SetField(visual, "dust", dust);
            BuilderUtil.SetBool(visual, "footsteps", isPlayer);

            if (isPlayer)
            {
                BuilderUtil.SetField(root.AddComponent<PlayerController>(), "config", config);
            }
            else
            {
                root.AddComponent<WorkerMover>();
            }

            string folder = isPlayer || spec.Key.StartsWith("Worker") ? "/Characters/" : "/Enemies/";
            return Save(root, Folder + folder + spec.Key + ".prefab");
        }

        // Düşman (şimdilik sadece görünüş + bekleme animasyonu).
        public static GameObject BuildEnemy(ModelSpec spec, RuntimeAnimatorController controller)
        {
            GameObject root = new GameObject(spec.Key);
            Transform body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            GameObject model = CreateModel(spec, body);
            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            AddBlobShadow(root.transform, spec.Axis == SizeAxis.Height ? 1.2f : spec.Size * 0.6f);
            return Save(root, Folder + "/Enemies/" + spec.Key + ".prefab");
        }

        // Geliştirme tezgahı: model + oyuncu üstüne gelince paneli açan alan (panel sahnede bağlanır).
        public static GameObject BuildUpgradeStation(GameConfig config)
        {
            GameObject root = new GameObject("UpgradeStation");
            AddRoundTrigger(root, 1.5f);
            AddGroundDecal("Decal", root.transform, Vector3.zero, new Vector2(2.6f, 2.6f), new Color(0.45f, 0.85f, 1f, 0.45f));

            Transform bench = new GameObject("Bench").transform;
            bench.SetParent(root.transform, false);
            bench.localPosition = new Vector3(0f, 0f, 1.6f);
            CreateModel(ModelCatalog.UpgradeBench, bench);
            BuilderUtil.SetVector2(bench.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(1.8f, 1f));

            Transform fx = new GameObject("EffectPoint").transform;
            fx.SetParent(root.transform, false);
            fx.localPosition = new Vector3(0f, 1.4f, 1.6f);

            UiFactory.CreateWorldLabel("Label", root.transform, new Vector3(0f, 3f, 1.6f), "GELİŞTİR", 5f, new Color(0.55f, 0.9f, 1f));

            UpgradeStation station = root.AddComponent<UpgradeStation>();
            BuilderUtil.SetField(station, "config", config);
            BuilderUtil.SetField(station, "effectPoint", fx);
            return Save(root, Folder + "/World/UpgradeStation.prefab");
        }

        private static ModelSpec FindDecor(string key)
        {
            foreach (ModelSpec spec in ModelCatalog.Decor) if (spec.Key == key) return spec;
            return ModelCatalog.Decor[0];
        }

        // Küçük parlama: yukarı süzülen altın kıvılcımlar.
        public static GameObject BuildSparkle()
        {
            ParticleSystem ps = NewParticles("Sparkle", "M_Particle", ArtGenerator.SoftCirclePath, out GameObject root);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.4f), new Color(1f, 1f, 0.85f));
            main.gravityModifier = -0.2f;
            main.maxParticles = 30;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;
            FadeOut(ps);
            return Save(root, Folder + "/FX/Sparkle.prefab");
        }

        // Havai fişek: yerden yükselen, iz bırakan roket; tepede (ölünce) çok renkli, iz bırakan parçacıklarla patlar.
        public static GameObject BuildFirework()
        {
            ParticleSystem rocket = NewParticles("Firework", "M_Particle", ArtGenerator.SoftCirclePath, out GameObject root);
            ParticleSystem.MainModule main = rocket.main;
            main.duration = 0.2f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 12f);
            main.startSize = 0.35f;
            main.startColor = new Color(1f, 0.9f, 0.6f);
            main.gravityModifier = 0.6f;
            main.maxParticles = 2;
            ParticleSystem.EmissionModule emission = rocket.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            ParticleSystem.ShapeModule shape = rocket.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.1f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            AddTrails(rocket, 0.35f, 0.12f);

            // Patlama: ölen roketin yerinde çok renkli parçacıklar, her biri iz bırakır.
            ParticleSystem burst = NewParticles("Burst", "M_Particle", ArtGenerator.SoftCirclePath, out GameObject burstGo);
            burstGo.transform.SetParent(root.transform, false);
            ParticleSystem.MainModule b = burst.main;
            b.duration = 0.2f;
            b.loop = false;
            b.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
            b.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
            b.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.38f);
            Gradient colors = new Gradient();
            colors.SetKeys(new[]
            {
                new GradientColorKey(new Color(1f, 0.85f, 0.25f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.45f), 0.25f),
                new GradientColorKey(new Color(0.4f, 0.75f, 1f), 0.5f), new GradientColorKey(new Color(0.55f, 1f, 0.45f), 0.75f),
                new GradientColorKey(new Color(0.85f, 0.45f, 1f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            b.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
            b.gravityModifier = 0.45f;
            b.maxParticles = 80;
            ParticleSystem.EmissionModule be = burst.emission;
            be.rateOverTime = 0f;
            be.SetBursts(new[] { new ParticleSystem.Burst(0f, 70) });
            ParticleSystem.ShapeModule bs = burst.shape;
            bs.shapeType = ParticleSystemShapeType.Sphere;
            bs.radius = 0.15f;
            ParticleSystem.LimitVelocityOverLifetimeModule drag = burst.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 1.2f;
            AddTrails(burst, 0.5f, 0.6f);
            FadeOut(burst);

            ParticleSystem.SubEmittersModule sub = rocket.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(burst, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);
            return Save(root, Folder + "/FX/Firework.prefab");
        }

        // Parçacıkların arkasında incelen iz.
        private static void AddTrails(ParticleSystem ps, float lifetime, float widthOverTrail)
        {
            ParticleSystem.TrailModule trails = ps.trails;
            trails.enabled = true;
            trails.lifetime = lifetime;
            trails.dieWithParticles = true;
            trails.inheritParticleColor = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, widthOverTrail));
            ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
            r.trailMaterial = r.sharedMaterial;
        }

        // Gemi parçası yerine oturunca kalkan kum/toz bulutu.
        public static GameObject BuildPuff()
        {
            ParticleSystem ps = NewParticles("Puff", "M_Particle", ArtGenerator.SoftCirclePath, out GameObject root);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            main.startColor = new Color(0.93f, 0.85f, 0.68f, 0.85f);
            main.maxParticles = 16;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            FadeOut(ps);
            return Save(root, Folder + "/FX/Puff.prefab");
        }

        // Gemi aşaması bitince altın ışık patlaması: büyüyüp sönen parlak halka + dışa saçılan ışık çizgileri.
        public static GameObject BuildGoldBurst()
        {
            ParticleSystem glow = NewParticles("GoldBurst", "M_Particle", ArtGenerator.SoftCirclePath, out GameObject root);
            ParticleSystem.MainModule main = glow.main;
            main.duration = 0.2f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = 0.9f;
            main.startSpeed = 0f;
            main.startSize = 7f;
            main.startColor = new Color(1f, 0.85f, 0.35f, 0.75f);
            main.maxParticles = 2;
            ParticleSystem.EmissionModule emission = glow.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            ParticleSystem.SizeOverLifetimeModule size = glow.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1.4f));
            FadeOut(glow);

            ParticleSystem rays = NewParticles("Rays", "M_Particle", ArtGenerator.SoftCirclePath, out GameObject raysGo);
            raysGo.transform.SetParent(root.transform, false);
            ParticleSystem.MainModule r = rays.main;
            r.duration = 0.2f;
            r.loop = false;
            r.playOnAwake = true;
            r.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
            r.startSpeed = new ParticleSystem.MinMaxCurve(7f, 12f);
            r.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            r.startColor = new Color(1f, 0.9f, 0.45f);
            r.maxParticles = 40;
            ParticleSystem.EmissionModule re = rays.emission;
            re.rateOverTime = 0f;
            re.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });
            ParticleSystem.ShapeModule rs = rays.shape;
            rs.shapeType = ParticleSystemShapeType.Sphere;
            rs.radius = 0.5f;
            ParticleSystemRenderer rr = raysGo.GetComponent<ParticleSystemRenderer>();
            rr.renderMode = ParticleSystemRenderMode.Stretch;
            rr.velocityScale = 0.12f;
            rr.lengthScale = 2f;
            FadeOut(rays);
            return Save(root, Folder + "/FX/GoldBurst.prefab");
        }

        // Ekranı kaplayan konfeti: kameraya bağlı, geniş bir alandan sürekli yağar (süreyi Effects ayarlar).
        public static GameObject BuildScreenConfetti()
        {
            ParticleSystem ps = NewParticles("ScreenConfetti", "M_Confetti", ArtGenerator.SquarePath, out GameObject root);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 8f;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.38f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.35f;
            main.maxParticles = 260;
            Gradient colors = new Gradient();
            colors.SetKeys(new[]
            {
                new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.45f), 0.33f),
                new GradientColorKey(new Color(0.35f, 0.75f, 1f), 0.66f), new GradientColorKey(new Color(0.45f, 0.95f, 0.45f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 90f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9f, 0.3f, 2f); // kameranın önünde, görüşün üst kenarı boyunca
            ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.4f;
            return Save(root, Folder + "/FX/ScreenConfetti.prefab");
        }

        // Su sıçraması: beyaz-mavi damlalar yukarı fışkırıp düşer.
        public static GameObject BuildSplash()
        {
            ParticleSystem ps = NewParticles("Splash", "M_Particle", ArtGenerator.SoftCirclePath, out GameObject root);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.9f), new Color(0.6f, 0.85f, 1f, 0.8f));
            main.gravityModifier = 1.4f;
            main.maxParticles = 60;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 1.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            FadeOut(ps);
            return Save(root, Folder + "/FX/Splash.prefab");
        }

        // Halat: kodla üretilen halka modeli + ResourceItem.
        public static ResourceItem BuildRopeItem()
        {
            GameObject root = new GameObject("Rope");
            GameObject model = new GameObject("Model");
            model.transform.SetParent(root.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = ArtGenerator.BuildRopeMesh();
            MeshRenderer r = model.AddComponent<MeshRenderer>();
            r.sharedMaterial = Cached("M_Rope", () => ArtGenerator.GetLit("M_Rope", null, new Color(0.74f, 0.55f, 0.32f)));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            root.AddComponent<ResourceItem>();
            return Save(root, Folder + "/Items/Rope.prefab").GetComponent<ResourceItem>();
        }

        // ================================================================ Canlılar (kodla üretilen low-poly modeller)

        // Yengeç: turuncu-kırmızı yassı gövde, iki kıskaç, altı bacak, iki göz. Çarpışması yok.
        public static GameObject BuildCrab(GameConfig config)
        {
            GameObject root = new GameObject("Crab");
            Transform body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            Material shell = ArtGenerator.GetLit("M_Crab", null, new Color(0.93f, 0.36f, 0.18f));
            Material dark = ArtGenerator.GetLit("M_CrabDark", null, new Color(0.18f, 0.1f, 0.08f));
            Mesh round = ArtGenerator.BuildFruitMesh();
            MeshPiece(body, "Shell", round, new Vector3(0f, 0.12f, 0f), new Vector3(0.42f, 0.16f, 0.32f), shell);
            for (int s = -1; s <= 1; s += 2)
            {
                // Kıskaç: kol + iki parçalı uç (önde).
                Box(body, "Arm", new Vector3(s * 0.2f, 0.12f, 0.16f), new Vector3(0.06f, 0.06f, 0.16f), Quaternion.Euler(0f, s * 30f, 0f), shell);
                MeshPiece(body, "Claw", round, new Vector3(s * 0.26f, 0.13f, 0.27f), new Vector3(0.13f, 0.09f, 0.14f), shell);
                for (int leg = 0; leg < 3; leg++)
                {
                    float z = -0.08f + leg * 0.08f;
                    Box(body, "Leg", new Vector3(s * 0.24f, 0.06f, z), new Vector3(0.16f, 0.03f, 0.03f), Quaternion.Euler(0f, 0f, s * -35f), shell);
                }
                Box(body, "EyeStalk", new Vector3(s * 0.07f, 0.22f, 0.1f), new Vector3(0.025f, 0.08f, 0.025f), Quaternion.identity, shell);
                MeshPiece(body, "Eye", round, new Vector3(s * 0.07f, 0.27f, 0.1f), Vector3.one * 0.045f, dark);
            }
            BuilderUtil.SetShadows(root, false);
            AddBlobShadow(root.transform, 0.5f);
            Crab crab = root.AddComponent<Crab>();
            BuilderUtil.SetField(crab, "config", config);
            BuilderUtil.SetField(crab, "body", body);
            return Save(root, Folder + "/Life/Crab.prefab");
        }

        // Martı: beyaz gövde, gri kanatlar (menteşeli), sarı gaga; yere düşen yumuşak gölge.
        public static GameObject BuildGull(GameConfig config)
        {
            GameObject root = new GameObject("Seagull");
            Material white = ArtGenerator.GetLit("M_GullWhite", null, new Color(0.96f, 0.96f, 0.94f));
            Material grey = ArtGenerator.GetLit("M_GullGrey", null, new Color(0.6f, 0.64f, 0.7f));
            Material beak = ArtGenerator.GetLit("M_GullBeak", null, new Color(1f, 0.75f, 0.2f));
            Mesh round = ArtGenerator.BuildFruitMesh();
            MeshPiece(root.transform, "Body", round, Vector3.zero, new Vector3(0.22f, 0.2f, 0.6f), white);
            MeshPiece(root.transform, "Head", round, new Vector3(0f, 0.08f, 0.3f), Vector3.one * 0.17f, white);
            Box(root.transform, "Beak", new Vector3(0f, 0.06f, 0.42f), new Vector3(0.04f, 0.04f, 0.12f), Quaternion.identity, beak);
            Box(root.transform, "Tail", new Vector3(0f, 0.02f, -0.34f), new Vector3(0.16f, 0.03f, 0.14f), Quaternion.identity, grey);
            Transform left = Wing(root.transform, "LeftWing", -1f, new Vector3(0.62f, 0.03f, 0.24f), grey);
            Transform right = Wing(root.transform, "RightWing", 1f, new Vector3(0.62f, 0.03f, 0.24f), grey);
            BuilderUtil.SetShadows(root, false);
            Transform shadow = AddGroundDecal("Shadow", root.transform, Vector3.zero, new Vector2(1f, 0.6f), new Color(0f, 0f, 0f, 0.18f),
                                              ArtGenerator.SoftCirclePath, 0f).transform;
            Flyer flyer = root.AddComponent<Flyer>();
            BuilderUtil.SetField(flyer, "config", config);
            BuilderUtil.SetEnum(flyer, "kind", 0);
            BuilderUtil.SetField(flyer, "leftWing", left);
            BuilderUtil.SetField(flyer, "rightWing", right);
            BuilderUtil.SetField(flyer, "shadow", shadow);
            return Save(root, Folder + "/Life/Seagull.prefab");
        }

        // Kelebek: iki renkli, menteşeli kanat (çırpar).
        public static GameObject BuildButterfly(GameConfig config, Color color, string name)
        {
            GameObject root = new GameObject(name);
            Material wing = ArtGenerator.GetLit("M_" + name, null, color);
            Material dark = ArtGenerator.GetLit("M_CrabDark", null, new Color(0.18f, 0.1f, 0.08f));
            Box(root.transform, "Body", Vector3.zero, new Vector3(0.025f, 0.025f, 0.12f), Quaternion.identity, dark);
            Transform left = Wing(root.transform, "LeftWing", -1f, new Vector3(0.12f, 0.008f, 0.13f), wing);
            Transform right = Wing(root.transform, "RightWing", 1f, new Vector3(0.12f, 0.008f, 0.13f), wing);
            BuilderUtil.SetShadows(root, false);
            Flyer flyer = root.AddComponent<Flyer>();
            BuilderUtil.SetField(flyer, "config", config);
            BuilderUtil.SetEnum(flyer, "kind", 1);
            BuilderUtil.SetField(flyer, "leftWing", left);
            BuilderUtil.SetField(flyer, "rightWing", right);
            return Save(root, Folder + "/Life/" + name + ".prefab");
        }

        // Kanat: menteşe gövdenin yanında; kanat menteşeden dışa uzanır (Z ekseninde çırpılır).
        private static Transform Wing(Transform parent, string name, float side, Vector3 size, Material material)
        {
            Transform hinge = new GameObject(name).transform;
            hinge.SetParent(parent, false);
            hinge.localPosition = new Vector3(side * 0.05f, 0.04f, 0f);
            Box(hinge, "Blade", new Vector3(side * size.x * 0.5f, 0f, 0f), size, Quaternion.identity, material);
            return hinge;
        }

        private static void MeshPiece(Transform parent, string name, Mesh mesh, Vector3 pos, Vector3 scale, Material material)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        // Zıplayan balık: Quaternius uskumru modeli (küçük).
        public static GameObject BuildFish()
        {
            GameObject root = new GameObject("JumpFish");
            CreateModel(ModelCatalog.Fish, root.transform);
            BuilderUtil.SetShadows(root, false);
            return Save(root, Folder + "/Life/JumpFish.prefab");
        }

        // ================================================================ Meyve, meyve çalısı, keten tarlası

        // Kodla üretilen alçak poligonlu meyve: kırmızı/turuncu yuvarlak + yeşil yaprak + kahverengi sap. Taban y=0.
        public static GameObject CreateFruitModel(Transform parent, float size, bool orange)
        {
            GameObject model = new GameObject("Model");
            model.transform.SetParent(parent, false);
            Mesh sphere = ArtGenerator.BuildFruitMesh();
            Color body = orange ? new Color(1f, 0.55f, 0.12f) : new Color(0.88f, 0.16f, 0.12f);
            GameObject ball = new GameObject("Body");
            ball.transform.SetParent(model.transform, false);
            ball.transform.localPosition = new Vector3(0f, size * 0.5f, 0f);
            ball.transform.localScale = new Vector3(size, size * 0.92f, size);
            ball.AddComponent<MeshFilter>().sharedMesh = sphere;
            ball.AddComponent<MeshRenderer>().sharedMaterial = ArtGenerator.GetLit(orange ? "M_FruitOrange" : "M_FruitRed", null, body);
            Box(model.transform, "Stem", new Vector3(0f, size * 0.98f, 0f), new Vector3(size * 0.08f, size * 0.22f, size * 0.08f),
                Quaternion.identity, ArtGenerator.GetLit("M_FruitStem", null, new Color(0.45f, 0.28f, 0.12f)));
            Box(model.transform, "Leaf", new Vector3(size * 0.14f, size * 1.02f, 0f), new Vector3(size * 0.32f, size * 0.05f, size * 0.16f),
                Quaternion.Euler(0f, 0f, 25f), ArtGenerator.GetLit("M_FruitLeaf", null, new Color(0.3f, 0.7f, 0.25f)));
            BuilderUtil.SetShadows(model, false);
            return model;
        }

        private static void Box(Transform parent, string name, Vector3 pos, Vector3 size, Quaternion rot, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        public static ResourceItem BuildFruitItem()
        {
            GameObject root = new GameObject("Fruit");
            CreateFruitModel(root.transform, 0.42f, false);
            root.AddComponent<ResourceItem>();
            return Save(root, Folder + "/Items/Fruit.prefab").GetComponent<ResourceItem>();
        }

        // Taşınan kaynağın rengi tek renge boyanır (sırttaki yığında kolay ayırt edilsin: gri taş, turuncu tuğla, krem bez).
        public static ResourceItem BuildTintedItem(ModelSpec spec, Color tint)
        {
            GameObject root = new GameObject(spec.Key);
            GameObject model = CreateModel(spec, root.transform);
            Material m = ArtGenerator.GetLit("M_Item_" + spec.Key, null, tint);
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = m;
                r.sharedMaterials = mats;
            }
            root.AddComponent<ResourceItem>();
            return Save(root, Folder + "/Items/" + spec.Key + ".prefab").GetComponent<ResourceItem>();
        }

        // Meyve çalısı: yeşil çalı + üstünde belirgin kırmızı/turuncu meyveler. Toplandıkça meyveler tek tek
        // kaybolur, dolarken tek tek belirir; çalı küçülmez.
        public static GameObject BuildFruitBush(ResourceNodeType nodeType, GameConfig config)
        {
            ModelSpec spec = ModelCatalog.FruitTree;
            GameObject root = new GameObject(spec.Key);
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            GameObject bush = CreateModel(spec, visual);
            Bounds b = WorldBounds(bush);
            List<Transform> fruits = new List<Transform>();
            int count = ModelCatalog.FruitsPerBush;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count + (i % 2) * 0.35f;
                float height = Mathf.Lerp(0.42f, 0.82f, (i * 37 % 10) / 10f);
                Transform holder = new GameObject("Fruit " + i).transform;
                holder.SetParent(visual, false);
                holder.position = new Vector3(b.center.x + Mathf.Cos(angle) * b.extents.x * 0.92f, b.min.y + b.size.y * height,
                                              b.center.z + Mathf.Sin(angle) * b.extents.z * 0.92f);
                CreateFruitModel(holder, 0.32f, i % 3 == 1);
                fruits.Add(holder);
            }
            return FinishNode(root, visual, spec, nodeType, config, 1.7f, new Vector2(1.0f, 1.0f), 1.25f, new Color(0.9f, 0.25f, 0.15f),
                              null, false, false, fruits.ToArray(), "Nodes/" + spec.Key);
        }

        // Keten/saz tarlası: alçak, sarımsı-yeşil bitki kümeleri (rüzgârda sallanır). Toplandıkça boyu kısalır,
        // tükenince kısa anızlık kalır, yeniden uzar. İçinden yürünebilir (engel yok).
        public static GameObject BuildFlaxField(ResourceNodeType nodeType, GameConfig config)
        {
            ModelSpec spec = ModelCatalog.FlaxClump;
            GameObject root = new GameObject("Node_FlaxField");
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            Material flax = ArtGenerator.GetLit("M_Flax", null, ModelCatalog.FlaxColor);
            System.Random random = new System.Random(17);
            int clumps = ModelCatalog.FlaxClumps;
            for (int i = 0; i < clumps; i++)
            {
                float a = i * 2.39996f; // altın açı: kümeler düzgün dağılır
                float r = ModelCatalog.FlaxRadius * Mathf.Sqrt((i + 0.5f) / clumps);
                Transform holder = new GameObject("Clump " + i).transform;
                holder.SetParent(visual, false);
                holder.localPosition = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                holder.localRotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                holder.localScale = Vector3.one * (0.85f + (float)random.NextDouble() * 0.3f);
                GameObject model = CreateModel(spec, holder);
                foreach (Renderer rend in model.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = rend.sharedMaterials;
                    for (int k = 0; k < mats.Length; k++) mats[k] = flax;
                    rend.sharedMaterials = mats;
                }
                Sway sway = holder.gameObject.AddComponent<Sway>();
                BuilderUtil.SetField(sway, "config", config);
            }
            return FinishNode(root, visual, spec, nodeType, config, 1.7f, Vector2.zero, 1.2f, new Color(0.9f, 0.82f, 0.4f),
                              null, true, true, new Transform[0], "Nodes/Node_FlaxField");
        }

        // Kaynak noktasının ortak kısmı: trigger, (varsa) gövde engeli, vuruş parçacıkları, ResourceNode ayarları.
        private static GameObject FinishNode(GameObject root, Transform visual, ModelSpec spec, ResourceNodeType nodeType, GameConfig config,
                                             float zoneRadius, Vector2 trunkSize, float standRadius, Color chipColor, GameObject remnantGo,
                                             bool shrinks, bool heightOnly, Transform[] stockVisuals, string path)
        {
            AddRoundTrigger(root, zoneRadius);
            if (trunkSize != Vector2.zero) BuilderUtil.SetVector2(root.AddComponent<NavObstacle>(), "size", trunkSize);
            ParticleSystem chips = CreateHitChips(root.transform, Mathf.Min(spec.Size * 0.6f, 1.6f), chipColor);
            ResourceNode node = root.AddComponent<ResourceNode>();
            BuilderUtil.SetField(node, "remnant", remnantGo);
            BuilderUtil.SetField(node, "hitEffect", chips);
            BuilderUtil.SetField(node, "config", config);
            BuilderUtil.SetField(node, "nodeType", nodeType);
            BuilderUtil.SetField(node, "visual", visual);
            BuilderUtil.SetFloat(node, "standRadius", standRadius);
            BuilderUtil.SetFloat(node, "spawnHeight", Mathf.Min(spec.Size * 0.5f, 1.2f));
            BuilderUtil.SetBool(node, "shrinks", shrinks);
            BuilderUtil.SetBool(node, "shrinkHeightOnly", heightOnly);
            BuilderUtil.SetArray(node, "stockVisuals", stockVisuals);
            return Save(root, Folder + "/" + path + ".prefab");
        }

        // ================================================================ Kaynak noktaları

        public static GameObject BuildNode(ModelSpec spec, ResourceNodeType nodeType, GameConfig config,
                                           float zoneRadius, Vector2 trunkSize, float standRadius,
                                           ModelSpec fruit = null, int fruitCount = 0, Color? chipColor = null,
                                           ModelSpec remnant = null)
        {
            GameObject root = new GameObject(spec.Key);
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            GameObject model = CreateModel(spec, visual);

            // Meyve ağacı: tepenin çevresine meyveler (ağaçla birlikte sallanır).
            if (fruit != null && fruitCount > 0)
            {
                Bounds crown = WorldBounds(model);
                for (int i = 0; i < fruitCount; i++)
                {
                    float angle = i * Mathf.PI * 2f / fruitCount + 0.4f;
                    Transform holder = new GameObject("Fruit " + i).transform;
                    holder.SetParent(visual, false);
                    holder.position = crown.center + new Vector3(Mathf.Cos(angle) * crown.extents.x * 0.8f,
                                                                 (i % 2 == 0 ? -0.15f : 0.25f) * crown.extents.y,
                                                                 Mathf.Sin(angle) * crown.extents.z * 0.8f);
                    holder.localRotation = Quaternion.Euler(0f, i * 67f, 0f);
                    CreateModel(fruit, holder);
                }
            }

            AddRoundTrigger(root, zoneRadius);
            BuilderUtil.SetVector2(root.AddComponent<NavObstacle>(), "size", trunkSize);
            ParticleSystem chips = CreateHitChips(root.transform, Mathf.Min(spec.Size * 0.6f, 2.2f), chipColor ?? new Color(0.45f, 0.75f, 0.25f));

            // Tükenince yerinde kalan parça (kütük / kırık taşlar); başta gizli.
            GameObject remnantGo = null;
            if (remnant != null)
            {
                remnantGo = new GameObject("Remnant");
                remnantGo.transform.SetParent(root.transform, false);
                CreateModel(remnant, remnantGo.transform);
                remnantGo.SetActive(false);
            }

            ResourceNode node = root.AddComponent<ResourceNode>();
            BuilderUtil.SetField(node, "remnant", remnantGo);
            BuilderUtil.SetField(node, "hitEffect", chips);
            BuilderUtil.SetField(node, "config", config);
            BuilderUtil.SetField(node, "nodeType", nodeType);
            BuilderUtil.SetField(node, "visual", visual);
            BuilderUtil.SetFloat(node, "standRadius", standRadius);
            BuilderUtil.SetFloat(node, "spawnHeight", Mathf.Min(spec.Size * 0.5f, 1.6f));
            return Save(root, Folder + "/Nodes/" + spec.Key + ".prefab");
        }

        // Kaynak noktasına vurunca saçılan küçük parçalar (yaprak / taş kırıntısı). Kod Emit ile tetikler.
        private static ParticleSystem CreateHitChips(Transform parent, float height, Color color)
        {
            GameObject go = new GameObject("HitChips");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, height, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Cached("M_Confetti", () => ArtGenerator.GetTransparent("M_Confetti", ArtGenerator.LoadSprite(ArtGenerator.SquarePath)?.texture, Color.white));
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, color * 0.8f);
            main.gravityModifier = 1.2f;
            main.maxParticles = 20;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;
            FadeOut(ps);
            return ps;
        }

        // ================================================================ İşleme binası

        // Bina düzeni (kök = binanın önü ortası; kamera güneyden baktığı için giriş/çıkış önde):
        //            [ halka ]
        //          [ bina gövdesi ]        z +0.9
        //     [giriş]          [çıkış]    z -1.5
        public static GameObject BuildBuilding(ModelSpec spec, BuildingType type, GameConfig config, GameObject workFxPrefab,
                                               ModelSpec decor, Vector3 decorPos)
        {
            Vector3 bodyPos = new Vector3(0f, 0f, 0.9f);
            GameObject root = new GameObject(spec.Key);
            ProcessingBuilding building = root.AddComponent<ProcessingBuilding>();

            Transform body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            body.localPosition = bodyPos;
            CreateModel(spec, body);
            BuilderUtil.SetVector2(body.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(spec.Size * 0.85f, spec.Size * 0.7f));

            if (decor != null)
            {
                Transform d = new GameObject("Decor").transform;
                d.SetParent(root.transform, false);
                d.localPosition = decorPos;
                CreateModel(decor, d);
            }

            Transform spawn = new GameObject("ProductSpawnPoint").transform;
            spawn.SetParent(root.transform, false);
            spawn.localPosition = bodyPos + Vector3.up * 1.2f;

            ProgressRing ring = CreateBuildingRing(root.transform, bodyPos + Vector3.up * (spec.Size + 1.3f)); // adın üstünde
            CreateBoardSign(root.transform, bodyPos + Vector3.up * (spec.Size + 0.55f), type.DisplayName);

            ParticleSystem fx = null;
            if (workFxPrefab != null)
            {
                fx = Instance(workFxPrefab, root.transform, bodyPos + Vector3.up * (spec.Size * 0.7f)).GetComponent<ParticleSystem>();
            }

            ItemPile inputPile = CreateSlot("Input", root.transform, new Vector3(-1.4f, 0f, -1.5f), type.Input, true, config);
            BuildingInput input = inputPile.transform.parent.gameObject.AddComponent<BuildingInput>();
            BuilderUtil.SetField(input, "config", config);
            BuilderUtil.SetField(input, "building", building);

            ItemPile outputPile = CreateSlot("Output", root.transform, new Vector3(1.4f, 0f, -1.5f), type.Output, false, config);
            BuildingOutput output = outputPile.transform.parent.gameObject.AddComponent<BuildingOutput>();
            BuilderUtil.SetField(output, "config", config);
            BuilderUtil.SetField(output, "building", building);

            BuilderUtil.SetField(building, "config", config);
            BuilderUtil.SetField(building, "type", type);
            BuilderUtil.SetField(building, "inputPile", inputPile);
            BuilderUtil.SetField(building, "outputPile", outputPile);
            BuilderUtil.SetField(building, "progressRing", ring);
            BuilderUtil.SetField(building, "productSpawnPoint", spawn);
            BuilderUtil.SetField(building, "bodyVisual", body);
            BuilderUtil.SetField(building, "workEffect", fx);
            return Save(root, Folder + "/Buildings/" + spec.Key + ".prefab");
        }

        // Binanın üstündeki ahşap tabela: yazının boyuna göre tahta, üstünde büyük yazı. Kameraya dönük (Billboard),
        // uzakta yumuşakça kaybolur.
        public static void CreateBoardSign(Transform parent, Vector3 localPos, string title)
        {
            GameObject sign = new GameObject("NameSign");
            sign.transform.SetParent(parent, false);
            sign.transform.localPosition = localPos;
            sign.AddComponent<Billboard>();
            float width = Mathf.Max(1.6f, title.Length * MapLayout.SignLetterWidth + 0.6f);
            Material board = ArtGenerator.GetLit("M_SignWood", null, MapLayout.SignWoodColor);
            Material frame = ArtGenerator.GetLit("M_ShipWood", null, MapLayout.ShipWoodColor);
            Box(sign.transform, "Board", new Vector3(0f, 0f, 0.06f), new Vector3(width, 0.8f, 0.1f), Quaternion.identity, board);
            Box(sign.transform, "FrameTop", new Vector3(0f, 0.42f, 0.04f), new Vector3(width + 0.12f, 0.1f, 0.14f), Quaternion.identity, frame);
            Box(sign.transform, "FrameBottom", new Vector3(0f, -0.42f, 0.04f), new Vector3(width + 0.12f, 0.1f, 0.14f), Quaternion.identity, frame);
            BuilderUtil.SetShadows(sign, false);
            TMPro.TextMeshPro text = UiFactory.CreateWorldText("Title", sign.transform, Vector3.zero, title, 6.5f, Color.white);
            ((RectTransform)text.transform).sizeDelta = new Vector2(width - 0.2f, 0.75f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 3f;
            text.fontSizeMax = 6.5f;
        }

        // Giriş/çıkış alanı: yerde daire + büyük kaynak ikonu + ok (giriş: binaya doğru, çıkış: binadan dışarı);
        // trigger; içinde yerdeki yığın. Yazı yok: ikon her dilde ve uzaktan okunur.
        private static ItemPile CreateSlot(string name, Transform parent, Vector3 localPos, ResourceType resource, bool isInput, GameConfig config)
        {
            GameObject slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            slot.transform.localPosition = localPos;
            AddBoxTrigger(slot, new Vector3(0f, 1f, 0f), new Vector3(1.6f, 2f, 1.6f));

            Canvas canvas = UiFactory.CreateWorldCanvas("Marker", slot.transform, new Vector3(0f, 0.035f, 0f),
                                                        Quaternion.Euler(90f, 0f, 0f), new Vector2(170f, 170f));
            Color tint = isInput ? new Color(0.55f, 0.85f, 1f) : new Color(1f, 0.85f, 0.35f);
            Image back = UiFactory.CreateImage("Back", canvas.transform, ArtGenerator.LoadSprite(ArtGenerator.CirclePath), new Color(1f, 1f, 1f, 0.35f));
            UiFactory.Stretch(back.rectTransform);
            Image ring = UiFactory.CreateImage("Ring", canvas.transform, ArtGenerator.LoadSprite(ArtGenerator.RingPath), tint);
            UiFactory.Stretch(ring.rectTransform);
            Image icon = UiFactory.CreateImage("Icon", canvas.transform, resource != null ? resource.Icon : null, Color.white);
            UiFactory.Size(icon, new Vector2(0f, -12f), new Vector2(104f, 104f));
            icon.preserveAspect = true;
            // Ok: giriş = binaya doğru (yukarı), çıkış = binadan dışarı (aşağı).
            Image arrow = UiFactory.CreateImage("Arrow", canvas.transform, ArtGenerator.LoadSprite(ArtGenerator.ArrowPath), tint);
            UiFactory.Size(arrow, new Vector2(0f, 62f), new Vector2(46f, 46f));
            arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, isInput ? 0f : 180f);

            GameObject pileGo = new GameObject("Pile");
            pileGo.transform.SetParent(slot.transform, false);
            pileGo.transform.localPosition = new Vector3(0f, 0.05f, 0.1f);
            ItemPile pile = pileGo.AddComponent<ItemPile>();
            BuilderUtil.SetField(pile, "config", config);
            return pile;
        }

        // Binanın üstünde kameraya dönük, işlerken dolan halka.
        private static ProgressRing CreateBuildingRing(Transform parent, Vector3 localPos)
        {
            Canvas canvas = UiFactory.CreateWorldCanvas("ProgressRing", parent, localPos, Quaternion.identity, new Vector2(110f, 110f));
            canvas.gameObject.AddComponent<Billboard>();
            GameObject visual = new GameObject("Visual", typeof(RectTransform));
            visual.transform.SetParent(canvas.transform, false);
            UiFactory.Stretch((RectTransform)visual.transform);

            Sprite ringSprite = ArtGenerator.LoadSprite(ArtGenerator.RingPath);
            Image back = UiFactory.CreateImage("Back", visual.transform, ArtGenerator.LoadSprite(ArtGenerator.CirclePath), new Color(0f, 0f, 0f, 0.35f));
            UiFactory.Stretch(back.rectTransform);
            Image fill = UiFactory.CreateImage("Fill", visual.transform, ringSprite, new Color(0.45f, 0.95f, 0.45f));
            UiFactory.Stretch(fill.rectTransform);
            MakeRadial(fill);

            ProgressRing ring = canvas.gameObject.AddComponent<ProgressRing>();
            BuilderUtil.SetField(ring, "fill", fill);
            BuilderUtil.SetBool(ring, "hideWhenEmpty", true);
            BuilderUtil.SetField(ring, "visualRoot", visual);
            return ring;
        }

        private static void MakeRadial(Image image)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 0f;
        }

        // ================================================================ Pazar, açma karesi, kamp

        public static GameObject BuildMarket(GameConfig config)
        {
            GameObject root = new GameObject("Market");
            AddBoxTrigger(root, new Vector3(0f, 1f, 0f), new Vector3(3f, 2f, 2.6f));
            AddGroundDecal("Decal", root.transform, Vector3.zero, new Vector2(3f, 2.6f), new Color(1f, 0.8f, 0.2f, 0.45f));

            Transform stall = new GameObject("Stall").transform;
            stall.SetParent(root.transform, false);
            stall.localPosition = new Vector3(0f, 0f, 2.2f);
            CreateModel(ModelCatalog.MarketStall, stall);
            BuilderUtil.SetVector2(stall.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(3f, 1.6f));

            Transform chest = new GameObject("Chest").transform;
            chest.SetParent(root.transform, false);
            chest.localPosition = new Vector3(1.9f, 0f, 1.6f);
            chest.localRotation = Quaternion.Euler(0f, -20f, 0f);
            CreateModel(ModelCatalog.MarketChest, chest);

            Transform drop = new GameObject("DropPoint").transform;
            drop.SetParent(root.transform, false);
            drop.localPosition = new Vector3(0f, 1.1f, 1.8f);

            UiFactory.CreateWorldLabel("Label", root.transform, new Vector3(0f, 3.9f, 2.2f), "PAZAR", 7f, UiFactory.Gold);

            Market market = root.AddComponent<Market>();
            BuilderUtil.SetField(market, "config", config);
            BuilderUtil.SetField(market, "dropPoint", drop);
            return Save(root, Folder + "/World/Market.prefab");
        }

        // Yerde yuvarlak açma karesi: koyu daire, dolan altın halka, ortada para ikonu + büyük fiyat, üstte ad.
        public static GameObject BuildUnlockPad(GameConfig config)
        {
            GameObject root = new GameObject("UnlockPad");
            AddBoxTrigger(root, new Vector3(0f, 1f, 0f), new Vector3(2.2f, 2f, 2.2f));

            Canvas canvas = UiFactory.CreateWorldCanvas("Canvas", root.transform, new Vector3(0f, 0.04f, 0.3f),
                                                        Quaternion.Euler(90f, 0f, 0f), new Vector2(240f, 330f));
            Transform c = canvas.transform;
            Image back = UiFactory.CreateImage("Back", c, ArtGenerator.LoadSprite(ArtGenerator.CirclePath), new Color(0.05f, 0.08f, 0.12f, 0.45f));
            UiFactory.Size(back, new Vector2(0f, -45f), new Vector2(225f, 225f));
            Image ringBack = UiFactory.CreateImage("RingBack", c, ArtGenerator.LoadSprite(ArtGenerator.RingPath), new Color(1f, 1f, 1f, 0.6f));
            UiFactory.Size(ringBack, new Vector2(0f, -45f), new Vector2(225f, 225f));
            Image fill = UiFactory.CreateImage("RingFill", c, ArtGenerator.LoadSprite(ArtGenerator.RingPath), UiFactory.Gold);
            UiFactory.Size(fill, new Vector2(0f, -45f), new Vector2(225f, 225f));
            MakeRadial(fill);

            // Para ikonu + fiyat yan yana, birlikte ortalı (fiyat büyük ve belirgin).
            Image coin = UiFactory.CreateImage("CoinIcon", c, ArtGenerator.LoadSprite(ArtGenerator.CoinIconPath), Color.white);
            UiFactory.Size(coin, new Vector2(-62f, -45f), new Vector2(78f, 78f));
            TMPro.TextMeshProUGUI price = UiFactory.CreateUIText("Price", c, "100", 86f, Color.white);
            price.alignment = TMPro.TextAlignmentOptions.Left;
            price.enableAutoSizing = true;
            price.fontSizeMin = 50f;
            price.fontSizeMax = 86f;
            UiFactory.Size(price, new Vector2(48f, -45f), new Vector2(140f, 100f));

            ProgressRing ring = canvas.gameObject.AddComponent<ProgressRing>();
            BuilderUtil.SetField(ring, "fill", fill);

            UnlockPad pad = root.AddComponent<UnlockPad>();
            BuilderUtil.SetField(pad, "config", config);
            BuilderUtil.SetField(pad, "priceLabel", price);
            BuilderUtil.SetField(pad, "ring", ring);
            return Save(root, Folder + "/World/UnlockPad.prefab");
        }

        public static GameObject BuildCamp()
        {
            GameObject root = new GameObject("Camp");
            Transform tent = new GameObject("Tent").transform;
            tent.SetParent(root.transform, false);
            tent.localRotation = Quaternion.Euler(0f, 160f, 0f);
            CreateModel(ModelCatalog.Tent, tent);
            BuilderUtil.SetVector2(tent.gameObject.AddComponent<NavObstacle>(), "size", new Vector2(2.4f, 2.2f));

            Transform fire = new GameObject("Campfire").transform;
            fire.SetParent(root.transform, false);
            fire.localPosition = new Vector3(2f, 0f, -0.9f);
            CreateModel(ModelCatalog.Campfire, fire);

            UiFactory.CreateWorldLabel("Label", root.transform, new Vector3(0f, 3.4f, 0f), "KAMP", 5f, Color.white);
            return Save(root, Folder + "/World/Camp.prefab");
        }

        public static GameObject BuildDecor(ModelSpec spec)
        {
            GameObject root = new GameObject(spec.Key);
            CreateModel(spec, root.transform);
            return Save(root, Folder + "/Decor/" + spec.Key + ".prefab");
        }

        // ================================================================ Efektler

        public static GameObject BuildFloatingText()
        {
            GameObject root = new GameObject("FloatingText");
            root.AddComponent<Billboard>();
            TMPro.TextMeshPro text = UiFactory.CreateWorldText("Text", root.transform, Vector3.zero, "+5", 7f, UiFactory.Gold);
            FloatingText floating = root.AddComponent<FloatingText>();
            BuilderUtil.SetField(floating, "label", text);
            root.SetActive(false);
            return Save(root, Folder + "/FX/FloatingText.prefab");
        }

        private static ParticleSystem NewParticles(string name, string materialName, string texturePath, out GameObject root)
        {
            root = new GameObject(name);
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = ArtGenerator.GetTransparent(materialName, ArtGenerator.LoadSprite(texturePath)?.texture, Color.white);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            return ps;
        }

        public static GameObject BuildConfetti()
        {
            ParticleSystem ps = NewParticles("Confetti", "M_Confetti", ArtGenerator.SquarePath, out GameObject root);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 1.1f;
            main.maxParticles = 70;
            Gradient colors = new Gradient();
            colors.SetKeys(new[]
            {
                new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0f), new GradientColorKey(new Color(1f, 0.35f, 0.45f), 0.33f),
                new GradientColorKey(new Color(0.35f, 0.75f, 1f), 0.66f), new GradientColorKey(new Color(0.45f, 0.95f, 0.45f), 1f)
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);

            FadeOut(ps);
            return Save(root, Folder + "/FX/Confetti.prefab");
        }

        public static GameObject BuildDust()
        {
            ParticleSystem ps = NewParticles("Dust", "M_Particle", ArtGenerator.SoftCirclePath, out GameObject root);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.55f);
            main.startColor = new Color(0.92f, 0.82f, 0.66f, 0.55f);
            main.gravityModifier = -0.05f;
            main.maxParticles = 16;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f; // CharacterVisual koşarken açar
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.4f));
            FadeOut(ps);
            return Save(root, Folder + "/FX/Dust.prefab");
        }

        // Bina çalışırken: duman (yukarı süzülen gri bulut) ya da talaş (fışkıran küçük parçalar).
        public static GameObject BuildWorkEffect(string name, bool sawdust)
        {
            ParticleSystem ps = NewParticles(name, sawdust ? "M_Confetti" : "M_Particle",
                                             sawdust ? ArtGenerator.SquarePath : ArtGenerator.SoftCirclePath, out GameObject root);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            ParticleSystem.EmissionModule emission = ps.emission;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            if (sawdust)
            {
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.6f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
                main.startColor = new Color(0.95f, 0.80f, 0.50f, 1f);
                main.gravityModifier = 1f;
                main.maxParticles = 24;
                emission.rateOverTime = 12f;
                shape.angle = 40f;
                shape.radius = 0.4f;
            }
            else
            {
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                main.startColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);
                main.maxParticles = 14;
                emission.rateOverTime = 4f;
                shape.angle = 10f;
                shape.radius = 0.2f;
                ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
            }
            FadeOut(ps);
            return Save(root, Folder + "/FX/" + name + ".prefab");
        }

        private static void FadeOut(ParticleSystem ps)
        {
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
        }
    }
}
