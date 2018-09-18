using UnityEngine;

public class AdapterUtils
{
    private static AdapterUtils _instance = null;
    internal static AdapterUtils Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = new AdapterUtils();
            }
            return _instance;
        }
    }
    internal void SetVertical(Transform targetTransform)
    {
        if (targetTransform == null)
            return;
        RectTransform adapter = targetTransform.Find("Adapter").GetComponent<RectTransform>();
        adapter.localRotation = Quaternion.Euler(0, 0, 0);
        adapter.anchorMin = new Vector2(0, 0);
        adapter.anchorMax = new Vector2(1, 1);
        adapter.offsetMin = new Vector2(0, 0);
        adapter.offsetMax = new Vector2(0, 0);
    }

    internal void SetHorizontal(Transform targetTransform)
    {
        if (targetTransform == null)
            return;
        RectTransform adapter = targetTransform.Find("Adapter").GetComponent<RectTransform>();
        adapter.localRotation = Quaternion.Euler(0, 0, 0);
        adapter.anchorMin = new Vector2(0, 0);
        adapter.anchorMax = new Vector2(1, 1);
        adapter.offsetMin = new Vector2(0, 0);
        adapter.offsetMax = new Vector2(0, 0);
        Rect rect = adapter.rect;
        adapter.anchorMin = new Vector2(0.5f, 0.5f);
        adapter.anchorMax = new Vector2(0.5f, 0.5f);
        adapter.sizeDelta = new Vector2(rect.height, rect.width);
        adapter.localRotation = Quaternion.Euler(0, 0, -90);
    }
}