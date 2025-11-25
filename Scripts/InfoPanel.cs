using TMPro;
using UnityEngine;

public class InfoPanel : MonoBehaviour
{
    public TMP_Text info;
    public ISelectionManager satManager;
    public GameObject listPanel;

    public GameObject satManagerGO;

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
        info.text = source.mySatellite.GetInfo();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        listPanel.SetActive(true);
        gameObject.SetActive(false);
        satManager.SetSelection(-1);
    }
}
