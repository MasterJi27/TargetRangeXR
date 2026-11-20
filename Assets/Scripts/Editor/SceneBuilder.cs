using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using TargetRange.Core;
using TargetRange.Feedback;
using TargetRange.Weapon;

namespace TargetRange.EditorTools
{
    /// <summary>
    /// Builds the whole playable scene (Assets/Scenes/Range.unity) from the generated assets.
    /// Menu: Target Range > 2. Build Project and Scene
    /// The result is a normal Unity scene: every object can be selected and edited afterwards.
    /// This file holds the entry point, materials, config and prefabs; the other partial files build the hall,
    /// the XR rig, the pistol, the targets and the world space UI.
    /// </summary>
    public static partial class SceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Range.unity";
        private const string ConfigPath = "Assets/Settings/RangeConfig.asset";
        private const string SampleRoot = "Assets/Samples/XR Interaction Toolkit/3.7.0";

        private static TMP_FontAsset titleFont, bodyFont, boldFont;

        [MenuItem("Target Range/2. Build Project and Scene")]
        public static void BuildAll()
        {
            ProjectSetup.EnsureFolders();

            // NewScene unloads unused assets, so the empty scene must exist BEFORE any asset is created and
            // held in a C# variable. Otherwise those references would silently turn into null in the saved scene.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AssetPipeline.ConfigureTextures();
            AssetPipeline.ConfigureModels();
            AssetPipeline.ConfigureAudio();
            AssetDatabase.Refresh();

            AssetPipeline.CreatePipeline();
            VolumeProfile post = AssetPipeline.CreatePostProcessing("PostProcessing_Range", 0.25f, 4f, 0f, 0.3f);

            titleFont = AssetPipeline.MakeFont("BarlowCondensed-SemiBold");
            bodyFont = AssetPipeline.MakeFont("Barlow-Medium");
            boldFont = AssetPipeline.MakeFont("Barlow-Bold");

            Materials mats = BuildMaterials();
            RangeConfig config = BuildConfig();
            Bullet bulletPrefab = BuildBulletPrefab(mats);
            ParticleSystem sparkPrefab = BuildSparkPrefab(mats);

            BuildScene(config, mats, post, bulletPrefab, sparkPrefab);
            AssetDatabase.SaveAssets();
            Debug.Log("TRANGE_BUILD_OK");
        }

        // ------------------------------------------------------------------ materials
        private sealed class Materials
        {
            public Material floor, wallFront, wallSide, wainscot, ceiling, backstop, wood, steel, darkRubber, pedestal;
            public Material stripe, pad, slide, sight, polymer, barrel, brass, targetFace, lightEmissive, lightHousing;
            public Material laser, laserDot, spark, trail, flash, boardStand;
        }

