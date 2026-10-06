using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// An explicit Edit Mode art pass. Never runs automatically on import or Play.
public static class BlockCatThemeSetup
{
    const string Folder = "Assets/Art/CatNookWarm";
    static readonly Color Ink = Hex("784126"), Cream = Hex("784126"), Muted = Hex("A16843");
    static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var color); return color; }

    [Serializable] sealed class SpriteRef { public string guid; public long fileID; }
    [Serializable] sealed class PoseRecord
    {
        public string name, breed, expression;
        public Vector2Int[] shape;
        public SpriteRef[] parts;
        public SpriteRef small;
    }
    [Serializable] sealed class Catalog { public PoseRecord[] cats; }

    public static void AssignArt(BlockArt art)
    {
        CreateSprites();
        AssetDatabase.Refresh();
        var catalog = JsonUtility.FromJson<Catalog>("{\"cats\":" + File.ReadAllText(Folder + "/cat_catalog.json") + "}");
        if (catalog?.cats == null || catalog.cats.Length == 0) throw new InvalidOperationException("Warm cat catalog is missing.");
        Sprite Resolve(SpriteRef reference)
        {
            var path = AssetDatabase.GUIDToAssetPath(reference.guid);
            foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string _, out long id) && id == reference.fileID) return sprite;
            throw new InvalidOperationException("Cannot resolve cat sprite " + reference.fileID + " in " + path);
        }
        art.cats = catalog.cats.Select(pose => new BlockCatPose {
            name = pose.name, breed = pose.breed, expression = pose.expression, shape = pose.shape,
            parts = pose.parts.Select(Resolve).ToArray(), small = Resolve(pose.small)
        }).ToArray();
        art.background = Sprite("background"); art.empty = Sprite("cell_empty");
        art.highlight = Sprite("cell_preview"); art.popup = Sprite("panel");
        art.retry = Sprite("button_primary"); art.back = Sprite("button_secondary"); art.gameOver = null;
        art.blocks = new[] { "blue", "mint", "gold", "coral", "violet", "pink" }.Select(n => Sprite("block_" + n)).ToArray();
        EditorUtility.SetDirty(art);
    }

    [MenuItem("Tools/Block Blast/Apply Cat Nook Theme")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        Directory.CreateDirectory(Folder);
        var art = AssetDatabase.LoadAssetAtPath<BlockArt>("Assets/Resources/BlockArt.asset");
        if (art == null) throw new InvalidOperationException("BlockArt is missing.");
        AssignArt(art);
        EditorUtility.SetDirty(art);
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            StyleScene("Assets/Scenes/Game.unity", art, false);
            StyleScene("Assets/Scenes/SampleScene.unity", art, true);
            AssetDatabase.SaveAssets();
            BlockSceneSetup.ValidateSavedScenes();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        Debug.Log("CAT NOOK: theme applied; scene references validated.");
    }

    static void StyleScene(string path, BlockArt art, bool menu)
    {
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var canvas = Find<Canvas>(scene);
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(946, 2048);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var bg = Named(scene, "BG").GetComponent<Image>();
        bg.sprite = art.background; bg.color = Color.white; bg.raycastTarget = false;
        bg.rectTransform.anchorMin = Vector2.zero; bg.rectTransform.anchorMax = Vector2.one;
        bg.rectTransform.offsetMin = bg.rectTransform.offsetMax = Vector2.zero;
        // Preserve the source illustration aspect ratio while covering the canvas.
        var fitter = bg.GetComponent<AspectRatioFitter>() ?? bg.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = (float)art.background.texture.width / art.background.texture.height;
        var content = Named(scene, "CatSafeContent") as RectTransform;
        if (content == null) content = Rect(canvas.transform, "CatSafeContent", Vector2.zero, new Vector2(946, 2048));
        if (content.GetComponent<BlockCatSafeArea>() == null) content.gameObject.AddComponent<BlockCatSafeArea>();
        foreach (var child in canvas.transform.Cast<Transform>().ToArray())
            if (child != bg.transform && child != content) child.SetParent(content, false);
        bg.transform.SetAsFirstSibling();
        if (menu) StyleMenu(scene, content); else StyleGame(scene, content);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }

    static void StyleMenu(Scene scene, RectTransform root)
    {
        foreach (var name in new[] { "Title", "Shapes", "Settings", "CatMenuFooter" })
        { var legacy = Named(scene, name); if (legacy != null) UnityEngine.Object.DestroyImmediate(legacy.gameObject); }
        Image(root, "CatMascot", "mascot", new Vector2(0, 524), new Vector2(380, 380));
        Label(root, "CatTitle", "CAT NOOK", new Vector2(0, 220), 90, Cream);
        Label(root, "CatSubtitle", "A little puzzle. A happy catnap.", new Vector2(0, 110), 29, Muted);
        var play = Named(scene, "PlayButton").GetComponent<Button>();
        StyleButton(play, "button_primary", "LET'S PLAY", new Vector2(0, -116), new Vector2(520, 140));
        var settings = Named(scene, "SettingButtonShow").GetComponent<Button>();
        settings.transform.SetParent(root, false); settings.gameObject.SetActive(true);
        StyleButton(settings, "button_secondary", "SOUND ON", new Vector2(0, -306), new Vector2(340, 100));
        var best = Label(root, "CatMenuBest", "PERSONAL BEST  0", new Vector2(0, -466), 29, Cream);
        var status = root.GetComponent<BlockCatMenuStatus>() ?? root.gameObject.AddComponent<BlockCatMenuStatus>();
        Set(status, "bestLabel", best); Set(status, "soundLabel", settings.GetComponentInChildren<TMP_Text>());
        Set(status, "soundButton", settings);
    }

    static void StyleGame(Scene scene, RectTransform root)
    {
        var top = Named(scene, "TopBackgroundPanel"); if (top != null) UnityEngine.Object.DestroyImmediate(top.gameObject);
        Image(root, "CatHeaderMascot", "mascot", new Vector2(-328, 844), new Vector2(140, 140));
        Label(root, "CatGameTitle", "CAT NOOK", new Vector2(62, 874), 39, Cream);
        StyleLabel(Named(scene, "Score").GetComponent<TMP_Text>(), new Vector2(62, 714), 86, Cream);
        StyleLabel(Named(scene, "BestScore").GetComponent<TMP_Text>(), new Vector2(0, 584), 25, Ink);
        Label(root, "CatScoreCaption", "SCORE", new Vector2(62, 772), 25, Muted);
        var bestPanel = Image(root, "CatBestPanel", "panel", new Vector2(0, 590), new Vector2(586, 90));
        bestPanel.type = UnityEngine.UI.Image.Type.Sliced;
        bestPanel.transform.SetSiblingIndex(Named(scene, "BestScore").GetSiblingIndex());
        var grid = Find<BlockGridView>(scene);
        var gridRect = (RectTransform)grid.transform;
        gridRect.anchoredPosition = new Vector2(0, 80); gridRect.localScale = Vector3.one;
        var frame = Image(root, "CatBoardFrame", "board_frame", new Vector2(0, 80), new Vector2(840, 840));
        frame.type = UnityEngine.UI.Image.Type.Sliced; frame.transform.SetSiblingIndex(grid.transform.GetSiblingIndex());
        foreach (var cell in grid.GetComponentsInChildren<BlockCell>(true)) cell.Show(Sprite("cell_empty"));
        var tray = Named(scene, "PieceSlots") as RectTransform;
        tray.anchoredPosition = new Vector2(0, -590);
        var trayPanel = Image(root, "CatTray", "panel", new Vector2(0, -590), new Vector2(870, 270));
        trayPanel.type = UnityEngine.UI.Image.Type.Sliced; trayPanel.color = Color.white;
        trayPanel.transform.SetSiblingIndex(tray.GetSiblingIndex());
        StyleLabel(Named(scene, "Message").GetComponent<TMP_Text>(), new Vector2(0, -420), 26, Muted);
        StyleButton(Named(scene, "MenuButton").GetComponent<Button>(), "button_secondary", "HOME", new Vector2(-225, -850), new Vector2(300, 108));
        StyleButton(Named(scene, "RestartButton").GetComponent<Button>(), "button_primary", "REPLAY", new Vector2(225, -850), new Vector2(300, 108));
        var panel = Named(scene, "GameOverPanel");
        var overlay = panel.GetComponent<Image>(); overlay.color = new Color(.35f, .19f, .1f, .6f);
        var popup = Named(scene, "PopupBackground").GetComponent<Image>();
        popup.sprite = Sprite("panel"); popup.type = UnityEngine.UI.Image.Type.Sliced; popup.color = Color.white;
        popup.rectTransform.sizeDelta = new Vector2(800, 860);
        var oldTitle = Named(scene, "GameOverTitle"); if (oldTitle != null) UnityEngine.Object.DestroyImmediate(oldTitle.gameObject);
        var oldBadge = Named(scene, "CatEndBadge"); if (oldBadge != null) UnityEngine.Object.DestroyImmediate(oldBadge.gameObject);
        Image(panel, "CatEndMascot", "mascot_sleep", new Vector2(0, 255), new Vector2(240, 240));
        Label(panel, "CatEndTitle", "TIME FOR A CATNAP", new Vector2(0, 70), 40, Ink);
        var score = Named(scene, "FinalScore").GetComponent<TMP_Text>();
        StyleLabel(score, new Vector2(0, -65), 38, Ink); score.rectTransform.sizeDelta = new Vector2(680, 140);
        StyleButton(Named(scene, "TryAgainButton").GetComponent<Button>(), "button_primary", "PLAY AGAIN", new Vector2(0, -206), new Vector2(480, 125));
        var home = Named(scene, "CatPopupHome")?.GetComponent<Button>();
        if (home == null)
        {
            home = Image(panel, "CatPopupHome", "button_secondary", new Vector2(0, -343), new Vector2(340, 95)).gameObject.AddComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(home.onClick, Find<BlockGame>(scene).ReturnToMenu);
        }
        StyleButton(home, "button_secondary", "HOME", new Vector2(0, -343), new Vector2(340, 95));
        panel.SetAsLastSibling(); panel.gameObject.SetActive(false);
    }

    static void StyleButton(Button button, string sprite, string text, Vector2 position, Vector2 size)
    {
        var rect = (RectTransform)button.transform; rect.anchoredPosition = position; rect.sizeDelta = size;
        button.image.sprite = Sprite(sprite); button.image.type = UnityEngine.UI.Image.Type.Sliced; button.image.color = Color.white; button.image.raycastTarget = true;
        foreach (var child in button.transform.Cast<Transform>()) child.gameObject.SetActive(false);
        var label = Label(button.transform, "CatButtonLabel", text, Vector2.zero, 32, Ink);
        label.rectTransform.sizeDelta = new Vector2(size.x - 100, size.y - 20);
        label.rectTransform.anchoredPosition = new Vector2(20, 4);
        var colors = button.colors; colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1, .97f, .92f); colors.pressedColor = new Color(.91f, .74f, .56f);
        colors.disabledColor = new Color(.55f, .6f, .62f, .7f); colors.fadeDuration = .08f;
        button.colors = colors; button.transition = Selectable.Transition.ColorTint;
    }
    static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();
    static Transform Named(Scene scene, string name) => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);
    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    { var s = new SerializedObject(target); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var rect = parent.Find(name) as RectTransform;
        if (rect == null) { rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); }
        rect.gameObject.layer = 5; rect.gameObject.SetActive(true);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
        rect.anchoredPosition = pos; rect.sizeDelta = size; return rect;
    }
    static Image Image(Transform parent, string name, string sprite, Vector2 pos, Vector2 size)
    {
        var rect = Rect(parent, name, pos, size);
        var image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>();
        image.sprite = Sprite(sprite); image.color = Color.white; image.raycastTarget = false; return image;
    }
    static TMP_Text Label(Transform parent, string name, string text, Vector2 pos, float size, Color color)
    {
        var rect = Rect(parent, name, pos, new Vector2(800, 110));
        var label = rect.GetComponent<TMP_Text>() ?? rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        label.text = text; StyleLabel(label, pos, size, color); label.fontStyle = FontStyles.Bold;
        return label;
    }
    static void StyleLabel(TMP_Text label, Vector2 pos, float size, Color color)
    { label.rectTransform.anchoredPosition = pos; label.fontSize = size; label.color = color; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false; }
    static Sprite Sprite(string name)
    {
        if (name == "mascot")
        {
            var art = AssetDatabase.LoadAssetAtPath<BlockArt>("Assets/Resources/BlockArt.asset");
            var pose = art?.cats?.FirstOrDefault(cat => cat != null && cat.name == "single_calico_1x1");
            if (pose?.small != null) return pose.small;
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + name + ".png");
    }

    // Import the authored PNG assets; reapplying the theme does not regenerate art.
    static void CreateSprites()
    {
        foreach (var name in new[] { "blue", "mint", "gold", "coral", "violet", "pink" })
            Import("block_" + name, Vector4.zero);
        foreach (var name in new[] { "background", "cell_empty", "cell_preview", "mascot_sleep", "icon_paw" })
            Import(name, Vector4.zero);
        Import("board_frame", new Vector4(36, 36, 36, 36));
        Import("panel", new Vector4(40, 40, 40, 40));
        Import("button_primary", new Vector4(62, 30, 30, 30));
        Import("button_secondary", new Vector4(62, 30, 30, 30));
        foreach (var path in Directory.GetFiles(Folder + "/Cats", "*.png"))
            AssetDatabase.ImportAsset(path.Replace('\\', '/'), ImportAssetOptions.ForceSynchronousImport);

    }
    static void Import(string name, Vector4 border)
    {
        var path = Folder + "/" + name + ".png";
        if (!File.Exists(path)) throw new InvalidOperationException("Missing Cat Nook asset: " + path);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = border; importer.spritePixelsPerUnit = 100; importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }
}
