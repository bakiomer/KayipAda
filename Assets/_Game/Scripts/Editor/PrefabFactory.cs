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
            }

            model.name = "Model";
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            Normalize(model, spec);
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
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.name = name;
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPos + Vector3.up * 0.025f;
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            Renderer r = quad.GetComponent<Renderer>();
            string key = "M_Decal_" + ColorUtility.ToHtmlStringRGBA(color);
            r.sharedMaterial = Cached(key, () => ArtGenerator.GetTransparent(key, ArtGenerator.LoadSprite(ArtGenerator.RoundedRectPath)?.texture, color));
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

        // Son gecenin iskelet korsanı: görünüş + Enemy bileşeni (yürür, saldırır, ölür).
        public static GameObject BuildSkeletonEnemy(ModelSpec spec, RuntimeAnimatorController controller)
        {
            GameObject root = new GameObject(spec.Key);
            Transform body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            GameObject model = CreateModel(spec, body);
            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            AddBlobShadow(root.transform, 1.2f);

            Enemy enemy = root.AddComponent<Enemy>();
            BuilderUtil.SetField(enemy, "animator", animator);
            BuilderUtil.SetField(enemy, "body", body);
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

            TMPro.TextMeshPro label = UiFactory.CreateWorldText("Label", root.transform, new Vector3(0f, 3f, 1.6f), "GELİŞTİR", 5f,
                                                                new Color(0.55f, 0.9f, 1f));
            label.gameObject.AddComponent<Billboard>();

            UpgradeStation station = root.AddComponent<UpgradeStation>();
            BuilderUtil.SetField(station, "config", config);
            BuilderUtil.SetField(station, "effectPoint", fx);
            return Save(root, Folder + "/World/UpgradeStation.prefab");
        }

        // Savunma topu: dönen top modeli + önünde taş doldurma alanı + üstte gülle göstergesi.
        public static GameObject BuildCannon(ShipConfig ship, GameConfig config, GameObject ballPrefab)
        {
            GameObject root = new GameObject("CannonTower");
            AddGroundDecal("Base", root.transform, Vector3.zero, new Vector2(2.2f, 2.2f), new Color(0.3f, 0.25f, 0.2f, 0.35f));

            Transform barrel = new GameObject("Barrel").transform;
            barrel.SetParent(root.transform, false);
            CreateModel(FindDecor("Cannon"), barrel);
            BuilderUtil.SetVector2(root.AddComponent<NavObstacle>(), "size", new Vector2(1.4f, 1.4f));

            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(barrel, false);
            muzzle.localPosition = new Vector3(0f, 0.9f, 0.9f);

            TMPro.TextMeshPro ammo = UiFactory.CreateWorldText("Ammo", root.transform, new Vector3(0f, 2.4f, 0f), "Gülle 0/10", 4f, Color.white);
            ammo.gameObject.AddComponent<Billboard>();

            Cannon cannon = root.AddComponent<Cannon>();
            BuilderUtil.SetField(cannon, "ship", ship);
            BuilderUtil.SetField(cannon, "barrel", barrel);
            BuilderUtil.SetField(cannon, "muzzle", muzzle);
            BuilderUtil.SetField(cannon, "ballPrefab", ballPrefab);
            BuilderUtil.SetField(cannon, "ammoLabel", ammo);

            // Doldurma alanı: topun önünde (kameraya bakan tarafta).
            GameObject loader = new GameObject("Loader");
            loader.transform.SetParent(root.transform, false);
            loader.transform.localPosition = new Vector3(0f, 0f, -1.9f);
            AddRoundTrigger(loader, 1.1f);
            AddGroundDecal("Decal", loader.transform, Vector3.zero, new Vector2(1.6f, 1.6f), new Color(0.75f, 0.75f, 0.8f, 0.5f));
            TMPro.TextMeshPro text = UiFactory.CreateWorldText("Label", loader.transform, new Vector3(0f, 0.05f, -1.05f), "Taş → Gülle", 2.4f, Color.white);
            text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CannonLoader cl = loader.AddComponent<CannonLoader>();
            BuilderUtil.SetField(cl, "config", config);
            BuilderUtil.SetField(cl, "ship", ship);
            BuilderUtil.SetField(cl, "cannon", cannon);
            return Save(root, Folder + "/World/CannonTower.prefab");
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

        // Gülle patlaması: turuncu parlama + gri duman.
        public static GameObject BuildExplosion()
        {
            ParticleSystem ps = NewParticles("Explosion", "M_Particle", ArtGenerator.SoftCirclePath, out GameObject root);
            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.15f), new Color(0.55f, 0.5f, 0.45f, 0.8f));
            main.gravityModifier = -0.1f;
            main.maxParticles = 40;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) });
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 1.6f));
            FadeOut(ps);
            return Save(root, Folder + "/FX/Explosion.prefab");
        }

        // ================================================================ Kaynak noktaları

        public static GameObject BuildNode(ModelSpec spec, ResourceNodeType nodeType, GameConfig config,
                                           float zoneRadius, Vector2 trunkSize, float standRadius,
                                           ModelSpec fruit = null, int fruitCount = 0, Color? chipColor = null)
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

            ResourceNode node = root.AddComponent<ResourceNode>();
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
            TMPro.TextMeshPro title = UiFactory.CreateWorldText("Title", root.transform, bodyPos + Vector3.up * (spec.Size + 0.2f),
                                                                 type.DisplayName, 4.5f, Color.white);
            title.gameObject.AddComponent<Billboard>();

            ParticleSystem fx = null;
            if (workFxPrefab != null)
            {
                fx = Instance(workFxPrefab, root.transform, bodyPos + Vector3.up * (spec.Size * 0.7f)).GetComponent<ParticleSystem>();
            }

            ItemPile inputPile = CreateSlot("Input", root.transform, new Vector3(-1.4f, 0f, -1.5f), "Giriş: " + type.Input.DisplayName, config);
            BuildingInput input = inputPile.transform.parent.gameObject.AddComponent<BuildingInput>();
            BuilderUtil.SetField(input, "config", config);
            BuilderUtil.SetField(input, "building", building);

            ItemPile outputPile = CreateSlot("Output", root.transform, new Vector3(1.4f, 0f, -1.5f), "Çıkış: " + type.Output.DisplayName, config);
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

        // Giriş/çıkış alanı: yerde yarı saydam kare + yazı + trigger; içinde yerdeki yığın.
        private static ItemPile CreateSlot(string name, Transform parent, Vector3 localPos, string label, GameConfig config)
        {
            GameObject slot = new GameObject(name);
            slot.transform.SetParent(parent, false);
            slot.transform.localPosition = localPos;
            AddGroundDecal("Decal", slot.transform, Vector3.zero, new Vector2(1.6f, 1.6f), new Color(1f, 1f, 1f, 0.45f));
            AddBoxTrigger(slot, new Vector3(0f, 1f, 0f), new Vector3(1.6f, 2f, 1.6f));

            TMPro.TextMeshPro text = UiFactory.CreateWorldText("Label", slot.transform, new Vector3(0f, 0.05f, -1.05f), label, 2.4f, Color.white);
            text.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

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

            TMPro.TextMeshPro label = UiFactory.CreateWorldText("Label", root.transform, new Vector3(0f, 3.9f, 2.2f), "PAZAR", 7f, UiFactory.Gold);
            label.gameObject.AddComponent<Billboard>();

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

            Image coin = UiFactory.CreateImage("CoinIcon", c, ArtGenerator.LoadSprite(ArtGenerator.CoinIconPath), Color.white);
            UiFactory.Size(coin, new Vector2(-52f, -45f), new Vector2(50f, 50f));
            TMPro.TextMeshProUGUI price = UiFactory.CreateUIText("Price", c, "100", 60f, Color.white);
            UiFactory.Size(price, new Vector2(20f, -45f), new Vector2(150f, 80f));
            TMPro.TextMeshProUGUI title = UiFactory.CreateUIText("Name", c, "Alan", 40f, Color.white);
            UiFactory.Size(title, new Vector2(0f, 125f), new Vector2(400f, 60f));

            ProgressRing ring = canvas.gameObject.AddComponent<ProgressRing>();
            BuilderUtil.SetField(ring, "fill", fill);

            UnlockPad pad = root.AddComponent<UnlockPad>();
            BuilderUtil.SetField(pad, "config", config);
            BuilderUtil.SetField(pad, "nameLabel", title);
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

            TMPro.TextMeshPro label = UiFactory.CreateWorldText("Label", root.transform, new Vector3(0f, 3.4f, 0f), "KAMP", 5f, Color.white);
            label.gameObject.AddComponent<Billboard>();
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
