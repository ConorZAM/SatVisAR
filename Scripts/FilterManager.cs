using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FilterManager : MonoBehaviour
{
    public TMP_Dropdown orbitTypeDropdown;
    public TMP_Dropdown ownerCountryDropdown;
    public TMP_Dropdown constellationDropdown;

    public TMP_Text[] numSatellites;

    public SatelliteRenderer satelliteRenderer;

    void UpdateNumSatellites(int number)
    {
        for (int i = 0; i < numSatellites.Length; i++)
        {
            numSatellites[i].text = $"Found {number} satellites";
        }
    }

    public void ApplyFilters()
    {
        string orbitType = orbitTypeDropdown.options[orbitTypeDropdown.value].text;
        string owner = ownerCountryDropdown.options[ownerCountryDropdown.value].text;
        string constellation = constellationDropdown.options[constellationDropdown.value].text;

        if (orbitType == "All" && owner == "All" && constellation == "All")
        {
            satelliteRenderer.filteredSatelliteIndices = new int[0];
            UpdateNumSatellites(satelliteRenderer.allSatellites.Length);
            return;
        }

        Debug.Log($"Filtering for orbit type: {orbitType}, owner: {owner}, and constellation {constellation}");

        List<int> filteredIndices = new List<int>();
        Satellite[] allSatellites = satelliteRenderer.allSatellites;
        for (int i = 0; i < allSatellites.Length; i++)
        {
            Satellite satellite = allSatellites[i];
            if (FiltersMatch(satellite.orbitType, orbitType)
                && FiltersMatch(satellite.firstOwnerCountry, owner)
                && FiltersMatch(satellite.constellation, constellation))
            {
                filteredIndices.Add(i);
            }
        }

        UpdateNumSatellites(filteredIndices.Count);
        satelliteRenderer.filteredSatelliteIndices = filteredIndices.ToArray();
    }

    bool FiltersMatch(string value, string filter)
    {
        if (filter == "All")
        {
            return true;
        }

        return value == filter;
    }

    public void UpdateFilterOptions(Satellite[] satellites)
    {
        List<string> orbitTypes = new List<string>();
        List<string> ownerCountries = new List<string>();
        List<string> constellations = new List<string>();

        for (int i = 0; i < satellites.Length; i++)
        {
            string orbit = satellites[i].orbitType;

            if (!string.IsNullOrEmpty(orbit) && !orbitTypes.Contains(orbit))
            {
                orbitTypes.Add(orbit);
            }

            string[] owners = satellites[i].ownerCountry;
            if (owners != null && owners.Length > 0)
            {
                if (!string.IsNullOrEmpty(owners[0]) && !ownerCountries.Contains(owners[0]))
                {
                    ownerCountries.Add(owners[0]);
                }
            }

            string constel = satellites[i].constellation;
            if (!string.IsNullOrEmpty(constel) && !constellations.Contains(constel))
            {
                constellations.Add(constel);
            }
        }

        ownerCountries.Sort();
        orbitTypes.Sort();
        constellations.Sort();

        ownerCountries.Insert(0, "All");
        orbitTypes.Insert(0, "All");
        constellations.Insert(0, "All");

        orbitTypeDropdown.ClearOptions();
        orbitTypeDropdown.AddOptions(orbitTypes);

        ownerCountryDropdown.ClearOptions();
        ownerCountryDropdown.AddOptions(ownerCountries);

        constellationDropdown.ClearOptions();
        constellationDropdown.AddOptions(constellations);

        UpdateNumSatellites(satellites.Length);

    }
}