        private static Materials BuildMaterials()
        {
            var m = new Materials();
            m.floor = AssetPipeline.Lit("Floor", "concrete_albedo.png", "concrete_normal.png", new Color(0.88f, 0.88f, 0.9f), 0.32f, 0f, new Vector2(8, 9), 0.8f);
            m.wallFront = AssetPipeline.Lit("WallFront", "plaster_albedo.png", "plaster_normal.png", new Color(0.95f, 0.95f, 0.94f), 0.15f, 0f, new Vector2(5, 2));
            m.wallSide = AssetPipeline.Lit("WallSide", "plaster_albedo.png", "plaster_normal.png", new Color(0.95f, 0.95f, 0.94f), 0.15f, 0f, new Vector2(6, 2));
            m.wainscot = AssetPipeline.Lit("Wainscot", "concrete_albedo.png", "concrete_normal.png", new Color(0.34f, 0.40f, 0.48f), 0.25f, 0f, new Vector2(6, 1));
            m.ceiling = AssetPipeline.Lit("Ceiling", "ceiling_albedo.png", "ceiling_normal.png", new Color(0.93f, 0.93f, 0.95f), 0.1f, 0f, new Vector2(8, 9));
            m.backstop = AssetPipeline.Lit("Backstop", "rubber_albedo.png", "rubber_normal.png", new Color(0.55f, 0.55f, 0.58f), 0.12f, 0f, new Vector2(7, 2));
            m.wood = AssetPipeline.Lit("Wood", "wood_albedo.png", "wood_normal.png", Color.white, 0.38f);
            m.steel = AssetPipeline.Lit("Steel", "slide_albedo.png", "slide_normal.png", new Color(0.62f, 0.64f, 0.67f), 0.62f, 0.85f);
            m.darkRubber = AssetPipeline.Lit("DarkRubber", "rubber_albedo.png", "rubber_normal.png", new Color(0.22f, 0.22f, 0.24f), 0.18f);
            m.pedestal = AssetPipeline.Lit("Pedestal", "concrete_albedo.png", "concrete_normal.png", new Color(0.62f, 0.64f, 0.67f), 0.28f, 0f, Vector2.one);
            m.stripe = AssetPipeline.Lit("SafetyStripe", null, null, new Color(0.98f, 0.74f, 0.22f), 0.35f);
            m.pad = AssetPipeline.Lit("BenchMat", null, "rubber_normal.png", new Color(0.50f, 0.44f, 0.30f), 0.12f, 0f, new Vector2(3, 2));
            m.slide = AssetPipeline.Lit("PistolSlide", "slide_albedo.png", "slide_normal.png", Color.white, 0.68f, 0.9f);
            m.sight = AssetPipeline.Lit("PistolSight", null, null, new Color(0.9f, 0.92f, 0.85f), 0.3f);
            AssetPipeline.MakeEmissive(m.sight, new Color(0.7f, 1f, 0.5f), 0.35f);
            m.polymer = AssetPipeline.Lit("PistolPolymer", "polymer_albedo.png", "polymer_normal.png", Color.white, 0.22f);
            m.barrel = AssetPipeline.Lit("PistolBarrel", null, null, new Color(0.09f, 0.09f, 0.10f), 0.6f, 0.9f);
            m.brass = AssetPipeline.Lit("Brass", null, null, new Color(0.95f, 0.72f, 0.28f), 0.8f, 1f);
            m.targetFace = AssetPipeline.Lit("TargetFace", "target_face.png", null, Color.white, 0.2f);
            m.lightEmissive = AssetPipeline.Lit("LightPanel", null, null, new Color(1f, 0.97f, 0.9f), 0.2f);
            AssetPipeline.MakeEmissive(m.lightEmissive, new Color(1f, 0.96f, 0.88f), 2.2f);
            m.lightHousing = AssetPipeline.Lit("LightHousing", null, null, new Color(0.18f, 0.19f, 0.21f), 0.5f, 0.6f);
            m.boardStand = AssetPipeline.Lit("BoardStand", null, null, new Color(0.16f, 0.17f, 0.19f), 0.5f, 0.7f);

            m.laser = AssetPipeline.Unlit("Laser", new Color(1f, 0.12f, 0.1f));
            m.trail = AssetPipeline.Unlit("BulletTrail", new Color(1f, 0.78f, 0.35f));
            m.laserDot = ParticleMaterial("LaserDot", new Color(1f, 0.2f, 0.15f));
            m.spark = ParticleMaterial("ImpactSpark", Color.white);
            m.flash = ParticleMaterial("MuzzleFlash", Color.white);
            return m;
        }

