using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class BlockPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Nine block Images authored as children of this prefab.")]
    [SerializeField] Image[] blocks;
    BlockGame game;
    RectTransform rect;
    Transform homeParent;
    Vector2 home, offset;
    Vector3 homeScale;
    int pointerId;
    bool dragging;
    public Vector2Int[] Shape { get; private set; }
    public int Color { get; private set; }
    public Vector3 FirstBlockPosition => blocks[0].transform.position;
    public bool ValidateBlocks() => blocks != null && blocks.Length >= 9 && !System.Array.Exists(blocks, block => block == null);
    public void Configure(BlockGame owner, Vector2Int[] shape, int color, Sprite sprite, float scale, RectTransform slot)
    {
        game = owner; Shape = shape; Color = color;
        rect = (RectTransform)transform; homeParent = slot; home = Vector2.zero; homeScale = Vector3.one * scale;
        ResetToSlot();
        var max = Vector2Int.zero;
        foreach (var p in shape) max = Vector2Int.Max(max, p);
        var center = (Vector2)max * .5f;
        for (int i = 0; i < blocks.Length; i++)
        {
            blocks[i].gameObject.SetActive(i < shape.Length);
            if (i >= shape.Length) continue;
            blocks[i].sprite = sprite; blocks[i].color = UnityEngine.Color.white;
            blocks[i].rectTransform.anchoredPosition = ((Vector2)shape[i] - center) * game.GridView.CellSize;
        }
    }
    public void OnBeginDrag(PointerEventData data)
    {
        if (game == null || game.IsOver || dragging) return;
        dragging = true; pointerId = data.pointerId;
        homeParent = rect.parent; home = rect.anchoredPosition; homeScale = rect.localScale;
        rect.SetParent(game.DragLayer, true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(game.DragLayer, data.position, data.pressEventCamera, out var pointer);
        offset = rect.anchoredPosition - pointer;
        rect.localScale = game.GridView.DragScale(game.DragLayer); rect.SetAsLastSibling(); OnDrag(data);
    }
    public void OnDrag(PointerEventData data)
    {
        if (!dragging || data.pointerId != pointerId || game.IsOver) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(game.DragLayer, data.position, data.pressEventCamera, out var pointer);
        rect.anchoredPosition = pointer + offset + new Vector2(0, game.DragLift); game.Preview(this);
    }
    public void OnEndDrag(PointerEventData data)
    {
        if (!dragging || data.pointerId != pointerId) return;
        dragging = false;
        if (!game.Drop(this)) ReturnHome();
    }
    void ReturnHome()
    {
        ResetToSlot(); game.ClearPreview();
    }
    public void ResetToSlot()
    {
        dragging = false;
        if (rect == null || homeParent == null) return;
        rect.SetParent(homeParent, false); rect.anchoredPosition = home; rect.localScale = homeScale;
    }
    void OnApplicationFocus(bool focused)
    {
        if (!focused && dragging) { dragging = false; ReturnHome(); }
    }
}
