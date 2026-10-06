using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public sealed class BlockGridView : MonoBehaviour
{
    [SerializeField, Min(1)] float cellSize = 72;
    [Tooltip("64 cells with coordinates from (0,0) at bottom-left to (7,7).")]
    [SerializeField] BlockCell[] cells;
    readonly BlockCell[,] lookup = new BlockCell[BlockBoard.Size, BlockBoard.Size];
    sealed class CatPlacement
    {
        public Vector2Int[] positions;
        public Sprite[] parts;
        public Sprite small;
        public float angle;
        public bool broken;
    }
    readonly List<CatPlacement> cats = new List<CatPlacement>();
    public void ResetCats() => cats.Clear();
    public void RememberCat(BlockPiece piece, Vector2Int origin)
    {
        if (piece.CatParts == null) return;
        var positions = new Vector2Int[piece.Shape.Length];
        for (int i = 0; i < positions.Length; i++) positions[i] = origin + piece.Shape[i];
        cats.Add(new CatPlacement { positions = positions, parts = piece.CatParts, small = piece.SmallCat, angle = piece.CatAngle });
    }
    public float CellSize => cellSize;
    public bool ValidateCells()
    {
        if (cells == null || cells.Length != BlockBoard.Size * BlockBoard.Size || cellSize <= 0) return false;
        System.Array.Clear(lookup, 0, lookup.Length);
        foreach (var cell in cells)
        {
            if (cell == null || !cell.IsConfigured) return false;
            var p = cell.Coordinate;
            if (p.x < 0 || p.y < 0 || p.x >= BlockBoard.Size || p.y >= BlockBoard.Size || lookup[p.x, p.y] != null) return false;
            lookup[p.x, p.y] = cell;
        }
        return true;
    }
    public void Render(BlockBoard board, BlockArt art)
    {
        foreach (var cell in cells)
        {
            int color = board.Cells[cell.Coordinate.x, cell.Coordinate.y];
            cell.Show(color == 0 ? art.empty : art.blocks[color - 1], 1, 0, CellSize * .96f);
        }
        for (int c = cats.Count - 1; c >= 0; c--)
        {
            var cat = cats[c]; int alive = 0;
            for (int i = 0; i < cat.positions.Length; i++)
            {
                var p = cat.positions[i];
                if (p.x < 0) continue;
                if (board.Cells[p.x, p.y] == 0) { cat.positions[i] = new Vector2Int(-1, -1); cat.broken = true; }
                else alive++;
            }
            if (alive == 0) { cats.RemoveAt(c); continue; }
            for (int i = 0; i < cat.positions.Length; i++)
            {
                var p = cat.positions[i]; if (p.x < 0) continue;
                lookup[p.x,p.y].Show(cat.broken ? cat.small : cat.parts[i], 1, cat.broken ? 0 : cat.angle, CellSize);
            }
        }
    }
    public void Preview(Vector2Int[] shape, Vector2Int origin, Sprite sprite, Sprite[] catParts = null, float angle = 0)
    {
        for (int i = 0; i < shape.Length; i++)
        {
            var p = shape[i];
            lookup[p.x + origin.x, p.y + origin.y].Show(catParts == null ? sprite : catParts[i], .55f, angle, catParts == null ? CellSize * .96f : CellSize);
        }
    }
    public Vector2Int Origin(BlockPiece piece)
    {
        var local = (Vector2)transform.InverseTransformPoint(piece.FirstBlockPosition);
        var start = (Vector2)transform.InverseTransformPoint(lookup[0, 0].transform.position);
        var relative = (local - start) / cellSize;
        return new Vector2Int(Mathf.RoundToInt(relative.x), Mathf.RoundToInt(relative.y)) - piece.Shape[0];
    }
    public Vector3 DragScale(Transform parent)
    {
        var scale = transform.lossyScale; var parentScale = parent.lossyScale;
        return new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, 1);
    }
}
