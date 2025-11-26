using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FutureSatellitesManager : MonoBehaviour
{
    public TMP_Text yearsText;
    public Slider yearsSlider;
    public SatelliteAPI satelliteAPI;
    public int yearIncrement = 5;

    public TMP_Dropdown modelDropdown;

    public void UpdateModelOptions(List<string> options)
    {
        modelDropdown.ClearOptions();
        modelDropdown.AddOptions(options);
    }

    public void UpdateYears()
    {
        int years = (int)yearsSlider.value * yearIncrement;
        yearsText.text = $"Years in the future: {years}";
        string model = modelDropdown.options[modelDropdown.value].text;
        satelliteAPI.GetPredictedSatellites(model, years);
    }

    public void UpdateText(float value)
    {
        int years = (int)value * yearIncrement;
        yearsText.text = $"Years in the future: {years}";
    }
}
