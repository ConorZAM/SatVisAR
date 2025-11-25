using TMPro;
using UnityEngine;

public class LabelManager : MonoBehaviour
{
    public TMP_Text label;
    public InfoPanel infoPanel;
    public Satellite mySatellite;
    int satIndex;
    public ISelectionManager satManager;
    public GameObject satManagerGO;

    private void Start()
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
    }

    public void SetSatellite(Satellite satellite, int index)
    {
        mySatellite = satellite;
        satIndex = index;

        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        label.text = satellite.name;
    }

    public void Hide()
    {
        if (gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
    }

    public void ShowInfoPanel()
    {
        infoPanel.Show(this);
        transform.parent.gameObject.SetActive(false);
        satManager.SetSelection(satIndex);
    }
}
