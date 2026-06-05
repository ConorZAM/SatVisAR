using UnityEngine;

public class MetaDataView : MonoBehaviour
{
    public InfoPanel infoPanel;
    public LabelManager labelManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        infoPanel.Show(labelManager);
    }

    private void Update()
    {
        infoPanel.UpdateInfo();
    }
}
