using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SatelliteAPI : MonoBehaviour
{
    public string rootUri;
    public SatelliteRenderer satManager;
    public FutureSatellitesManager futureSatellitesManager;

    [System.Serializable]
    public class SatelliteOptions
    {
        public string example;
        public List<string> format;
        public List<string> model;
        public List<int> year;
    }

    int currentYear = -1;
    string currentModel = "";

    private void Start()
    {
        GetLiveSatellites();
        UpdateFutureOptions();
    }

    public void UpdateFutureOptions()
    {
        StartCoroutine(GetOptionsRequest());
    }

    IEnumerator GetOptionsRequest()
    {
        string uri = $"{rootUri}/sats/options";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            webRequest.SetRequestHeader("Accept-Encoding", "gzip, deflate");
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogWarning(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogWarning(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(pages[page] + ":\nReceived data");
                    //Debug.Log(webRequest.downloadHandler.text);

                    SatelliteOptions options = JsonConvert.DeserializeObject<SatelliteOptions>(webRequest.downloadHandler.text);

                    futureSatellitesManager.UpdateModelOptions(options.model);
                    break;
            }
        }
    }

    public void GetLiveSatellites()
    {
        if (currentYear != 0)
        {
            Debug.Log("Getting live satellite data");
            StartCoroutine(GetSatelliteRequest($"{rootUri}/sats?model=live&format=cartesian"));
            currentYear = 0;
        }
    }

    public void GetPredictedSatellites(string model, int year)
    {
        if (year == currentYear && model == currentModel)
        {
            Debug.Log("Ignoring update for satellites as the year and model requested are already being displayed");
        }

        if (year == 0)
        {
            GetLiveSatellites();
        }
        else
        {
            Debug.Log($"Getting satellite data using model {model} for {year} years in the future");
            StartCoroutine(GetSatelliteRequest($"{rootUri}/sats?model={model}&year={year}&format=cartesian"));
            currentYear = year;
            currentModel = model;
        }
    }

    IEnumerator GetSatelliteRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            webRequest.SetRequestHeader("Accept-Encoding", "gzip, deflate");
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogWarning(pages[page] + ": Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogWarning(pages[page] + ": HTTP Error: " + webRequest.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log(pages[page] + ":\nReceived data");

                    satManager.UpdateSatellites(JsonConvert.DeserializeObject<List<Satellite>>(webRequest.downloadHandler.text).ToArray());
                    break;
            }
        }
    }
}
