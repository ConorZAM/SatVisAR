using Newtonsoft.Json;
using System;
using UnityEngine;

[System.Serializable]
public class Satellite
{
    const float GM = 3.98e5f;
    public const float earthRadius = 6378f;
    public Vector3 positionITRS; // In km
    public Vector3 velocityITRS; // In km/s

    public string name;
    //public string category;

    [JsonProperty("launch date")]
    public DateTime launchDate;

    [JsonProperty("launch site")]
    public string launchSite;

    [JsonProperty("launch country")]
    public string launchCountry;

    [JsonProperty("operational status")]
    public string operationalStatus;

    [JsonProperty("orbit type")]
    public string orbitType;

    [JsonProperty("owner country")]
    public string[] ownerCountry;

    public string firstOwnerCountry = "";

    public string constellation;

    //public string Payload;

    //public string[] tags;

    //public double e;
    //public double i;

    public float x, y, z;
    public float x_v, y_v, z_v;

    public Satellite()
    {

    }

    public Satellite(string info, float x, float y, float z, float vx, float vy, float vz)
    {
        positionITRS = new Vector3(x, y, z);
        velocityITRS = new Vector3(vx, vy, vz);
        //satelliteInfo = info;
    }

    public Satellite(string name, string info, Vector3 position, Vector3 velocity)
    {
        positionITRS = position;
        velocityITRS = velocity;
        this.name = name;
    }

    public void Initialise()
    {
        positionITRS = new Vector3(x, z, y);
        velocityITRS = new Vector3(x_v, z_v, y_v);

        if (ownerCountry != null && ownerCountry.Length > 0)
        {
            firstOwnerCountry = ownerCountry[0];
        }
        else
        {
            firstOwnerCountry = "Unknown";
        }
    }

    public void UpdatePosition(float dt)
    {
        float r = 1f / positionITRS.magnitude;
        velocityITRS += -dt * (GM * r * r * r) * positionITRS;
        positionITRS += dt * velocityITRS;
    }

    public string GetInfo()
    {
        return $"Name: {name}\nOwned by: {firstOwnerCountry}\nCountry of launch: {launchCountry}\nLaunch date: {launchDate.ToShortDateString()}\nOperational status: {operationalStatus}\nOrbit type: {orbitType}\nApprox. altitude {ApproxAltitude().ToSignificantFigures(2)} km";
    }

    public float ApproxAltitude()
    {
        return positionITRS.magnitude - earthRadius;
    }
}