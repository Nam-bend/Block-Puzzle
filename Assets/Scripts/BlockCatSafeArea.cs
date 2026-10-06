using UnityEngine;

// The authored layout stays in reference units; the background remains full bleed.
[RequireComponent(typeof(RectTransform))]
public sealed class BlockCatSafeArea : MonoBehaviour
{
    Rect lastArea;
    Vector2 lastSize;
    void LateUpdate()
    {
        var parent = transform.parent as RectTransform;
        if (parent == null || Screen.width == 0 || Screen.height == 0) return;
        var area = Screen.safeArea;
        if (area == lastArea && parent.rect.size == lastSize) return;
        lastArea = area; lastSize = parent.rect.size;
        var rect = (RectTransform)transform;
        rect.anchoredPosition = new Vector2((area.center.x / Screen.width - .5f) * parent.rect.width,
            (area.center.y / Screen.height - .5f) * parent.rect.height);
        float width = parent.rect.width * area.width / Screen.width;
        float height = parent.rect.height * area.height / Screen.height;
        rect.localScale = Vector3.one * Mathf.Min(width / 946f, height / 2048f);
    }
}
