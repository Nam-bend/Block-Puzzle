using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Invoked in an isolated batch Editor, never automatically in the user's Editor.
[InitializeOnLoad]
public static class BlockGameSmokeCheck
{
    const string Running = "BlockBlast.BatchChecks";
    static int frames, phase;
    static readonly List<string> results = new List<string>();
    static BlockGameSmokeCheck()
    {
        if (SessionState.GetBool(Running, false)) EditorApplication.update += Tick;
    }
    public static void BatchRun()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run checks in an isolated batch-mode project.");
        try
        {
            BlockSceneSetup.PrepareScenes();
            BlockSceneSetup.ValidateSavedScenes();
            EditorSceneManager.OpenScene("Assets/Scenes/Game.unity");
            SessionState.SetBool(Running, true);
            SessionState.SetBool("BlockBlast.HadBest", PlayerPrefs.HasKey("BlockBlastBest"));
            SessionState.SetInt("BlockBlast.SavedBest", PlayerPrefs.GetInt("BlockBlastBest", 0));
            EditorApplication.EnterPlaymode();
        }
        catch (Exception error) { Finish(error); }
    }
    static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(owner);
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        results.Add("PASS " + name);
    }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || ++frames < 12) return;
        frames = 0;
        try
        {
            if (phase == 0)
            {
                var game = UnityEngine.Object.FindFirstObjectByType<BlockGame>();
                Check(game != null && game.enabled && game.ValidateConfiguration(out _), "game starts with serialized Inspector references");
                Check(UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Length == 1, "one pre-authored Canvas");
                Check(UnityEngine.Object.FindObjectsByType<BlockCell>(FindObjectsSortMode.None).Length == 64, "64 prefab cells in scene");
                Check(UnityEngine.Object.FindObjectsByType<BlockGame>(FindObjectsSortMode.None).Length == 1, "one scene GameManager, no runtime bootstrap");
                Check(UnityEngine.Object.FindObjectsByType<BlockPiece>(FindObjectsSortMode.None).Length == 3, "three prefab pieces dealt");
                var piece = UnityEngine.Object.FindFirstObjectByType<BlockPiece>();
                var parent = piece.transform.parent;
                Canvas.ForceUpdateCanvases();
                var hits = new List<RaycastResult>();
                var pointer = new PointerEventData(EventSystem.current) { pointerId = -1, position = RectTransformUtility.WorldToScreenPoint(null, piece.FirstBlockPosition) };
                EventSystem.current.RaycastAll(pointer, hits);
                Check(hits.Any(hit => hit.gameObject.GetComponentInParent<BlockPiece>() == piece), "visible prefab blocks receive UI raycasts");
                piece.OnBeginDrag(pointer);
                Check(piece.transform.parent == game.DragLayer, "drag re-parents into scene DragLayer");
                pointer.position = new Vector2(-10000, -10000); piece.OnDrag(pointer); piece.OnEndDrag(pointer);
                Check(piece.transform.parent == parent, "invalid drop returns piece to its slot");
                Check(!game.IsOver, "invalid drop does not end game");
                var board = Field<BlockBoard>(game, "board");
                Vector2Int origin = Vector2Int.zero; bool found = false;
                for (int y = 0; y < 8 && !found; y++) for (int x = 0; x < 8 && !found; x++)
                    if (board.Fits(piece.Shape, new Vector2Int(x,y))) { origin = new Vector2Int(x,y); found = true; }
                Check(found, "dealt piece has a legal move");
                // Check snapping after changing the board's scale in the Inspector.
                var grid = game.GridView; var originalScale = grid.transform.localScale;
                grid.transform.localScale = Vector3.one * .8f;
                piece.transform.SetParent(game.DragLayer, true); piece.transform.localScale = grid.DragScale(game.DragLayer);
                var cells = Field<BlockCell[]>(grid, "cells");
                var target = cells.First(cell => cell.Coordinate == origin + piece.Shape[0]).transform.position;
                piece.transform.position += target - piece.FirstBlockPosition;
                Check(grid.Origin(piece) == origin, "drop coordinates follow scaled scene grid");
                game.Preview(piece);
                Check(game.Drop(piece) && Field<int>(game, "score") > 0, "valid prefab drop places blocks and scores");
                grid.transform.localScale = originalScale;
                game.Restart();
                phase = 1;
            }
            else if (phase == 1)
            {
                var game = UnityEngine.Object.FindFirstObjectByType<BlockGame>();
                Check(UnityEngine.Object.FindObjectsByType<BlockPiece>(FindObjectsSortMode.None).Length == 3 && Field<int>(game, "score") == 0, "restart clears old pieces and score");
                var board = Field<BlockBoard>(game, "board");
                for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) board.Cells[x,y] = 1;
                typeof(BlockGame).GetMethod("CheckMoves", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(game, null);
                var panel = Field<GameObject>(game, "gameOverPanel");
                Check(game.IsOver && panel.activeSelf, "no moves opens existing Game Over panel");
                panel.GetComponentInChildren<Button>().onClick.Invoke();
                Check(!game.IsOver && !panel.activeSelf, "serialized Try Again button resets game");
                game.ReturnToMenu(); phase = 2;
            }
            else if (phase == 2)
            {
                Check(UnityEngine.Object.FindFirstObjectByType<BlockGame>() == null, "menu has no gameplay bootstrap");
                var play = GameObject.Find("PlayButton").GetComponent<Button>();
                Check(play.onClick.GetPersistentEventCount() == 1, "Play button has serialized event");
                play.onClick.Invoke(); phase = 3;
            }
            else
            {
                var game = UnityEngine.Object.FindFirstObjectByType<BlockGame>();
                Check(game != null && game.enabled && UnityEngine.Object.FindObjectsByType<BlockPiece>(FindObjectsSortMode.None).Length == 3, "menu Play button loads a working game scene");
                Finish(null);
            }
        }
        catch (Exception error) { Finish(error); }
    }
    static void Finish(Exception error)
    {
        EditorApplication.update -= Tick;
        if (SessionState.GetBool(Running, false))
        {
            if (SessionState.GetBool("BlockBlast.HadBest", false)) PlayerPrefs.SetInt("BlockBlastBest", SessionState.GetInt("BlockBlast.SavedBest", 0));
            else PlayerPrefs.DeleteKey("BlockBlastBest");
            PlayerPrefs.Save();
        }
        SessionState.SetBool(Running, false);
        if (error != null) results.Add("FAIL " + error);
        File.WriteAllLines(Path.Combine(Application.dataPath, "../PlayModeChecks.txt"), results);
        foreach (var line in results) Debug.Log(line);
        EditorApplication.Exit(error == null ? 0 : 1);
    }
}
