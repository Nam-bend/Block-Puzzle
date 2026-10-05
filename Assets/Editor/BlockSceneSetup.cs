using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// This builds serialized assets in Edit Mode only. Runtime uses Inspector references.
public static class BlockSceneSetup
{
    const string Prefabs = "Assets/Prefabs";

    [MenuItem("Tools/Block Blast/Set Up Scenes and Prefabs")]
    public static void PrepareScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode before editing scene assets.");
        BlockGameSetup.Prepare();
        if (!AssetDatabase.IsValidFolder(Prefabs)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        var art = AssetDatabase.LoadAssetAtPath<BlockArt>("Assets/Resources/BlockArt.asset");
        if (art == null || art.empty == null || art.blocks.Length == 0) throw new InvalidOperationException("Missing BlockArt sprites.");
        CreatePrefabs(art);
        EditScene("Assets/Scenes/Game.unity", scene => SetUpGame(scene, art));
        EditScene("Assets/Scenes/SampleScene.unity", SetUpMenu);
        AssetDatabase.SaveAssets();
        Debug.Log("Block Blast: scene UI, prefab grid/pieces and button events are ready.");
    }
    static void EditScene(string path, Action<Scene> edit)
    {
        var scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.IsValid() || !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        try { edit(scene); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }
    static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
    static Transform Named(Scene scene, string name) => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);
    static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(owner); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static void SetArray(UnityEngine.Object owner, string field, UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(owner); var array = serialized.FindProperty(field); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Float(UnityEngine.Object owner, string field, float value)
    {
        var serialized = new SerializedObject(owner); serialized.FindProperty(field).floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static void CreatePrefabs(BlockArt art)
    {
        if (AssetDatabase.LoadAssetAtPath<BlockCell>(Prefabs + "/BlockCell.prefab") == null)
        {
            var image = Image(null, "BlockCell", art.empty, Vector2.zero, Vector2.one * 96);
            image.raycastTarget = false;
            var cell = image.gameObject.AddComponent<BlockCell>(); Set(cell, "image", image);
            PrefabUtility.SaveAsPrefabAsset(image.gameObject, Prefabs + "/BlockCell.prefab"); UnityEngine.Object.DestroyImmediate(image.gameObject);
        }
        if (AssetDatabase.LoadAssetAtPath<BlockPiece>(Prefabs + "/BlockPiece.prefab") == null)
        {
            var root = Rect(null, "BlockPiece", Vector2.zero, Vector2.one * 500);
            var piece = root.gameObject.AddComponent<BlockPiece>();
            var blocks = new UnityEngine.Object[9];
            for (int i = 0; i < 9; i++) blocks[i] = Image(root, "Block " + i, art.blocks[0], new Vector2((i % 3 - 1) * 100, (i / 3 - 1) * 100), Vector2.one * 96);
            SetArray(piece, "blocks", blocks);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, Prefabs + "/BlockPiece.prefab"); UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
        if (AssetDatabase.LoadAssetAtPath<BlockGridView>(Prefabs + "/BoardGrid.prefab") == null)
        {
            var root = Rect(null, "Grid", Vector2.zero, Vector2.one * 800);
            var grid = root.gameObject.AddComponent<BlockGridView>(); Float(grid, "cellSize", 100);
            var prefab = AssetDatabase.LoadAssetAtPath<BlockCell>(Prefabs + "/BlockCell.prefab");
            var cells = new UnityEngine.Object[64];
            for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
            {
                var cell = (BlockCell)PrefabUtility.InstantiatePrefab(prefab, root);
                cell.name = "Cell " + x + "," + y; cell.SetCoordinate(new Vector2Int(x, y));
                ((RectTransform)cell.transform).anchoredPosition = new Vector2((x - 3.5f) * 100, (y - 3.5f) * 100);
                PrefabUtility.RecordPrefabInstancePropertyModifications(cell);
                PrefabUtility.RecordPrefabInstancePropertyModifications(cell.transform);
                cells[y * 8 + x] = cell;
            }
            SetArray(grid, "cells", cells);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, Prefabs + "/BoardGrid.prefab"); UnityEngine.Object.DestroyImmediate(root.gameObject);
        }
    }
    static Canvas Canvas(Scene scene)
    {
        var canvas = Find<Canvas>(scene);
        if (canvas == null)
        {
            var root = Rect(null, "Canvas", Vector2.zero, Vector2.zero);
            SceneManager.MoveGameObjectToScene(root.gameObject, scene);
            canvas = root.gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.transform.localScale = Vector3.one;
        var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(946, 2048);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }
    static void Events(Scene scene)
    {
        var events = Find<EventSystem>(scene);
        if (events == null)
        {
            var root = new GameObject("EventSystem", typeof(EventSystem)); SceneManager.MoveGameObjectToScene(root, scene); events = root.GetComponent<EventSystem>();
        }
        var legacy = events.GetComponent<StandaloneInputModule>();
        if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy);
        if (events.GetComponent<InputSystemUIInputModule>() == null) events.gameObject.AddComponent<InputSystemUIInputModule>();
    }
    static void SetUpGame(Scene scene, BlockArt art)
    {
        var canvas = Canvas(scene); Events(scene);
        var game = Find<BlockGame>(scene);
        if (game != null && game.ValidateConfiguration(out _)) return;
        if (game == null)
        {
            var manager = new GameObject("GameManager"); SceneManager.MoveGameObjectToScene(manager, scene); game = manager.AddComponent<BlockGame>();
        }
        var root = canvas.transform;
        var background = Named(scene, "BG")?.GetComponent<Image>() ?? Image(root, "BG", art.background, Vector2.zero, new Vector2(946,2048));
        Stretch(background.rectTransform); background.raycastTarget = false; background.transform.SetAsFirstSibling();
        var top = Named(scene, "TopBackgroundPanel")?.GetComponent<Image>();
        if (top != null) top.raycastTarget = false;
        var grid = Find<BlockGridView>(scene);
        if (grid == null)
        {
            var old = Named(scene, "Grid");
            // Only replace the empty legacy placeholder, which references the removed Grid script.
            if (old != null && old.childCount == 0) UnityEngine.Object.DestroyImmediate(old.gameObject);
            grid = (BlockGridView)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<BlockGridView>(Prefabs + "/BoardGrid.prefab"), root);
            ((RectTransform)grid.transform).anchoredPosition = new Vector2(0,100);
            PrefabUtility.RecordPrefabInstancePropertyModifications(grid.transform);
        }
        var score = Label(root, "Score", "0", new Vector2(0,730), 72);
        var best = Label(root, "BestScore", "KY LUC  0", new Vector2(0,620), 32);
        var message = Label(root, "Message", "Keo khoi vao bang de lap day hang hoac cot", new Vector2(0,-395), 28);
        var slots = Rect(root, "PieceSlots", new Vector2(0,-620), new Vector2(900,300));
        var slotRefs = new UnityEngine.Object[3];
        var pieceRefs = new UnityEngine.Object[3];
        var piecePrefab = AssetDatabase.LoadAssetAtPath<BlockPiece>(Prefabs + "/BlockPiece.prefab");
        for (int i = 0; i < 3; i++)
        {
            var slot = Rect(slots, "Slot " + (i + 1), new Vector2((i - 1) * 280,0), new Vector2(260,300));
            slotRefs[i] = slot;
            var piece = (BlockPiece)PrefabUtility.InstantiatePrefab(piecePrefab, slot);
            piece.transform.localScale = Vector3.one * .58f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(piece.transform);
            pieceRefs[i] = piece;
        }
        var drag = Rect(root, "DragLayer", Vector2.zero, Vector2.zero); Stretch(drag);
        var panel = Image(root, "GameOverPanel", null, Vector2.zero, Vector2.zero); Stretch(panel.rectTransform); panel.color = new Color(0,0,0,.8f);
        Image(panel.transform, "PopupBackground", art.popup, Vector2.zero, new Vector2(800,720));
        Image(panel.transform, "GameOverTitle", art.gameOver, new Vector2(0,230), new Vector2(600,130)).preserveAspect = true;
        var finalScore = Label(panel.transform, "FinalScore", "DIEM  0\nKY LUC  0", Vector2.zero, 48);
        var retry = Button(panel.transform, "TryAgainButton", "CHOI LAI", new Vector2(0,-230), new Vector2(430,120));
        if (art.retry != null) { retry.image.sprite = art.retry; retry.image.color = Color.white; retry.GetComponentInChildren<TMP_Text>().text = ""; }
        UnityEventTools.AddPersistentListener(retry.onClick, game.Restart);
        var menu = Button(root, "MenuButton", "MENU", new Vector2(-230,-875), new Vector2(220,90));
        UnityEventTools.AddPersistentListener(menu.onClick, game.ReturnToMenu);
        var restart = Button(root, "RestartButton", "CHOI LAI", new Vector2(230,-875), new Vector2(220,90));
        UnityEventTools.AddPersistentListener(restart.onClick, game.Restart);
        panel.gameObject.SetActive(false);
        Set(game, "art", art); Set(game, "gridView", grid); Set(game, "dragLayer", drag); SetArray(game, "pieceSlots", slotRefs);
        SetArray(game, "pieceViews", pieceRefs);
        Set(game, "scoreText", score); Set(game, "bestText", best); Set(game, "message", message); Set(game, "gameOverPanel", panel.gameObject); Set(game, "gameOverScore", finalScore);
        if (!game.ValidateConfiguration(out var error)) throw new InvalidOperationException(error);
    }
    static void SetUpMenu(Scene scene)
    {
        var canvas = Canvas(scene); Events(scene);
        var menu = Find<BlockMenu>(scene) ?? canvas.gameObject.AddComponent<BlockMenu>();
        var play = Named(scene, "PlayButton")?.GetComponent<Button>();
        if (play == null)
        {
            play = Button(canvas.transform, "PlayButton", "CHOI NGAY", new Vector2(0,-650), new Vector2(430,130));
            UnityEventTools.AddPersistentListener(play.onClick, menu.PlayGame);
        }
        var settings = Named(scene, "SettingButtonShow")?.GetComponent<Button>();
        if (settings != null && settings.onClick.GetPersistentEventCount() == 0) UnityEventTools.AddPersistentListener(settings.onClick, menu.ToggleSound);
        var bg = Named(scene, "BG")?.GetComponent<Image>();
        if (bg != null) { Stretch(bg.rectTransform); bg.raycastTarget = false; }
    }
    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = Vector2.one * .5f; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.gameObject.layer = 5; rect.SetParent(parent, false); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
    static Image Image(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        var image = Rect(parent, name, position, size).gameObject.AddComponent<Image>(); image.sprite = sprite; return image;
    }
    static TMP_Text Label(Transform parent, string name, string text, Vector2 position, float size)
    {
        var label = Rect(parent, name, position, new Vector2(850,150)).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.text = text; label.fontSize = size; label.alignment = TextAlignmentOptions.Center; label.color = Color.white; label.raycastTarget = false; return label;
    }
    static Button Button(Transform parent, string name, string text, Vector2 position, Vector2 size)
    {
        var image = Image(parent, name, null, position, size); image.color = new Color(.12f,.55f,.8f);
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Label(image.transform, "Label", text, Vector2.zero, 36).rectTransform.sizeDelta = size; return button;
    }
    public static void ValidateSavedScenes()
    {
        foreach (var path in new[] { "Assets/Scenes/Game.unity", "Assets/Scenes/SampleScene.unity" })
        {
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                foreach (var go in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0) throw new UnityEditor.Build.BuildFailedException(path + ": missing script on " + go.name);
                if (Find<EventSystem>(scene)?.GetComponent<InputSystemUIInputModule>() == null) throw new UnityEditor.Build.BuildFailedException(path + ": missing UI input module.");
                if (path.EndsWith("/Game.unity"))
                {
                    var game = Find<BlockGame>(scene);
                    if (game == null) throw new UnityEditor.Build.BuildFailedException("Game scene has no BlockGame. Run Tools > Block Blast > Set Up Scenes and Prefabs.");
                    if (!game.ValidateConfiguration(out var error)) throw new UnityEditor.Build.BuildFailedException(error);
                }
                else if (Find<BlockMenu>(scene) == null) throw new UnityEditor.Build.BuildFailedException("Menu scene has no BlockMenu.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
