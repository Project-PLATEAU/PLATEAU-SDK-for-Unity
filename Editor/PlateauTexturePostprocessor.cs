#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace PLATEAU.CityAdjust.ConvertToAsset
{
    /// <summary>
    /// iOS用テクスチャインポート設定の自動調整。
    /// iPhoneビルド時に DXT5 / BC3 を ASTC に置き換えます。
    /// </summary>
    internal class PlateauTexturePostprocessor : AssetPostprocessor
    {
        private const string IOSPlatformName = "iPhone";

        private void OnPreprocessTexture()
        {
            if (!(assetImporter is TextureImporter importer))
            {
                return;
            }

            ApplyIosSettings(importer);
        }

        private static void ApplyIosSettings(TextureImporter importer)
        {
            // var settings = importer.GetPlatformTextureSettings(IOSPlatformName);
            // settings.name = IOSPlatformName;
            // settings.overridden = true;
            // // settings.format = TextureImporterFormat.ASTC_6x6;
            // settings.format = TextureImporterFormat.ASTC_4x4;
            // settings.maxTextureSize = 1024;
            // settings.textureCompression = TextureImporterCompression.Compressed;
            // settings.compressionQuality = (int)TextureCompressionQuality.Best;
            var settings = new TextureImporterPlatformSettings
            {
                name = IOSPlatformName,
                overridden = true,
                format = TextureImporterFormat.ASTC_4x4,
                maxTextureSize = 1024,
                compressionQuality = (int)TextureCompressionQuality.Best,
                textureCompression = TextureImporterCompression.Compressed
            };
            importer.SetPlatformTextureSettings(settings);
        }

        [MenuItem("PLATEAU/Debug/Apply iOS Texture Settings to All Textures")]
        private static void ApplyIosSettingsToAllTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D");
            var appliedCount = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
                {
                    continue;
                }

                ApplyIosSettings(importer);
                importer.SaveAndReimport();
                appliedCount++;
            }

            Debug.Log($"PLATEAU: Applied iOS texture settings to {appliedCount} textures.");
        }

        [MenuItem("PLATEAU/Debug/Check iOS Texture Formats")]
        private static void CheckTextures()
        {
            var guids = AssetDatabase.FindAssets("t:Texture");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null) continue;

                var settings = importer.GetPlatformTextureSettings("iPhone");

                if (settings.format == TextureImporterFormat.DXT5
                //  || settings.format == TextureImporterFormat.BC3
                 )
                {
                    Debug.Log($"❌ DXT5残ってる: {path}");
                }
                else
                {
                    Debug.Log($"✅ {path} -> {settings.format}");
                }
            }
        }
    }
}

#endif