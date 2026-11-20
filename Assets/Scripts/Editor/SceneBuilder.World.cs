using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TargetRange.Core;
using TargetRange.Feedback;
using TargetRange.Targets;
using TargetRange.Weapon;

namespace TargetRange.EditorTools
{
    /// <summary>The 3D world: lighting, the hall, the XR rig, the shooting bench with the pistol, and the targets.</summary>
    public static partial class SceneBuilder
    {
        // Hall: x from -8 to 8, z from -3 (behind the player) to 16 (the backstop), 5 m high.
        private const float HallHalfWidth = 8f, HallBack = -3f, HallFront = 16f, HallHeight = 5f;

        private static void BuildScene(RangeConfig config, Materials m, VolumeProfile post, Bullet bulletPrefab, ParticleSystem sparkPrefab)
        {
            Transform environment = Folder("Environment");
            Transform lighting = Folder("Lighting");
            Transform bench = Folder("Shooting Bench");
            Transform targetsRoot = Folder("Targets");
            Transform systems = Folder("Game Systems");

            // --- systems first, because everything else wires itself to them
            AudioService audio = BuildAudio(systems);
            var game = Child<GameManager>(systems, "Game Manager");
            Wire.Ref(game, "config", config);

            var bulletPool = Child<BulletPool>(systems, "Bullet Pool");
            Wire.Ref(bulletPool, "prefab", bulletPrefab);
            Wire.Ref(bulletPool, "config", config);

            var effects = Child<EffectPool>(systems, "Impact Effects");
            Wire.Ref(effects, "prefab", sparkPrefab);

            // --- the world
            BuildLighting(lighting, post);
            BuildHall(environment, m);
            Camera headCamera = BuildXrRig();
            Pistol pistol = BuildBench(bench, m, config, game, bulletPool);
            BuildTargets(targetsRoot, m, game);

            var feedback = Child<FeedbackController>(systems, "Feedback Controller");
            Wire.Ref(feedback, "game", game);
            Wire.Ref(feedback, "pistol", pistol);
            Wire.Ref(feedback, "bulletPool", bulletPool);
            Wire.Ref(feedback, "audioService", audio);
            Wire.Ref(feedback, "impactEffects", effects);

            var bridge = Child<XRDesktopBridge>(systems, "Desktop Controls");
            Wire.Ref(bridge, "game", game);
            Wire.Ref(bridge, "pistol", pistol);
            Wire.Ref(bridge, "headCamera", headCamera);
            Wire.Ref(bridge, "xrOrigin", headCamera.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>().transform);
            Wire.Ref(bridge, "simulatorRoot", UnityEngine.Object.FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>().gameObject);
            BuildUi(game, audio, headCamera);
            var desktopUi = Child<TargetRange.UI.DesktopView>(systems, "Desktop UI");
            Wire.Ref(desktopUi, "game", game);
            Wire.Ref(desktopUi, "bridge", bridge);
            Wire.Ref(desktopUi, "font", bodyFont);

            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ------------------------------------------------------------------ audio
        private static AudioService BuildAudio(Transform parent)
        {
            var audio = Child<AudioService>(parent, "Audio Service");
            var so = new SerializedObject(audio);
            SerializedProperty list = so.FindProperty("effects");
            var map = new (SfxId id, string file, float volume, float lo, float hi)[]
            {
                (SfxId.UiClick, "ui_click", 0.7f, 1f, 1f), (SfxId.UiHover, "ui_hover", 0.35f, 0.95f, 1.05f),
                (SfxId.Shot, "pistol_shot", 0.9f, 0.96f, 1.04f), (SfxId.DryFire, "dry_fire", 0.6f, 0.95f, 1.05f),
                (SfxId.Hit, "target_hit", 0.85f, 0.9f, 1.12f), (SfxId.Grab, "grab", 0.7f, 0.95f, 1.05f),
                (SfxId.GameStart, "game_start", 0.8f, 1f, 1f), (SfxId.GameEnd, "game_end", 0.85f, 1f, 1f),
                (SfxId.Tick, "tick", 0.5f, 1f, 1f), (SfxId.Bullseye, "bullseye", 0.95f, 1f, 1f),
            };
            list.arraySize = map.Length;
            for (int i = 0; i < map.Length; i++)
            {
                SerializedProperty e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)map[i].id;
                e.FindPropertyRelative("clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{map[i].file}.wav");
                e.FindPropertyRelative("volume").floatValue = map[i].volume;
                e.FindPropertyRelative("pitchRange").vector2Value = new Vector2(map[i].lo, map[i].hi);
            }
            so.FindProperty("ambience").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/ambience.wav");
            so.ApplyModifiedPropertiesWithoutUndo();
            return audio;
        }

