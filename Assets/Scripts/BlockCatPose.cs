using System;
using UnityEngine;

[Serializable]
public sealed class BlockCatPose
{
    public string name, breed, expression;
    public Vector2Int[] shape;
    public Sprite[] parts;
    public Sprite small;

    public bool Match(Vector2Int[] target, out Sprite[] matched, out float angle)
    {
        matched = null; angle = 0;
        if (shape == null || parts == null || shape.Length != target.Length || parts.Length != shape.Length || small == null) return false;
        if (Array.Exists(parts, part => part == null)) return false;
        for (int turn = 0; turn < 4; turn++)
        {
            var rotated = new Vector2Int[shape.Length];
            var min = new Vector2Int(int.MaxValue, int.MaxValue);
            for (int i = 0; i < shape.Length; i++)
            {
                var point = shape[i];
                for (int t = 0; t < turn; t++) point = new Vector2Int(-point.y, point.x);
                rotated[i] = point; min = Vector2Int.Min(min, point);
            }
            for (int i = 0; i < rotated.Length; i++) rotated[i] -= min;
            var result = new Sprite[target.Length]; bool fits = true;
            for (int i = 0; i < target.Length; i++)
            {
                int index = Array.IndexOf(rotated, target[i]);
                if (index < 0) { fits = false; break; }
                result[i] = parts[index];
            }
            if (!fits) continue;
            matched = result; angle = turn * 90; return true;
        }
        return false;
    }
}
