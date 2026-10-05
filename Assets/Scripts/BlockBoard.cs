using System.Collections.Generic;
using UnityEngine;

// Coordinates start at the bottom left. Resolve every full line simultaneously.
public sealed class BlockBoard
{
    public const int Size = 8;
    public readonly int[,] Cells = new int[Size, Size];
    public bool Fits(Vector2Int[] shape, Vector2Int origin)
    {
        foreach (var p in shape)
        {
            var q = p + origin;
            if (q.x < 0 || q.y < 0 || q.x >= Size || q.y >= Size || Cells[q.x, q.y] != 0) return false;
        }
        return true;
    }
    public bool HasMove(Vector2Int[] shape)
    {
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                if (Fits(shape, new Vector2Int(x, y))) return true;
        return false;
    }
    public int Place(Vector2Int[] shape, Vector2Int origin, int color, out int lines)
    {
        lines = 0;
        if (!Fits(shape, origin)) return 0;
        foreach (var p in shape) Cells[p.x + origin.x, p.y + origin.y] = color;
        var clear = new HashSet<Vector2Int>();
        for (int i = 0; i < Size; i++)
        {
            bool row = true, column = true;
            for (int j = 0; j < Size; j++) { row &= Cells[j, i] != 0; column &= Cells[i, j] != 0; }
            if (row) { lines++; for (int j = 0; j < Size; j++) clear.Add(new Vector2Int(j, i)); }
            if (column) { lines++; for (int j = 0; j < Size; j++) clear.Add(new Vector2Int(i, j)); }
        }
        foreach (var p in clear) Cells[p.x, p.y] = 0;
        return shape.Length * 10 + lines * 100 * Mathf.Max(1, lines);
    }
    public static readonly Vector2Int[][] Shapes = CreateShapes();
    static Vector2Int[][] CreateShapes()
    {
        var shapes = new List<Vector2Int[]>();
        for (int n = 1; n <= 5; n++)
        {
            var horizontal = new Vector2Int[n]; var vertical = new Vector2Int[n];
            for (int i = 0; i < n; i++) { horizontal[i] = new Vector2Int(i, 0); vertical[i] = new Vector2Int(0, i); }
            shapes.Add(horizontal); if (n > 1) shapes.Add(vertical);
        }
        shapes.Add(new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) });
        var square = new Vector2Int[9];
        for (int i = 0; i < 9; i++) square[i] = new Vector2Int(i % 3, i / 3);
        shapes.Add(square);
        var seeds = new[] {
            new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(1,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) }
        };
        foreach (var seed in seeds)
        {
            var current = seed;
            for (int turn = 0; turn < 4; turn++)
            {
                shapes.Add(current);
                var next = new Vector2Int[current.Length]; var min = new Vector2Int(int.MaxValue, int.MaxValue);
                for (int i = 0; i < next.Length; i++) { next[i] = new Vector2Int(-current[i].y, current[i].x); min = Vector2Int.Min(min, next[i]); }
                for (int i = 0; i < next.Length; i++) next[i] -= min;
                current = next;
            }
        }
        return shapes.ToArray();
    }
}