        // ------------------------------------------------------------------ lighting
        private static void BuildLighting(Transform parent, VolumeProfile post)
        {
            // Skylight: a soft directional light. The ceiling does not cast shadows, so the light "enters the hall".
            var sun = new GameObject("Skylight").AddComponent<Light>();
            sun.transform.SetParent(parent, false);
            sun.transform.rotation = Quaternion.Euler(58f, -24f, 0f);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.9f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;

            // Pools of light from the ceiling fixtures (no shadows, they are cheap fill lights).
            foreach (float z in new[] { 2.5f, 8f, 13f })
            {
                foreach (float x in new[] { -4f, 4f })
                {
                    var l = new GameObject($"Ceiling Light {x}/{z}").AddComponent<Light>();
                    l.transform.SetParent(parent, false);
                    l.transform.position = new Vector3(x, HallHeight - 0.4f, z);
                    l.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    l.type = LightType.Spot;
                    l.spotAngle = 110f;
                    l.innerSpotAngle = 60f;
                    l.range = 11f;
                    l.intensity = 4.2f;
                    l.color = new Color(1f, 0.95f, 0.88f);
                    l.shadows = LightShadows.None;
                }
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.70f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.52f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.28f, 0.29f);
            RenderSettings.skybox = AssetPipeline.Skybox("RangeSky", new Color(0.4f, 0.45f, 0.5f), new Color(0.2f, 0.2f, 0.2f), 0.7f, 0.04f, 1f);

            var volumeObject = new GameObject("Post Processing");
            volumeObject.transform.SetParent(parent, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = post;
        }

        // ------------------------------------------------------------------ the hall
        private static void BuildHall(Transform parent, Materials m)
        {
            float depth = HallFront - HallBack;
            float centerZ = (HallFront + HallBack) * 0.5f;
            float width = HallHalfWidth * 2f;

            Block(parent, "Floor", new Vector3(0f, -0.1f, centerZ), new Vector3(width, 0.2f, depth), m.floor);
            Block(parent, "Back Wall", new Vector3(0f, HallHeight / 2f, HallBack - 0.15f), new Vector3(width + 0.6f, HallHeight, 0.3f), m.wallFront);
            Block(parent, "Left Wall", new Vector3(-HallHalfWidth - 0.15f, HallHeight / 2f, centerZ), new Vector3(0.3f, HallHeight, depth), m.wallSide);
            Block(parent, "Right Wall", new Vector3(HallHalfWidth + 0.15f, HallHeight / 2f, centerZ), new Vector3(0.3f, HallHeight, depth), m.wallSide);
            Block(parent, "Front Wall", new Vector3(0f, HallHeight / 2f, HallFront + 0.15f), new Vector3(width + 0.6f, HallHeight, 0.3f), m.wallFront);
            Block(parent, "Ceiling", new Vector3(0f, HallHeight + 0.15f, centerZ), new Vector3(width + 0.6f, 0.3f, depth + 0.6f), m.ceiling, collider: false, castShadows: false);

            // Dark concrete skirting along the walls gives the plaster a base and hides the hard corner.
            Block(parent, "Skirting Left", new Vector3(-HallHalfWidth + 0.04f, 0.55f, centerZ), new Vector3(0.08f, 1.1f, depth), m.wainscot);
            Block(parent, "Skirting Right", new Vector3(HallHalfWidth - 0.04f, 0.55f, centerZ), new Vector3(0.08f, 1.1f, depth), m.wainscot);
            Block(parent, "Skirting Back", new Vector3(0f, 0.55f, HallBack + 0.04f), new Vector3(width, 1.1f, 0.08f), m.wainscot);

            // A thin amber line around the room at the top of the skirting: it ties the hall to the UI colour.
            Block(parent, "Stripe Left", new Vector3(-HallHalfWidth + 0.045f, 1.13f, centerZ), new Vector3(0.09f, 0.05f, depth), m.stripe, collider: false, castShadows: false);
            Block(parent, "Stripe Right", new Vector3(HallHalfWidth - 0.045f, 1.13f, centerZ), new Vector3(0.09f, 0.05f, depth), m.stripe, collider: false, castShadows: false);
            Block(parent, "Stripe Back", new Vector3(0f, 1.13f, HallBack + 0.045f), new Vector3(width, 0.05f, 0.09f), m.stripe, collider: false, castShadows: false);

            // Rubber backstop in front of the far wall with the big scoreboard above it.
            Block(parent, "Backstop", new Vector3(0f, 1.5f, HallFront - 0.25f), new Vector3(width - 1.2f, 3f, 0.5f), m.backstop);
            Block(parent, "Backstop Frame", new Vector3(0f, 3.05f, HallFront - 0.25f), new Vector3(width - 1.2f, 0.1f, 0.54f), m.steel);

            // Painted floor markings: the firing line and the rails of the moving targets.
            Block(parent, "Firing Line", new Vector3(0f, 0.004f, 1.55f), new Vector3(7f, 0.01f, 0.08f), m.stripe, collider: false, castShadows: false);
            foreach (float x in new[] { -3f, 0f, 3f })
                Block(parent, $"Rail {x}", new Vector3(x, 0.01f, 10f), new Vector3(3.8f, 0.02f, 0.14f), m.steel, collider: false);

            // Tactical shooting booth partitions on both sides of the bench
            Block(parent, "Booth Divider Left", new Vector3(-1.35f, 1.25f, 0.75f), new Vector3(0.04f, 2.2f, 1.6f), m.darkRubber, castShadows: true);
            Block(parent, "Booth Divider Right", new Vector3(1.35f, 1.25f, 0.75f), new Vector3(0.04f, 2.2f, 1.6f), m.darkRubber, castShadows: true);

            // Floor distance markers
            Block(parent, "Distance Mark 7M", new Vector3(-3.8f, 0.005f, 7.0f), new Vector3(1.4f, 0.01f, 0.06f), m.stripe, collider: false, castShadows: false);
            Block(parent, "Distance Mark 10M", new Vector3(-3.8f, 0.005f, 10.0f), new Vector3(1.4f, 0.01f, 0.06f), m.stripe, collider: false, castShadows: false);
            Block(parent, "Distance Mark 13M", new Vector3(-3.8f, 0.005f, 13.2f), new Vector3(1.4f, 0.01f, 0.06f), m.stripe, collider: false, castShadows: false);

            // Ceiling light fixtures (the visible emissive strips; the real light comes from the spot lights).
            foreach (float z in new[] { 2.5f, 8f, 13f })
            {
                foreach (float x in new[] { -4f, 4f })
                {
                    GameObject fixture = Model("CeilingLight", parent, new Dictionary<string, Material> { { "Housing", m.lightHousing }, { "Diffuser", m.lightEmissive } }, 0);
                    fixture.name = $"Light Fixture {x}/{z}";
                    fixture.transform.SetPositionAndRotation(new Vector3(x, HallHeight, z), Quaternion.Euler(180f, 0f, 0f));
                    foreach (MeshRenderer r in fixture.GetComponentsInChildren<MeshRenderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        // ------------------------------------------------------------------ XR rig
        private static Camera BuildXrRig()
        {
            var manager = new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            manager.transform.SetParent(null);

            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SampleRoot}/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rig.name = "XR Origin (XR Rig)";
            rig.transform.position = new Vector3(0f, 0f, -0.2f);

            Camera head = rig.GetComponentInChildren<Camera>();
            UniversalAdditionalCameraData data = head.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            head.nearClipPlane = 0.05f;
            head.farClipPlane = 60f;

            // Needed for UI: the event system with the XR UI input module (controller rays can press world space buttons).
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            var uiModule = eventSystemObject.AddComponent<XRUIInputModule>();
            var preset = AssetDatabase.LoadAssetAtPath<Preset>($"{SampleRoot}/Starter Assets/Presets/XRI Default XR UI Input Module.preset");
            if (preset != null) preset.ApplyTo(uiModule);
            else Debug.LogWarning("XR UI Input Module preset not found: UI buttons will not react to the controller ray.");

            // The XR Device Simulator lets the whole game be played with keyboard and mouse (no headset needed).
            var simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SampleRoot}/XR Device Simulator/XR Device Simulator.prefab");
            PrefabUtility.InstantiatePrefab(simulatorPrefab);

            return head;
        }

        // ------------------------------------------------------------------ shooting bench and pistol
        private static Pistol BuildBench(Transform parent, Materials m, RangeConfig config, GameManager game, BulletPool bulletPool)
        {
            const float tableZ = 0.85f;
            GameObject table = Model("Table", parent, new Dictionary<string, Material> { { "Top", m.wood }, { "Legs", m.steel }, { "Shelf", m.wood } }, AssetPipeline.LayerEnvironment);
            table.transform.position = new Vector3(0f, 0f, tableZ);
            BoxFromRenderers(table);

            Block(parent, "Gun Mat", new Vector3(0.08f, 0.9205f, tableZ - 0.05f), new Vector3(0.46f, 0.011f, 0.24f), m.pad, collider: false);

            // Boards on little stands: instructions on the left, the close scoreboard on the right.
            foreach (float x in new[] { -0.46f, 0.50f })
                Block(parent, "Board Stand", new Vector3(x, 1.02f, tableZ + 0.24f), new Vector3(0.03f, 0.22f, 0.03f), m.boardStand, collider: false);

            return BuildPistol(parent, m, config, game, bulletPool, new Vector3(0.08f, 0.9515f, tableZ - 0.05f));
        }

        private static Pistol BuildPistol(Transform parent, Materials m, RangeConfig config, GameManager game, BulletPool bulletPool, Vector3 position)
        {
            GameObject pistolObject = Model("Pistol", parent, new Dictionary<string, Material>
            {
                { "Slide", m.slide }, { "Serrations", m.slide }, { "Sights", m.sight }, { "EjectionPort", m.barrel },
                { "Frame", m.polymer }, { "TriggerGuard", m.polymer }, { "Trigger", m.steel }, { "MagazineBase", m.steel }, { "Barrel", m.barrel }
            }, 0);
            pistolObject.name = "Pistol";
            pistolObject.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 10f, 90f)); // lying on its side on the mat

            BoxFromRenderers(pistolObject, 0.004f);

            var body = pistolObject.AddComponent<Rigidbody>();
            body.mass = 0.9f;
            body.isKinematic = true; // sits still on the table until it is picked up
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var gripAttach = new GameObject("Grip Attach").transform;
            gripAttach.SetParent(pistolObject.transform, false);
            gripAttach.localPosition = new Vector3(0f, -0.035f, -0.035f);
            gripAttach.localRotation = Quaternion.identity;

            var grab = pistolObject.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grab.attachTransform = gripAttach;
            grab.useDynamicAttach = false;              // snap precisely to grip attach, never tilt arbitrarily
            grab.throwOnDetach = true;
            grab.forceGravityOnDetach = true;           // dropped pistol falls even though it started kinematic
            grab.farAttachMode = InteractableFarAttachMode.DeferToInteractor; // allows far ray grab cleanly

            Transform muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(pistolObject.transform, false);
            muzzle.localPosition = new Vector3(0f, 0.052f, 0.132f);

            ParticleSystem flash = BuildMuzzleFlash(muzzle, m);

            var flashLight = new GameObject("Flash Light").AddComponent<Light>();
            flashLight.transform.SetParent(muzzle, false);
            flashLight.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            flashLight.type = LightType.Point;
            flashLight.color = new Color(1f, 0.8f, 0.45f);
            flashLight.intensity = 6f;
            flashLight.range = 3.5f;
            flashLight.shadows = LightShadows.None;
            flashLight.enabled = false;

            LaserSight laser = BuildLaser(pistolObject.transform, muzzle, m, config);

            var pistol = pistolObject.AddComponent<Pistol>();
            Wire.Ref(pistol, "config", config);
            Wire.Ref(pistol, "game", game);
            Wire.Ref(pistol, "bulletPool", bulletPool);
            Wire.Ref(pistol, "muzzle", muzzle);
            Wire.Ref(pistol, "muzzleFlash", flash);
            Wire.Ref(pistol, "flashLight", flashLight);
            Wire.Ref(pistol, "laser", laser);
            Wire.Ref(pistol, "triggerPart", Part(pistolObject.transform, "Trigger"));
            Wire.Refs(pistol, "slideParts", new Object[]
            {
                Part(pistolObject.transform, "Slide"), Part(pistolObject.transform, "Serrations"), Part(pistolObject.transform, "Sights"),
                Part(pistolObject.transform, "EjectionPort")
            });
            return pistol;
        }

        private static ParticleSystem BuildMuzzleFlash(Transform muzzle, Materials m)
        {
            var go = new GameObject("Muzzle Flash");
            go.transform.SetParent(muzzle, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.7f), new Color(1f, 0.6f, 0.2f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 9) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 14f;
            shape.radius = 0.004f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = m.flash;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return ps;
        }

        private static LaserSight BuildLaser(Transform pistol, Transform muzzle, Materials m, RangeConfig config)
        {
            var laserObject = new GameObject("Laser Sight");
            laserObject.transform.SetParent(pistol, false);
            var line = laserObject.AddComponent<LineRenderer>();
            line.sharedMaterial = m.laser;
            line.widthMultiplier = 0.004f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;

            // The dot is a small glowing quad that is moved to wherever the laser lands.
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dot.name = "Laser Dot";
            Object.DestroyImmediate(dot.GetComponent<MeshCollider>());
            dot.transform.SetParent(laserObject.transform, false);
            dot.transform.localScale = Vector3.one * 0.05f;
            var dotRenderer = dot.GetComponent<MeshRenderer>();
            dotRenderer.sharedMaterial = m.laserDot;
            dotRenderer.shadowCastingMode = ShadowCastingMode.Off;
            dotRenderer.receiveShadows = false;

            var laser = laserObject.AddComponent<LaserSight>();
            Wire.Ref(laser, "config", config);
            Wire.Ref(laser, "muzzle", muzzle);
            Wire.Ref(laser, "dot", dot.transform);
            return laser;
        }


        /// <summary>Finds a named part of an imported model anywhere below root. Throws with the real hierarchy if it is missing.</summary>
        private static Transform Part(Transform root, string partName)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == partName) return t;

