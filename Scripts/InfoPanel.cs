using TMPro;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    public TMP_Text info;
    public SatelliteManager satManager;
    public GameObject listPanel;

    private void Awake()
    {
        Hide();
    }

    public void Show(LabelManager source)
    {
        info.text = source.mySatellite.GetInfo();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        listPanel.SetActive(true);
        gameObject.SetActive(false);
        satManager.selectedIndex = -1;
    }
}
