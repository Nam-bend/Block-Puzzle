using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BlockGame : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] BlockArt art;
    [SerializeField] BlockGridView gridView;
    [SerializeField] RectTransform dragLayer;
    [SerializeField] RectTransform[] pieceSlots;
    [Tooltip("Three prefab instances placed under Piece Slots in the scene.")]
    [SerializeField] BlockPiece[] pieceViews;
    [SerializeField] TMP_Text scoreText, bestText, message;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] TMP_Text gameOverScore;
    [Header("Piece presentation")]
    [SerializeField, Range(.1f, 1f)] float trayScale = .58f;
    [SerializeField] float dragLift = 90;
    BlockBoard board;
    readonly List<BlockPiece> pieces = new List<BlockPiece>();
    int score, best, combo;
    public bool IsOver { get; private set; }
    public BlockGridView GridView => gridView;
    public RectTransform DragLayer => dragLayer;
    public float DragLift => dragLift;
    void Start()
    {
        if (!ValidateConfiguration(out var error)) { Debug.LogError("BlockGame: " + error, this); enabled = false; return; }
        best = PlayerPrefs.GetInt("BlockBlastBest", 0);
        AudioListener.pause = PlayerPrefs.GetInt("Muted", 0) == 1;
        Restart();
    }
    public bool ValidateConfiguration(out string error)
    {
        if (art == null || art.empty == null || art.blocks == null || art.blocks.Length == 0) error = "Assign BlockArt with empty and block sprites in the Inspector.";
        else if (System.Array.Exists(art.blocks, sprite => sprite == null)) error = "BlockArt contains a missing block sprite.";
        else if (gridView == null || !gridView.ValidateCells()) error = "Assign a Grid View with 64 unique cell coordinates.";
        else if (dragLayer == null || pieceSlots == null || pieceSlots.Length != 3 || System.Array.Exists(pieceSlots, slot => slot == null)) error = "Assign Drag Layer and three Piece Slots.";
        else if (pieceViews == null || pieceViews.Length != 3 || System.Array.Exists(pieceViews, piece => piece == null || !piece.ValidateBlocks())) error = "Assign three scene BlockPiece prefab instances, each with nine block Images.";
        else if (pieceViews[0] == pieceViews[1] || pieceViews[0] == pieceViews[2] || pieceViews[1] == pieceViews[2]) error = "Piece Views must contain three different instances.";
        else if (scoreText == null || bestText == null || message == null || gameOverPanel == null || gameOverScore == null) error = "Assign score labels, message and Game Over panel/score.";
        else { error = null; return true; }
        return false;
    }
    public void Restart()
    {
        if (!enabled) return;
        foreach (var piece in pieceViews) { piece.ResetToSlot(); piece.gameObject.SetActive(false); }
        pieces.Clear(); board = new BlockBoard(); score = 0; combo = 0; IsOver = false;
        gameOverPanel.SetActive(false); message.text = "Keo khoi vao bang de lap day hang hoac cot";
        Refresh(); Deal();
    }
    public void ReturnToMenu() => SceneManager.LoadScene("SampleScene");
    void Deal()
    {
        var playable = new List<Vector2Int[]>();
        foreach (var shape in BlockBoard.Shapes) if (board.HasMove(shape)) playable.Add(shape);
        if (playable.Count == 0) { EndGame(); return; }
        for (int i = 0; i < pieceViews.Length; i++)
        {
            var shape = playable[Random.Range(0, playable.Count)];
            int color = Random.Range(1, art.blocks.Length + 1);
            var piece = pieceViews[i];
            piece.Configure(this, shape, color, art.blocks[color - 1], trayScale, pieceSlots[i]);
            piece.gameObject.SetActive(true);
            pieces.Add(piece);
        }
        CheckMoves();
    }
    public void Preview(BlockPiece piece)
    {
        if (IsOver || board == null) return;
        Refresh(); var origin = gridView.Origin(piece);
        if (board.Fits(piece.Shape, origin)) gridView.Preview(piece.Shape, origin, art.blocks[piece.Color - 1]);
    }
    public void ClearPreview() => Refresh();
    public bool Drop(BlockPiece piece)
    {
        if (IsOver || board == null || !pieces.Contains(piece)) { Refresh(); return false; }
        var origin = gridView.Origin(piece);
        if (!board.Fits(piece.Shape, origin)) { Refresh(); return false; }
        int gained = board.Place(piece.Shape, origin, piece.Color, out int lines);
        combo = lines > 0 ? combo + 1 : 0; gained += Mathf.Max(0, combo - 1) * 50; score += gained;
        if (score > best) { best = score; PlayerPrefs.SetInt("BlockBlastBest", best); PlayerPrefs.Save(); }
        message.text = lines > 0 ? "+" + gained + "   " + lines + " HANG!" + (combo > 1 ? "   COMBO x" + combo : "") : "Keo khoi vao bang de lap day hang hoac cot";
        pieces.Remove(piece); piece.ResetToSlot(); piece.gameObject.SetActive(false); Refresh();
        if (pieces.Count == 0) Deal(); else CheckMoves();
        return true;
    }
    void Refresh()
    {
        if (board == null) return;
        gridView.Render(board, art); scoreText.text = score.ToString(); bestText.text = "KY LUC  " + best;
    }
    void CheckMoves()
    {
        foreach (var piece in pieces) if (board.HasMove(piece.Shape)) return;
        EndGame();
    }
    void EndGame()
    {
        IsOver = true; gameOverScore.text = "DIEM  " + score + "\nKY LUC  " + best;
        gameOverPanel.SetActive(true); gameOverPanel.transform.SetAsLastSibling();
    }
}
