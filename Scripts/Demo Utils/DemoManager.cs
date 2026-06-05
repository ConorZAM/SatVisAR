using TMPro;
using UnityEngine;

public class DemoManager : MonoBehaviour
{
    [Header("Info Panel")]
    public KeyCode toggleInfoPanelKey = KeyCode.I;
    public GameObject infoPanel;

    [Header("Filters")]
    public KeyCode showStarlinkKey = KeyCode.S;
    public KeyCode showUkKey = KeyCode.U;
    public KeyCode clearFiltersKey = KeyCode.C;
    public FilterManager filterManager;

    [Header("Orbit Types")]
    public KeyCode leoKey = KeyCode.L;
    public KeyCode meoKey = KeyCode.M;
    public KeyCode geoKey = KeyCode.G;

    [Header("Futures")]
    public KeyCode currentSatellitesKey = KeyCode.Alpha1;
    public KeyCode spaceWinterKey = KeyCode.Alpha2;
    public KeyCode intensiveUseKey = KeyCode.Alpha3;
    public TMP_Dropdown futuresDropdown;
    public FutureSatellitesManager futureSatellitesManager;

    [Header("Collision")]
    public KeyCode toggleCollisionKey = KeyCode.H;
    public SatelliteRenderer satelliteRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (infoPanel == null)
        {
            Debug.LogError("Info Panel GameObject is not assigned in the DemoManager.");
        }
        infoPanel.SetActive(false);

        if (filterManager == null)
        {
            Debug.LogError("FilterManager is not assigned in the DemoManager.");
        }

        if (futuresDropdown == null)
        {
            Debug.LogError("Futures dropdown is not assigned in the DemoManager");
        }

        if (futureSatellitesManager == null)
        {
            Debug.LogError("Future satellites manager is not assigned in the DemoManager");

        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(toggleInfoPanelKey))
        {
            infoPanel.SetActive(!infoPanel.activeSelf);
        }

        if (Input.GetKeyUp(showStarlinkKey))
        {
            filterManager.ApplyFilters("All", "All", "STARLINK");
        }

        if (Input.GetKeyUp(showUkKey))
        {
            filterManager.ApplyFilters("All", "United Kingdom", "All");
        }

        if (Input.GetKeyUp(clearFiltersKey))
        {
            filterManager.ApplyFilters("All", "All", "All");
        }

        if (Input.GetKeyUp(leoKey))
        {
            filterManager.ApplyFilters("LEO", "All", "All");
        }

        if (Input.GetKeyUp(meoKey))
        {
            filterManager.ApplyFilters("MEO", "All", "All");
        }

        if (Input.GetKeyUp(geoKey))
        {
            filterManager.ApplyFilters("GEO", "All", "All");
        }

        if (Input.GetKeyUp(currentSatellitesKey))
        {
            futureSatellitesManager.UpdateYears(futuresDropdown.options[0].text, 0);
        }

        if (Input.GetKeyUp(spaceWinterKey))
        {
            futureSatellitesManager.UpdateYears("sep3m: space winter", 50);
        }

        if (Input.GetKeyUp(intensiveUseKey))
        {
            futureSatellitesManager.UpdateYears("sep6h: intense growth and sustainability", 50);
        }

        if (Input.GetKeyUp(toggleCollisionKey))
        {
            if (satelliteRenderer.colorMode == SatelliteRenderer.SatelliteColorMode.CollisionRisk)
            {
                satelliteRenderer.SetColorMode(SatelliteRenderer.SatelliteColorMode.Custom);

            }
            else
            {

                satelliteRenderer.SetColorMode(SatelliteRenderer.SatelliteColorMode.CollisionRisk);
            }
        }
    }
}
