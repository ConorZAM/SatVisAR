using TMPro;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    public TMP_Text info;
    public ISelectionManager satManager;
    public GameObject listPanel;

    public GameObject satManagerGO;
    LabelManager source;

    private void Awake()
    {
        Component[] allComponents = satManagerGO.GetComponents<Component>();
        foreach (Component comp in allComponents)
        {
            if (comp is ISelectionManager manager)
            {
                satManager = manager;
                break;
            }
        }

        Hide();
    }

    public void Show(LabelManager source)
    {
        this.source = source;
        info.text = source.mySatellite.GetInfo();
        gameObject.SetActive(true);
    }

    public void UpdateInfo()
    {
        if (source == null)
        {
            return;
        }

        info.text = source.mySatellite.GetInfo();
    }

    public void Hide()
    {
        listPanel.SetActive(true);
        gameObject.SetActive(false);
        satManager.SetSelection(-1);
    }
}
