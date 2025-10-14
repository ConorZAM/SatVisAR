using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SatelliteManager))]
public class SatManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Do orientation update"))
        {
            ((SatelliteManager)target).UpdateOrientation();
        }
    }
}
