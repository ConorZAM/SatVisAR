using UnityEngine;

public class MouseSelection : MonoBehaviour
{
    public SatelliteRenderer satelliteRenderer;
    public Camera cam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    bool active = true;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            active = !active;
        }

        if (!active)
        {
            return;
        }

        satelliteRenderer.SetSelectionDirection(cam.ScreenPointToRay(Input.mousePosition).direction);
    }
}
