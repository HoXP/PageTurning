using UnityEngine;
using UnityEngine.UI;

public class UIPageItem : MonoBehaviour
{
    private RawImage img = null;

    private void Awake()
    {
        img = transform.Find("img").GetComponent<RawImage>();
    }
    internal void Flush(Texture tex)
    {
        if(img == null)
        {
            Debug.LogError(string.Format("[Error]img == null.\n{0}",transform.parent.gameObject.name));
            return;
        }
        img.texture = tex;
    }
}