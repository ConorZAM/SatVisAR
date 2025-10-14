using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FutureSatellitesManager : MonoBehaviour
{
    public TMP_Text yearsText;
    public Slider yearsSlider;
    public SatelliteAPI satelliteAPI;
    public int yearIncrement = 5;

    public void UpdateYears()
    {
        int years = (int)yearsSlider.value * yearIncrement;
        yearsText.text = $"Years in the future: {years}";
        satelliteAPI.GetPredictedSatellites(years);
    }

    public void UpdateText(float value)
    {
        int years = (int)value * yearIncrement;
        yearsText.text = $"Years in the future: {years}";
    }
}
