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
        art.background = Sprite("Background"); art.empty = Sprite("Squares/BackGrid"); art.highlight = Sprite("Squares/HighlightGrid");
        art.popup = Sprite("WinLosePopup/PopupBackground"); art.gameOver = Sprite("WinLosePopup/GameOver"); art.retry = Sprite("WinLosePopup/Try Again"); art.back = Sprite("MainMenu/Back");
        art.blocks = new[] { "Blue", "Green", "Ornage", "Pink", "Red", "Violet", "Yellow", "NavyBlue" }.Select(n => Sprite("Squares/" + n)).Where(s => s != null).ToArray();
        EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
    }
    static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/Texture/" + name + ".png").OfType<Sprite>().FirstOrDefault();
}