            var names = new System.Text.StringBuilder();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) names.Append(t.name).Append(", ");
            throw new System.Exception($"Part '{partName}' not found under '{root.name}'. Found: {names}");
        }

        // ------------------------------------------------------------------ targets
        private static void BuildTargets(Transform parent, Materials m, GameManager game)
        {
            const float lowHeight = 0.95f, midHeight = 1.30f, highHeight = 1.65f;

            // Near row: five static targets on pedestals of different heights (10 points each).
            float[] nearX = { -3.0f, -1.5f, 0f, 1.5f, 3.0f };
            string[] nearKind = { "Mid", "Low", "High", "Low", "Mid" };
            float[] nearHeight = { midHeight, lowHeight, highHeight, lowHeight, midHeight };
            for (int i = 0; i < nearX.Length; i++)
                PlaceTarget(parent, m, game, $"Near Target {i + 1}", nearKind[i], nearHeight[i], new Vector3(nearX[i], 0f, 7f), 10, null);

            // Middle row: three targets sliding left and right on rails (20 points each).
            float[] railX = { -3f, 0f, 3f };
            float[] periods = { 4.6f, 5.6f, 6.8f };
            float[] phases = { 0f, 0.35f, 0.7f };
            for (int i = 0; i < railX.Length; i++)
            {
                var mover = new MoverSettings { axis = Vector3.right, amplitude = 1.5f, period = periods[i], phase = phases[i] };
                PlaceTarget(parent, m, game, $"Moving Target {i + 1}", "Low", lowHeight, new Vector3(railX[i], 0f, 10f), 20, mover);
            }

            // Far row: two high targets close to the backstop (30 points each).
            PlaceTarget(parent, m, game, "Far Target 1", "High", highHeight, new Vector3(-2.2f, 0f, 13.2f), 30, null);
            PlaceTarget(parent, m, game, "Far Target 2", "High", highHeight, new Vector3(2.2f, 0f, 13.2f), 30, null);
        }

        private struct MoverSettings
        {
            public Vector3 axis;
            public float amplitude, period, phase;
        }

        private static void PlaceTarget(Transform parent, Materials m, GameManager game, string name, string pedestalKind, float pedestalHeight,
            Vector3 position, int points, MoverSettings? moving)
        {
            GameObject pedestal = Model($"Pedestal_{pedestalKind}", parent, new Dictionary<string, Material> { { "Body", m.pedestal } }, AssetPipeline.LayerEnvironment);
            pedestal.name = name + " Platform";
            pedestal.transform.position = position;
            BoxFromRenderers(pedestal);

            GameObject target = Model("Target", pedestal.transform, new Dictionary<string, Material>
            {
                { "Face", m.targetFace }, { "Rim", m.steel }, { "Stand", m.darkRubber }
            }, AssetPipeline.LayerTargets);
            target.name = name;
            target.transform.localPosition = new Vector3(0f, pedestalHeight, 0f);

            // Colliders: the face is a convex mesh (the real disc shape, so hits near the corner do not count), the stand a box.
            Transform face = Part(target.transform, "Face");
            var faceCollider = face.gameObject.AddComponent<MeshCollider>();
            faceCollider.sharedMesh = face.GetComponent<MeshFilter>().sharedMesh;
            faceCollider.convex = true;
            BoxFromRenderers(Part(target.transform, "Stand").gameObject);

            var body = target.AddComponent<Rigidbody>();
            body.mass = 1.5f;
            body.linearDamping = 0.05f;
            body.angularDamping = 0.08f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.isKinematic = true;

            var targetScript = target.AddComponent<Target>();
            Wire.Ref(targetScript, "game", game);
            Wire.Int(targetScript, "points", points);
            Wire.Ref(targetScript, "faceTransform", face);

            GameObject lightBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lightBar.name = "Status LED";
            lightBar.transform.SetParent(pedestal.transform, false);
            lightBar.transform.localPosition = new Vector3(0f, pedestalHeight - 0.08f, 0.22f);
            lightBar.transform.localScale = new Vector3(0.38f, 0.04f, 0.04f);
            Object.DestroyImmediate(lightBar.GetComponent<Collider>());
            var pedLight = lightBar.AddComponent<TargetPedestalLight>();
            Wire.Ref(pedLight, "lightRenderer", lightBar.GetComponent<MeshRenderer>());
            Wire.Ref(targetScript, "pedestalLight", pedLight);

            if (!moving.HasValue) return;

            var platformBody = pedestal.AddComponent<Rigidbody>();
            platformBody.isKinematic = true;

            var mover = pedestal.AddComponent<PlatformMover>();
            Wire.Vec3(mover, "axis", moving.Value.axis);
            Wire.Float(mover, "amplitude", moving.Value.amplitude);
            Wire.Float(mover, "periodSeconds", moving.Value.period);
            Wire.Float(mover, "phase", moving.Value.phase);
            Wire.Ref(targetScript, "mover", mover);
        }
    }
}