        private static Material ParticleMaterial(string name, Color tint)
        {
            string path = $"{AssetPipeline.MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Sprites/Default"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.mainTexture = AssetPipeline.Tex("ui_glow.png");
            m.color = tint;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ------------------------------------------------------------------ config
        private static RangeConfig BuildConfig()
        {
            // Always start from the defaults written in RangeConfig, so a rebuild can never keep stale numbers.
            AssetDatabase.DeleteAsset(ConfigPath);
            var config = ScriptableObject.CreateInstance<RangeConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);

            config.bulletMask = (1 << AssetPipeline.LayerEnvironment) | (1 << AssetPipeline.LayerTargets);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        // ------------------------------------------------------------------ prefabs
        private static Bullet BuildBulletPrefab(Materials mats)
        {
            var root = new GameObject("Bullet");
            GameObject model = Model("Bullet", root.transform, new Dictionary<string, Material> { { "Body", mats.brass } }, 0);
            model.transform.localScale = Vector3.one * 2.4f; // a real 9 mm round is too small to see in flight
            foreach (MeshRenderer r in model.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;

            var trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.07f;
            trail.minVertexDistance = 0.02f;
            trail.startWidth = 0.014f;
            trail.endWidth = 0f;
            trail.sharedMaterial = mats.trail;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.alignment = LineAlignment.View;

            var bullet = root.AddComponent<Bullet>();
            Wire.Ref(bullet, "trail", trail);

            string path = "Assets/Prefabs/Bullet.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved.GetComponent<Bullet>();
        }

        private static ParticleSystem BuildSparkPrefab(Materials mats)
        {
            var go = new GameObject("ImpactSpark");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.45f), new Color(1f, 0.55f, 0.2f));
            main.gravityModifier = 1.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.01f;

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = mats.spark;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.2f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            string path = "Assets/Prefabs/ImpactSpark.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved.GetComponent<ParticleSystem>();
        }

        // ------------------------------------------------------------------ shared helpers
        private static Transform Folder(string name) => new GameObject(name).transform;

        private static T Child<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        /// <summary>A cube with a collider on the Environment layer: walls, floor, benches.</summary>
        private static GameObject Block(Transform parent, string name, Vector3 center, Vector3 size, Material material, bool collider = true,
            bool castShadows = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.layer = AssetPipeline.LayerEnvironment;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            go.GetComponent<MeshRenderer>().shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            if (!collider) Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            return go;
        }

        /// <summary>
        /// Builds a model from its part meshes (Assets/Art/Models/Parts/Model__Part.obj). Unity merges the objects of one OBJ
        /// file into a single mesh, so every part is its own file; each part becomes a child with its own material,
        /// which is what lets the pistol's slide and trigger move on their own.
        /// </summary>
        private static GameObject Model(string model, Transform parent, Dictionary<string, Material> byPartName, int layer)
        {
            var root = new GameObject(model) { layer = layer };
            root.transform.SetParent(parent, false);

            foreach (KeyValuePair<string, Material> part in byPartName)
            {
                string path = $"{AssetPipeline.ModelDir}/Parts/{model}__{part.Key}.obj";
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                MeshFilter source = asset != null ? asset.GetComponentInChildren<MeshFilter>() : null;
                if (source == null) throw new System.Exception($"Model part not found or empty: {path}");

                var child = new GameObject(part.Key) { layer = layer };
                child.transform.SetParent(root.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                child.AddComponent<MeshRenderer>().sharedMaterial = part.Value;
            }
            return root;
        }

        /// <summary>Bounds of all renderers under a root, expressed in the root's local space.</summary>
        private static Bounds LocalBounds(GameObject root)
        {
            Bounds bounds = default;
            bool first = true;
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>())
            {
                Bounds b = r.bounds;
                // Transform the world bounds corners into the root's space so a rotated root still gives a tight box.
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 local = root.transform.InverseTransformPoint(corner);
                    if (first) { bounds = new Bounds(local, Vector3.zero); first = false; }
                    else bounds.Encapsulate(local);
                }
            }
            return bounds;
        }

        private static BoxCollider BoxFromRenderers(GameObject root, float shrink = 0f)
        {
            Bounds b = LocalBounds(root);
            var box = root.AddComponent<BoxCollider>();
            box.center = b.center;
            box.size = b.size - Vector3.one * shrink;
            return box;
        }
    }
}
