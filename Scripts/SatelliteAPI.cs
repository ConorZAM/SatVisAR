using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SatelliteAPI : MonoBehaviour
{
    public string rootUri = "http://urgently-stunning-ape.ngrok-free.app/sats?";
    string liveDataUri = "http://urgently-stunning-ape.ngrok-free.app/sats?model=live&format=cartesian";
    public SatelliteManager satManager;

    int currentYear = -1;

    private void Start()
    {
        GetLiveSatellites();
    }

    public void GetLiveSatellites()
    {
        if (currentYear != 0)
        {
            Debug.Log("Getting live satellite data");
            StartCoroutine(GetSatelliteRequest($"{rootUri}model=live&format=cartesian"));
            currentYear = 0;
        }
    }

    public void GetPredictedSatellites(int year)
    {
        if (year == currentYear)
        {
            Debug.Log("Ignoring update for satellites as the year requested is already being displayed");
        }

        if (year == 0)
        {
            GetLiveSatellites();
        }
        else
        {
            Debug.Log($"Getting satellite data for {year} years in the future");
            StartCoroutine(GetSatelliteRequest($"{rootUri}model=future&year={year}&format=cartesian"));
            currentYear = year;
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
