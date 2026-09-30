using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupCargoGridScene : EditorWindow
{
    [MenuItem("Tools/Setup Cargo Grid Scene")]
    public static void CreateSceneHierarchy()
    {
        CargoGridAutoBootstrap.BuildRuntimeSetup();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Cargo Grid Scene hierarchy created with all visuals and screens!");
    }

    [MenuItem("Tools/Convert Cargo Textures To Sprites")]
    public static void ConvertTexturesToSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { "Assets/Resources/CargoGrid/Sprites" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool modified = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    modified = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    modified = true;
                }
                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    modified = true;
                }
                if (modified)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
}
