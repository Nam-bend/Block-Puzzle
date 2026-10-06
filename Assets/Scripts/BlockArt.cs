using UnityEngine;

public sealed class BlockArt : ScriptableObject
{
    public Sprite background, empty, highlight, popup, gameOver, retry, back;
    public Sprite[] blocks;
    [Tooltip("Whole cats split into cell sprites. Leave empty to use the classic block art.")]
    public BlockCatPose[] cats;
}
