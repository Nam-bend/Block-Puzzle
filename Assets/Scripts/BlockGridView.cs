using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class BlockGridView : MonoBehaviour
{
    [SerializeField, Min(1)] float cellSize = 72;
    [Tooltip("64 cells with coordinates from (0,0) at bottom-left to (7,7).")]
    [SerializeField] BlockCell[] cells;
    readonly BlockCell[,] lookup = new BlockCell[BlockBoard.Size, BlockBoard.Size];
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
            cell.Show(color == 0 ? art.empty : art.blocks[color - 1]);
        }
    }
    public void Preview(Vector2Int[] shape, Vector2Int origin, Sprite sprite)
    {
        foreach (var p in shape) lookup[p.x + origin.x, p.y + origin.y].Show(sprite, .5f);
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
