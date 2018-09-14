using UnityEngine;
using UnityEngine.UI;

public class UIPageItem : MonoBehaviour
{
    private RawImage img = null;

    private void Awake()
    {
        img = transform.Find("content/img").GetComponent<RawImage>();
    }
    internal void Flush(Texture tex)
    {
        if(img == null)
        {
            return;
        }
        img.texture = tex;
    }
}