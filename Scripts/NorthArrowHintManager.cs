using UnityEngine;

public class NorthArrowHintManager : MonoBehaviour
{
    public GameObject northArrowHint;
    public UserLocationManager userLocationManager;

    private void OnEnable()
    {
        northArrowHint.SetActive(true);
    }

    private void OnDisable()
    {
        northArrowHint.SetActive(false);
    }

    private void Update()
    {
        northArrowHint.transform.rotation = userLocationManager.worldRotation;
    }
}
