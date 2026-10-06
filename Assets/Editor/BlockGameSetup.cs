using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class BlockGameSetup : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) { BlockSceneSetup.ValidateSavedScenes(); }
    [MenuItem("Tools/Block Blast/Prepare Game")]
    public static void Prepare()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string path = "Assets/Resources/BlockArt.asset";
        var art = AssetDatabase.LoadAssetAtPath<BlockArt>(path);
        if (art != null) return; // Preserve sprites assigned by the designer.
        art = ScriptableObject.CreateInstance<BlockArt>(); AssetDatabase.CreateAsset(art, path);
        BlockCatThemeSetup.AssignArt(art);
        EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
    }
}
