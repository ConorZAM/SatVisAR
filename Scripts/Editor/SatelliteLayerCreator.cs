using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures the required satellite rendering layer exists in the project's TagManager.
/// Runs automatically on editor startup and after script recompilation.
/// This is an editor-only script and is excluded from builds.
/// </summary>
[InitializeOnLoad]
public static class SatelliteLayerSetup
{
    /// <summary>
    /// Name of the layer to create. Must match the renderLayerName field on SatelliteRenderer.
    /// </summary>
    const string LayerName = "Satellites";

    const int LayerIndex = 6;

    static SatelliteLayerSetup()
    {
        EnsureLayer(LayerName, LayerIndex);
    }

    static void EnsureLayer(string layerName, int layerIndex)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        SerializedProperty layersProp = tagManager.FindProperty("layers");

        // Check whether the named layer already exists at any index
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            if (layersProp.GetArrayElementAtIndex(i).stringValue == layerName)
            {
                return; // Layer already exists — nothing to do
            }
        }

        // Validate that the target slot is within bounds and is free
        if (layerIndex >= layersProp.arraySize)
        {
            Debug.LogError($"[SatelliteLayerSetup] Layer index {layerIndex} is out of range (max {layersProp.arraySize - 1}).");
            return;
        }

        SerializedProperty targetSlot = layersProp.GetArrayElementAtIndex(layerIndex);
        if (!string.IsNullOrEmpty(targetSlot.stringValue))
        {
            Debug.LogWarning($"[SatelliteLayerSetup] Layer slot {layerIndex} is already occupied by '{targetSlot.stringValue}'. " +
                             $"Cannot create '{layerName}' there. Update LayerIndex to a free slot.");
            return;
        }

        targetSlot.stringValue = layerName;
        tagManager.ApplyModifiedProperties();
        Debug.Log($"[SatelliteLayerSetup] Layer '{layerName}' created at index {layerIndex}.");
    }
}