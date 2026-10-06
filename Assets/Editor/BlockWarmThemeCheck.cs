using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Read-only checks. Preview scenes never modify or save the user's open scene.
[InitializeOnLoad]
public static class BlockWarmThemeCheck
{
    const string Marker = "Temp/RunWarmThemeCheck";
    const string Report = "Temp/WarmThemeNativeValidation.txt";
    static BlockWarmThemeCheck()
    {
        EditorApplication.delayCall += () => {
            if (!File.Exists(Marker) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Marker);
            Run();
        };
    }
    [MenuItem("Tools/Block Blast/Validate Warm Theme")]
    public static void Run()
    {
        try
        {
            var art = AssetDatabase.LoadAssetAtPath<BlockArt>("Assets/Resources/BlockArt.asset");
            if (art == null || art.cats == null || art.cats.Length != 17) throw new InvalidOperationException("Missing warm cat catalog.");
            foreach (var pose in art.cats)
            {
                if (pose.small == null || pose.parts.Any(sprite => sprite == null)) throw new InvalidOperationException("Missing imported sprite in " + pose.name);
                foreach (var sprite in pose.parts.Concat(new[] { pose.small }))
                    if (!AssetDatabase.GetAssetPath(sprite).StartsWith("Assets/Art/CatNookWarm/")) throw new InvalidOperationException("Retired art in " + pose.name);
            }
            foreach (var shape in BlockBoard.Shapes)
                if (!art.cats.Any(pose => pose.Match(shape, out _, out _))) throw new InvalidOperationException("Uncovered puzzle shape.");
            foreach (var path in new[] { "Assets/Scenes/Game.unity", "Assets/Scenes/SampleScene.unity" })
            {
                var scene = EditorSceneManager.OpenPreviewScene(path);
                try
                {
                    var roots = scene.GetRootGameObjects();
                    var game = roots.SelectMany(root => root.GetComponentsInChildren<BlockGame>(true)).FirstOrDefault();
                    if (game != null && !game.ValidateConfiguration(out var error)) throw new InvalidOperationException(error);
                    foreach (var image in roots.SelectMany(root => root.GetComponentsInChildren<Image>(true)))
                        if (image.sprite != null && !AssetDatabase.GetAssetPath(image.sprite).StartsWith("Assets/Art/CatNookWarm/"))
                            throw new InvalidOperationException("Retired sprite on " + image.name);
                    foreach (var button in roots.SelectMany(root => root.GetComponentsInChildren<Button>(true)))
                        if (button.onClick.GetPersistentEventCount() != 1 || button.onClick.GetPersistentTarget(0) == null)
                            throw new InvalidOperationException("Unwired button " + button.name);
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); }
            }
            File.WriteAllText(Report, "PASS native imported cat sprites, all 27 shapes, saved-scene configuration, warm Image references and button targets. Play Mode and device gestures were not run.");
            Debug.Log("Cat Nook Warm: native theme checks passed.");
        }
        catch (Exception error)
        {
            File.WriteAllText(Report, "FAIL " + error);
            Debug.LogException(error);
        }
    }
}
