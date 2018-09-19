using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIStart : MonoBehaviour
{
    private Button _btnStart = null;
    private Toggle _tglLandscape = null;

    private void Awake()
    {
        _btnStart = transform.Find("btnStart").GetComponent<Button>();
        _btnStart.onClick.AddListener(OnClickBtnStart);
        _tglLandscape = transform.Find("tglLandscape").GetComponent<Toggle>();
        _tglLandscape.onValueChanged.AddListener(OnTglLandscape);
    }

    void Start()
    {
        _tglLandscape.isOn = UITurningPage.isLandScape;
    }

    private void OnClickBtnStart()
    {
        SceneManager.LoadScene("Main");
    }
    private void OnTglLandscape(bool arg0)
    {
        UITurningPage.isLandScape = arg0;
    }
}