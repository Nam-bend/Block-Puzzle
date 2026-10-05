using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class BlockCell : MonoBehaviour
{
    [SerializeField] Vector2Int coordinate;
    [SerializeField] Image image;
    public Vector2Int Coordinate => coordinate;
    public void SetCoordinate(Vector2Int value) => coordinate = value;
    public void Show(Sprite sprite, float opacity = 1)
    {
        image.sprite = sprite; image.color = new Color(1, 1, 1, opacity);
    }
    public bool IsConfigured => image != null;
    void Reset() { image = GetComponent<Image>(); image.raycastTarget = false; }
}
