using UnityEngine;
using UnityEditor;
using System.IO;

namespace ARRC.ARRCTexture
{
    public static class TextureIO_Editor
    {
        public static Texture2D LoadTextureUsingAssetDataBase(string assetPath, int size)
        { 
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);

            if (importer == null)
                throw new FileNotFoundException(string.Format("The asset is not found. : {0}", assetPath));

            if (!
                (importer.textureType == TextureImporterType.Default &&
                importer.isReadable == true &&
                importer.mipmapEnabled == false &&
                importer.wrapMode == TextureWrapMode.Clamp &&
                importer.maxTextureSize == size &&
                importer.textureCompression == TextureImporterCompression.Uncompressed &&
                importer.filterMode == FilterMode.Point
                ))

            {
                importer.textureType = TextureImporterType.Default;
                importer.isReadable = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = size;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                //importer.textureFormat = TextureImporterFormat.RGBAFloat;
                AssetDatabase.ImportAsset(assetPath);
            }

            //Texture2D texture = (Texture2D)UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
            Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
            return texture;
        }
    }
}