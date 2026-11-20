using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

namespace TargetRange.EditorTools
{
    /// <summary>
    /// Everything that turns raw files (PNG, OBJ, TTF) into configured Unity assets:
    /// import settings, TextMeshPro fonts, the URP pipeline asset, post processing and materials.
    /// Running it twice is safe; existing assets are updated, not duplicated.
    /// </summary>
    public static class AssetPipeline
    {
        public const string Root = "Assets";
        public const string TexDir = "Assets/Art/Textures";
        public const string ModelDir = "Assets/Art/Models";
        public const string MatDir = "Assets/Art/Materials";
        public const string FontDir = "Assets/Fonts";
        public const string SettingsDir = "Assets/Settings";

        // Layer indices (names are written into TagManager by ProjectSetup).
        public const int LayerEnvironment = 8;
        public const int LayerTargets = 9;
        public const int LayerPlayer = 10;

        // ------------------------------------------------------------------ importers
        public static void ConfigureTextures()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { TexDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                string file = Path.GetFileNameWithoutExtension(path);

                if (file.StartsWith("ui_"))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.mipmapEnabled = false;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.spritePixelsPerUnit = 100;
                    if (file == "ui_round" || file == "ui_round_outline") importer.spriteBorder = new Vector4(36, 36, 36, 36);
                    else if (file == "ui_pill") importer.spriteBorder = new Vector4(30, 0, 30, 0);
                    else importer.spriteBorder = Vector4.zero;
                }
                else if (file.EndsWith("_normal"))
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.anisoLevel = 8;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                }
                else
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.sRGBTexture = true;
                    importer.anisoLevel = 8;
                    importer.mipmapEnabled = true;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.maxTextureSize = 2048;
                }
                importer.SaveAndReimport();
            }
        }

        public static void ConfigureModels()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ModelDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.materialImportMode = ModelImporterMaterialImportMode.None; // we assign our own URP materials
                importer.importCameras = false;
                importer.importLights = false;
                importer.addCollider = false;
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import;
                importer.SaveAndReimport();
            }
        }

        public static void ConfigureAudio()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (AudioImporter)AssetImporter.GetAtPath(path);
                var settings = importer.defaultSampleSettings;
                settings.loadType = Path.GetFileNameWithoutExtension(path) == "ambience" ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------ fonts
        public static TMP_FontAsset MakeFont(string ttfName)
        {
            string assetPath = $"{FontDir}/{ttfName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var source = AssetDatabase.LoadAssetAtPath<Font>($"{FontDir}/{ttfName}.ttf");
            if (source == null) throw new FileNotFoundException("Font missing: " + ttfName);

            TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);
            font.name = ttfName + " SDF";
            AssetDatabase.CreateAsset(font, assetPath);
            font.atlasTextures[0].name = ttfName + " Atlas";
            AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            font.material.name = ttfName + " Material";
            AssetDatabase.AddObjectToAsset(font.material, font);

            // Pre-bake the characters the UI uses, so nothing is generated at runtime.
            const string chars = " ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,:;!?+-/|%()[]'\"&@#*=<>_·–—…";
            font.TryAddCharacters(chars, out _);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return font;
        }

        // ------------------------------------------------------------------ URP + post processing
        public static UniversalRenderPipelineAsset CreatePipeline()
        {
            string rendererPath = $"{SettingsDir}/TargetRange_Renderer.asset";
            string pipelinePath = $"{SettingsDir}/TargetRange_URP.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (existing != null)
            {
                Assign(existing);
                return existing;
            }

            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, rendererPath);
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, pipelinePath);

            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 40f;
            pipeline.shadowCascadeCount = 3;
            var so = new SerializedObject(pipeline);
            so.FindProperty("m_SoftShadowsSupported").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            Assign(pipeline);
            return pipeline;
        }

        private static void Assign(UniversalRenderPipelineAsset pipeline)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
                QualitySettings.vSyncCount = 1;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        public static VolumeProfile CreatePostProcessing(string name, float exposure, float saturation, float vignette, float bloom)
        {
            string path = $"{SettingsDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing != null) AssetDatabase.DeleteAsset(path);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tonemap = profile.Add<Tonemapping>(true);
            tonemap.mode.Override(TonemappingMode.ACES);

            var bloomFx = profile.Add<Bloom>(true);
            bloomFx.threshold.Override(1.0f);
            bloomFx.intensity.Override(bloom);
            bloomFx.scatter.Override(0.65f);

            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(vignette);
            vig.smoothness.Override(0.45f);

            var grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(exposure);
            grade.contrast.Override(8f);
            grade.saturation.Override(saturation);

            foreach (VolumeComponent component in profile.components)
            {
                component.name = component.GetType().Name;
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        // ------------------------------------------------------------------ materials
        public static Texture2D Tex(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexDir}/{file}");

        public static Material Lit(string name, string albedo, string normal, Color tint, float smoothness, float metallic = 0f,
            Vector2? tiling = null, float bumpScale = 1f)
        {
            Material m = LoadOrCreate(name);
            m.shader = Shader.Find("Universal Render Pipeline/Lit");
            Vector2 tile = tiling ?? Vector2.one;

            m.SetColor("_BaseColor", tint);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);

            if (!string.IsNullOrEmpty(albedo))
            {
                m.SetTexture("_BaseMap", Tex(albedo));
                m.SetTextureScale("_BaseMap", tile);
            }

            if (!string.IsNullOrEmpty(normal))
            {
                m.SetTexture("_BumpMap", Tex(normal));
                m.SetTextureScale("_BumpMap", tile);
                m.SetFloat("_BumpScale", bumpScale);
                m.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        public static void MakeEmissive(Material m, Color color, float intensity)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(m);
        }

        public static void AddDetail(Material m, string normal, Vector2 tiling, float strength)
        {
            m.SetTexture("_DetailAlbedoMap", Texture2D.grayTexture);
            m.SetTexture("_DetailNormalMap", Tex(normal));
            m.SetTextureScale("_DetailAlbedoMap", tiling);
            m.SetTextureScale("_DetailNormalMap", tiling);
            m.SetFloat("_DetailNormalMapScale", strength);
            m.EnableKeyword("_DETAIL_MULX2");
            EditorUtility.SetDirty(m);
        }

        private static Material LoadOrCreate(string name)
        {
            Directory.CreateDirectory(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        public static Material Unlit(string name, Color color)
        {
            Material m = LoadOrCreate(name);
            m.shader = Shader.Find("Universal Render Pipeline/Unlit");
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Skybox(string name, Color tint, Color ground, float exposure, float sunSize, float thickness)
        {
            Directory.CreateDirectory(MatDir);
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_SkyTint", tint);
            m.SetColor("_GroundColor", ground);
            m.SetFloat("_Exposure", exposure);
            m.SetFloat("_SunSize", sunSize);
            m.SetFloat("_AtmosphereThickness", thickness);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
