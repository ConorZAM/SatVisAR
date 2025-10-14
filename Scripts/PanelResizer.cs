using UnityEngine;
using UnityEngine.EventSystems;

public class PanelResizer : MonoBehaviour, IDragHandler
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private Vector2 minSize = new Vector2(100, 100);
    [SerializeField] private Vector2 maxSize = new Vector2(1000, 1000);

    private Canvas canvas;

    private void Awake()
    {
        canvas = panel.GetComponentInParent<Canvas>();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (panel == null || canvas == null)
        {
            return;
        }

        // Convert to UI units
        Vector2 delta = eventData.delta / canvas.scaleFactor;

        // Pivot = (1,0), bottom-right fixed
        Vector2 newSize = panel.sizeDelta - new Vector2(delta.x, -delta.y);

        // Clamp
        newSize.x = Mathf.Clamp(newSize.x, minSize.x, maxSize.x);
        newSize.y = Mathf.Clamp(newSize.y, minSize.y, maxSize.y);

        panel.sizeDelta = newSize;
    }
}