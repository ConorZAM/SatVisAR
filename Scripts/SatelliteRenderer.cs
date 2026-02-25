using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is responsible for maintaining the satellites in the simulation.
/// There are a few jobs assigned to the class which could be split into separate managers for ease of use.
/// Jobs include:
/// - Simple Euler forward integration of all satellite positions
/// - FOV culling using camera direction and satellite positions to ensure only visible satellites are drawn (reduces computation cost of drawing unnecessary satellites)
/// - Drawing of satellites using simple billboard and texture, note satellites don't exist as game objects as the overheads would be too expensive with 50,000+ objects
/// - Exposing public functions for UI tools to adjust rendering of satellites. Future work will also filter which satellites should be rendered.
/// </summary>
public class SatelliteRenderer : MonoBehaviour, ISelectionManager
{
    // Satellites contain a lot of information, including their positions.
    // However, it is faster to cache frequently used data so we don't have to access satellite instances during the rendering
    public Satellite[] allSatellites = new Satellite[0];

    public Satellite[] filteredSatellites = new Satellite[0];

    /// <summary>
    /// World space positions of satellites
    /// </summary>
    private Vector3[] currentPositions;

    /// <summary>
    /// Direction from the camera to each satellite - dot product of this direction and the camera's forward direction gives us their alignment
    /// and is used to cull satellites outside the camera's FOV
    /// </summary>
    private Vector3[] directions;

    /// <summary>
    /// Rotation of all satellites
    /// </summary>
    private Quaternion[] satRotations;

    /// <summary>
    ///  Random rotation offsets make the simulation look a bit more real than having all satellites perfectly aligned
    /// </summary>
    private float[] satRotationOffsets;

    /// <summary>
    /// Mesh used to render the satellites
    /// </summary>
    private Mesh quadMesh;

    [Header("Satellite Rendering")]
    public Texture2D[] satelliteTextures;
    public Material satelliteMaterial;
    public Material labelledSatelliteMaterial;
    public Material selectedSatelliteMaterial;
    public float quadSize = 1f;
    public float satelliteSize = 0.05f;
    public string renderLayerName;
    int renderLayer;

    public UserLocationManager userLocationManager;

    /// <summary>
    /// Fixed distance used to render satellites - this is an optimisation for the AR app and a cheap way to avoid issues with floating point precision
    /// Individual atellite sizes can be adjusted to give the impression of depth/distance
    /// </summary>
    public float shellDistance = 10f;

    [Header("Oclussion Culling")]
    public float cullFOV = 60f; // Camera field of view in degrees
    /// <summary>
    /// Minimum result of the dot product between satellite direction and camera forward direction.
    /// Satellites further away from the camera's centre will be ignored when checking for satellites to label.
    /// This value could be changed to a FOV value and then the value Mathf.Cos(FOV * 0.5f * Mathf.Deg2Rad) would be compared with dot products
    /// </summary>
    public float labelCull = 0.97f;

    /// <summary>
    /// No longer used, was the name for the text file containing satellite information
    /// </summary>
    string fileName = "satelliteData";

    public int selectedIndex = -1;

    /// <summary>
    /// If this is true then satellites that are below the viewer's horizon will not be rendered.
    /// This helps to avoid the mess of satellites seen when looking at the ground.
    /// Because those satellites are further away and we are not using any size-depth relationship currently, looking down reveals a large concentration of satellites
    /// </summary>
    public bool hideBelowHorizon = false;

    public int numLabels = 5;

    const int batchSize = 1023;
    int BaseColorId; // use "_Color" if your shader uses that name
    readonly Matrix4x4[] matrixBatch = new Matrix4x4[batchSize];
    readonly Vector4[] colorBatch = new Vector4[batchSize];
    readonly List<Vector4> colorList = new List<Vector4>(batchSize);
    MaterialPropertyBlock instancingBlock;

    private void Awake()
    {
        instancingBlock = new MaterialPropertyBlock();
        BaseColorId = Shader.PropertyToID("_BaseColor");
    }

    [Tooltip("The label prefab which is created in the UI. numLabels determines how many of these are created at runtime.")]
    public GameObject labelGameObject;
    LabelManager[] labels;

    private List<Matrix4x4[]> instanceBatches = new();

    public Camera cam;
    public Transform cameraDirection;

    #region Unity Functions
    void Start()
    {
        renderLayer = LayerMask.NameToLayer(renderLayerName);
        //cameraDirection = cam.transform;

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.Portrait;

        quadMesh = GenerateBillboardQuad();

        labels = new LabelManager[numLabels];
        labels[0] = labelGameObject.GetComponent<LabelManager>();
        for (int i = 0; i < numLabels - 1; i++)
        {
            labels[i + 1] = Instantiate(labelGameObject, labelGameObject.transform.parent).GetComponent<LabelManager>();
        }

        SetSatelliteTexture(0);
    }

    /// <summary>
    /// dt is stored as Unity has some overhead on Time.fixedDeltaTime so we don't want to keep calling it in the same update loop.
    /// </summary>
    float dt;

    // Fixed update is for physics simulation as it maintains a constant (fixed) time step
    private void FixedUpdate()
    {
        dt = Time.fixedDeltaTime;
        //Transform cameraTransform = Camera.main.transform;
        Quaternion rotation = cameraDirection.rotation;
        Vector3 cameraForward = cameraDirection.forward;
        for (int i = 0; i < allSatellites.Length; i++)
        {
            // Integrate orbit (not super accurate, but works for short intervals)
            allSatellites[i].UpdatePosition(dt);

            // Get the satellite's relative position to the observer
            currentPositions[i] = allSatellites[i].positionITRS - userLocationManager.userPositionECEF;

            // Convert to direction
            directions[i] = Vector3.Normalize(currentPositions[i]);

            // Trying some rotation but janky rn
            satRotations[i] = Quaternion.AngleAxis(satRotationOffsets[i], cameraForward) * rotation;
            //satRotations[i] = Quaternion.AngleAxis(satRotationOffsets[i], dir) * Quaternion.LookRotation(dir);
        }
    }

    float cosFOV;
    float[] labelDots;
    int[] labelIdx;
    //public TMP_Text debugPanel;

    // Update is used for rendering, Unity will aim for the highest possible frames per second but may also lag/slow down.
    // This means that the delta time (Time.deltaTime) between Update calls can vary significantly, making physics simulations unreliable and potentially random
    private void Update()
    {
        //debugPanel.text = $"Location status: {alignmentResult}\nInitial compass heading: {initialHeading}\nLat: {viewerLat}\nLon: {viewerLon}\n" +
        //    $"Alt: {viewerAlt}\nOrigin ea_x: {origin.transform.eulerAngles.x}\nOrigin ea_y: {origin.transform.eulerAngles.y}\nOrigin ea_z: {origin.transform.eulerAngles.z}\n" +
        //    $"ECEF x: {observerPositionECEF.x}\nECEF y: {observerPositionECEF.y}\nECEF z: {observerPositionECEF.z}\n";

        // Here we need some kind of transformation based on location so that the camera forward direction isn't just
        // working as though we're in the middle of the world and aligned with the ECEF coordinate frame
        // Shoooouuld be a pretty easy adjustment.
        // Also, I'm not sure if we want to offset the positions of the satellites...
        // There's an entire diameter of earth in there as well that would change what we can see!

        //Vector3 camForward = cam.transform.forward;

        //Quaternion rotation = camera.rotation;


        if (filteredSatellites.Length > 0)
        {
            RenderSatellites(filteredSatellites);
        }
        else
        {
            RenderSatellites(allSatellites);
        }

        // Next step here is to get satellites within a really small fov and label them
        // If we can get n satellites closest to a dot product of 1 then we're onto a winner
        // Then we want to colour code them and have a UI element which writes labels according to the satellite names.
        // Note that i should correspond to the satellite array!

    }

    struct SatelliteMatrix
    {
        public Matrix4x4 matrix;
        public float dot;
        public int index;
        public Color colour;
    }

    public Material[] collisionMaterials;
    public float[] collisionThresholds = new float[] { 1, 1e-2f, 1e-4f, 1e-6f };

    List<SatelliteMatrix> visibleSatellites = new();
    SatelliteMatrix[] labelledSatellites;

    /// <summary>
    /// World space positions of satellites
    /// </summary>
    private Matrix4x4[] labelledMatricesBuffer;
    private int labelledSatelliteCount;


    Vector3 cameraForward;
    Vector3 camPos;
    List<SatelliteMatrix> GetSatelliteMatrices(Satellite[] satellites)
    {
        visibleSatellites.Clear();

        if (labelledSatellites == null || labelledSatellites.Length != numLabels)
        {
            labelledSatellites = new SatelliteMatrix[numLabels];
        }

        
        labelDots = new float[numLabels];
        labelIdx = new int[numLabels];
        for (int i = 0; i < numLabels; i++)
        {
            labelDots[i] = 1;
            labelIdx[i] = -1;
        }

        float furthestDot = 0;
        int furthestDotIndex = 0;
        int labelCount = 0;

        for (int i = 0; i < satellites.Length; i++)
        {
            Vector3 dir = directions[i];
            if (hideBelowHorizon && Vector3.Dot(dir, userLocationManager.userPositionECEF) < 0)
            {
                continue;
            }

            float dot = Vector3.Dot(cameraForward, dir);
            float selectionDot = Vector3.Dot(selectionDirection, dir);

            if (dot > cosFOV)
            {
                SatelliteMatrix mtx = new SatelliteMatrix
                {
                    matrix = Matrix4x4.TRS(camPos + (dir * shellDistance), satRotations[i], satelliteSize * sizeScales[i] * Vector3.one),
                    dot = dot,
                    index = i,
                    colour = Color.white //GetSatelliteColour(satellites[i])
                };

                if (labelCount < numLabels)
                {
                    // Just add the new label
                    labelledSatellites[labelCount] = mtx;
                    labelDots[labelCount] = selectionDot;
                    labelIdx[labelCount] = i;

                    if (selectionDot < furthestDot)
                    {
                        furthestDot = selectionDot;
                        furthestDotIndex = labelCount;
                    }

                    labelCount++;
                }
                else
                {
                    // If we have a satellite closer than our furthest label so far, replace it
                    if (selectionDot > furthestDot)
                    {
                        // Move the previous furthest over to the regular rendering list
                        visibleSatellites.Add(labelledSatellites[furthestDotIndex]);

                        // Replace
                        labelledSatellites[furthestDotIndex] = mtx;
                        labelDots[furthestDotIndex] = selectionDot;
                        labelIdx[furthestDotIndex] = i;

                        // Update the furthest info
                        furthestDot = 1f;
                        for (int l = 0; l < labelCount; l++)
                        {
                            if (labelDots[l] < furthestDot)
                            {
                                furthestDot = labelDots[l];
                                furthestDotIndex = l;
                            }
                        }
                    }
                    else
                    {
                        visibleSatellites.Add(mtx);
                    }
                }
            }
        }

        labelledSatelliteCount = labelCount;
        return visibleSatellites;
    }

    public float minFluxDebrisDensity = 6.779351e-06f;
    public float midFluxDebrisDensity = 0.5f * (6.779351e-06f + 0.0001862074f);
    public float maxFluxDebrisDensity = 0.0001862074f;
    Color GetSatelliteColour(Satellite satellite)
    {
        // This would be ideal but it doesn't seem to give much difference
        float t = Mathf.InverseLerp(minFluxDebrisDensity, maxFluxDebrisDensity, satellite.maxFluxDebrisDensity);
        return Color.Lerp(Color.blue, Color.red, t);
    }


    /// <summary>
    /// This is actually the worst wow...
    /// </summary>
    void RenderSatellites(Satellite[] satellites)
    {
        Vector3 cameraForward = cameraDirection.forward;
        //cam.transform.rotation = rotation;

        if (!hasSelectionDirection)
        {
            selectionDirection = cameraForward;
        }

        Vector3 camPos = cameraDirection.position;
        cosFOV = Mathf.Cos(cullFOV * 0.5f * Mathf.Deg2Rad);

        GetSatelliteMatrices(satellites);

        // Batch draw calls with per-instance color
        for (int i = 0; i < visibleSatellites.Count; i += batchSize)
        {
            int count = Mathf.Min(batchSize, visibleSatellites.Count - i);
            colorList.Clear();
            for (int j = 0; j < count; j++)
            {
                SatelliteMatrix sat = visibleSatellites[i + j];
                matrixBatch[j] = sat.matrix;
                colorBatch[j] = sat.colour;
                colorList.Add(colorBatch[j]);
            }

            instancingBlock.Clear();
            instancingBlock.SetVectorArray(BaseColorId, colorList);

            Graphics.DrawMeshInstanced(
                quadMesh, 0, satelliteMaterial,
                matrixBatch, count, instancingBlock,
                UnityEngine.Rendering.ShadowCastingMode.Off, false, renderLayer);
        }

        // Draw the labelled satellites!
        if (labelledSatelliteCount > 0)
        {
            if (labelledMatricesBuffer == null || labelledMatricesBuffer.Length != numLabels)
            {
                labelledMatricesBuffer = new Matrix4x4[numLabels];
            }

            for (int i = 0; i < labelledSatelliteCount; i++)
            {
                labelledMatricesBuffer[i] = labelledSatellites[i].matrix;
            }

            Graphics.DrawMeshInstanced(
                quadMesh,
                0,
                labelledSatelliteMaterial,
                labelledMatricesBuffer,
                labelledSatelliteCount,
                null,
                UnityEngine.Rendering.ShadowCastingMode.Off,
                false,
                renderLayer);
        }

        // Draw the selected satellite
        if (selectedIndex > 0)
        {
            Graphics.DrawMesh(quadMesh, Matrix4x4.TRS(camPos + (directions[selectedIndex] * shellDistance), satRotations[selectedIndex], Vector3.one * satelliteSize), selectedSatelliteMaterial, renderLayer);
        }

        // Update the labels
        // Sorting the IDs so we have more consistent labels
        Array.Sort(labelIdx);
        for (int i = 0; i < numLabels; i++)
        {
            if (labelIdx[i] > -1)
            {
                labels[i].SetSatellite(allSatellites[labelIdx[i]], labelIdx[i]);
            }
            else
            {
                labels[i].Hide();
            }
        }
    }

    Vector3 selectionDirection;
    bool hasSelectionDirection = false;
    public void SetSelectionDirection(Vector3 direction)
    {
        selectionDirection = direction;
        hasSelectionDirection = true;
    }

    public void ClearSelectionDirection()
    {
        hasSelectionDirection = false;
    }

    /// <summary>
    /// This is actually the worst wow...
    /// </summary>
    void DoTheRest(int[] satellites)
    {
        Vector3 cameraForward = cameraDirection.forward;

        if (!hasSelectionDirection)
        {
            selectionDirection = cameraForward;
        }

        //cam.transform.rotation = rotation;

        Vector3 camPos = cameraDirection.position;
        cosFOV = Mathf.Cos(cullFOV * 0.5f * Mathf.Deg2Rad);

        List<Matrix4x4> visibleMatrices = new();

        // New info for labelling
        Matrix4x4[] labelledMatrices = new Matrix4x4[numLabels];
        labelDots = new float[numLabels];
        labelIdx = new int[numLabels];
        for (int i = 0; i < numLabels; i++)
        {
            labelDots[i] = 1;
            labelIdx[i] = -1;
        }

        float furthestDot = 0;
        int furthestDotIndex = 0;
        int labelCount = 0;

        Vector3 renderPos;
        Vector3 userPositionECEF = userLocationManager.userPositionECEF;
        Matrix4x4 mtx;

        for (int i = 0; i < satellites.Length; i++)
        {
            int idx = satellites[i];
            Vector3 dir = directions[idx];

            /* Sorting the satellites based on nearest to the camera direction
             * I think we can have an array of nearestMatrices and an array of their dot product results
             * If we get a satellite which is closer, then we pop out the furthest satellite from the nearestMatrices array and add it
             * to the visibleMatrices array.
             * 
             * The potential issue here is that we might be searching the array of labelled satellites a lot unecessarily..
             */

            if (hideBelowHorizon && Vector3.Dot(dir, userPositionECEF) < 0)
            {
                continue;
            }

            if (idx == selectedIndex)
            {
                continue;
            }

            float dot = Vector3.Dot(cameraForward, dir);
            float selectionDot = Vector3.Dot(selectionDirection, dir);

            // Lazy way to filter for sats that are close to the centre of view
            if (selectionDot > labelCull)
            {
                renderPos = camPos + (dir * shellDistance);
                //mtx = Matrix4x4.TRS(renderPos, Quaternion.LookRotation(dir), Vector3.one * 0.05f);
                mtx = Matrix4x4.TRS(renderPos, satRotations[idx], satelliteSize * sizeScales[idx] * Vector3.one);

                if (labelCount < numLabels)
                {
                    // Just add the new label
                    labelledMatrices[labelCount] = mtx;
                    labelDots[labelCount] = selectionDot;
                    labelIdx[labelCount] = idx;

                    if (selectionDot < furthestDot)
                    {
                        furthestDot = selectionDot;
                        furthestDotIndex = labelCount;
                    }

                    labelCount++;
                }
                else
                {
                    // If we have a satellite closer than our furthest label so far, replace it
                    if (selectionDot > furthestDot)
                    {
                        // Move the previous furthest over to the regular rendering list
                        visibleMatrices.Add(labelledMatrices[furthestDotIndex]);

                        // Replace
                        labelledMatrices[furthestDotIndex] = mtx;
                        labelDots[furthestDotIndex] = selectionDot;
                        labelIdx[furthestDotIndex] = idx;

                        // Update the furthest info
                        furthestDot = 1f;
                        for (int l = 0; l < labelCount; l++)
                        {
                            if (labelDots[l] < furthestDot)
                            {
                                furthestDot = labelDots[l];
                                furthestDotIndex = l;
                            }
                        }
                    }
                    else
                    {
                        visibleMatrices.Add(mtx);
                    }
                }
            }
            // Just need to check if the satellite is within the camera's FOV
            else if (dot > cosFOV)
            {
                renderPos = camPos + (dir * shellDistance);
                mtx = Matrix4x4.TRS(renderPos, satRotations[idx], satelliteSize * sizeScales[idx] * Vector3.one);
                visibleMatrices.Add(mtx);
            }
        }

        // Batch draw calls
        instanceBatches.Clear();
        for (int i = 0; i < visibleMatrices.Count; i += batchSize)
        {
            int count = Mathf.Min(batchSize, visibleMatrices.Count - i);
            instanceBatches.Add(visibleMatrices.GetRange(i, count).ToArray());
        }

        foreach (Matrix4x4[] batch in instanceBatches)
        {
            //Graphics.DrawMeshInstanced(rp, quadMesh, 0, batch);
            Graphics.DrawMeshInstanced(quadMesh, 0, satelliteMaterial, batch, batch.Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, renderLayer);
        }

        // Draw the labelled satellites!
        Graphics.DrawMeshInstanced(quadMesh, 0, labelledSatelliteMaterial, labelledMatrices, labelledMatrices.Length, null, UnityEngine.Rendering.ShadowCastingMode.Off, false, renderLayer);

        // Draw the selected satellite
        if (selectedIndex > 0)
        {
            renderPos = camPos + (directions[selectedIndex] * shellDistance);
            mtx = Matrix4x4.TRS(renderPos, satRotations[selectedIndex], Vector3.one * satelliteSize);
            Graphics.DrawMesh(quadMesh, mtx, selectedSatelliteMaterial, renderLayer);
        }

        // Update the labels
        // Sorting the IDs so we have more consistent labels
        Array.Sort(labelIdx);
        for (int i = 0; i < numLabels; i++)
        {
            if (labelIdx[i] > -1)
            {
                labels[i].SetSatellite(allSatellites[labelIdx[i]], labelIdx[i]);
            }
            else
            {
                labels[i].Hide();
            }
        }
    }

    #endregion Unity Functions

    #region UI Functions

    public void SetSelection(int selection)
    {
        selectedIndex = selection;
    }

    public void SetSatelliteSize(float size)
    {
        satelliteSize = size;
    }

    public void SetSatelliteTexture(int selectionIndex)
    {
        satelliteMaterial.mainTexture = satelliteTextures[selectionIndex];
        labelledSatelliteMaterial.mainTexture = satelliteTextures[selectionIndex];
        selectedSatelliteMaterial.mainTexture = satelliteTextures[selectionIndex];
    }

    /// <summary>
    /// Yes I hate this too but it makes more sense for the user to see show rather than hide and it's saving the not operation in the loop
    /// I'm stupid for my optimisations but here we are
    /// </summary>
    public void SetShowBelowHorizon(bool showBelowHorizon)
    {
        hideBelowHorizon = !showBelowHorizon;
    }

    #endregion UI Functions

    #region Loading Satellites

    public float leoSizeScale = 1f;
    public float meoSizeScale = 0.7f;
    public float geoSizeScale = 0.5f;
    public float heoSizeScale = 0.3f;
    public float undefinedOrbitTypeSizeScale = 1f;
    float[] sizeScales;

    public FilterManager filterManager;
    public void UpdateSatellites(Satellite[] satellites)
    {
        allSatellites = satellites;

        currentPositions = new Vector3[satellites.Length];
        directions = new Vector3[satellites.Length];
        satRotations = new Quaternion[satellites.Length];
        satRotationOffsets = new float[satellites.Length];
        sizeScales = new float[satellites.Length];

        float minFluxDensity = float.MaxValue;
        float maxFluxDensity = float.MinValue;

        // Initialize currentPositions with initial positions
        for (int i = 0; i < satellites.Length; i++)
        {
            satellites[i].Initialise();
            currentPositions[i] = satellites[i].positionITRS;

            // If the satellite doesn't have an orbit type we should infer the type from the altitude and update the orbit type property
            if (satellites[i].orbitType != "LEO" && satellites[i].orbitType != "MEO" && satellites[i].orbitType != "GEO" && satellites[i].orbitType != "HEO")
            {
                float altitude = satellites[i].ApproxAltitude();
                if (altitude < 2000)
                {
                    satellites[i].orbitType = "LEO";
                }
                else if (altitude < 35786 + 2000)
                {
                    satellites[i].orbitType = "MEO";
                }
                else
                {
                    satellites[i].orbitType = "GEO";
                }
            }

            sizeScales[i] = satellites[i].orbitType switch
            {
                "LEO" => leoSizeScale,
                "MEO" => meoSizeScale,
                "GEO" => geoSizeScale,
                "HEO" => heoSizeScale,
                _ => undefinedOrbitTypeSizeScale,
            };
            satRotationOffsets[i] = UnityEngine.Random.Range(-180f, 180f);

            minFluxDensity = Mathf.Min(minFluxDensity, satellites[i].maxFluxDebrisDensity);
            maxFluxDensity = Mathf.Max(maxFluxDensity, satellites[i].maxFluxDebrisDensity);
        }

        Debug.Log($"Min flux density: {minFluxDensity}, Max flux density: {maxFluxDensity}");

        filterManager.UpdateFilterOptions(allSatellites);

        Debug.Log($"Loaded {satellites.Length} satellites");
    }

    /// <summary>
    /// Previous method used to load satellite data from text file - manager now uses the SatelliteAPI to get data from the server instead.
    /// </summary>
    void LoadSatelliteData()
    {
        //if (!File.Exists(fileName))
        //{
        //    Debug.LogWarning("Could not find file!");
        //    return;
        //}

        TextAsset text = Resources.Load(fileName) as TextAsset;

        string[] data = text.text.Split('\n');
        int numData = data.Length - 1;

        List<Satellite> satellitesList = new List<Satellite>();

        // Can probably just create satellites instead of storing data on them.
        // Also note we're skipping the header here

        Debug.Log("File contains " + numData + " satellites.");

        for (int i = 0; i < data.Length - 1; i++)
        {
            // Skip the header
            string[] line = data[i + 1].Split(',');
            Vector3 position = new Vector3(float.Parse(line[2]), float.Parse(line[4]), float.Parse(line[3]));

            if (position.sqrMagnitude > 0.0001f)
            {
                Vector3 velocity = new Vector3(float.Parse(line[5]), float.Parse(line[7]), float.Parse(line[6]));
                satellitesList.Add(new Satellite(line[0], line[1], position, velocity));
                //satellites[(x * numData) + i] = new Satellite(line[0], position, velocity);
            }
        }

        allSatellites = satellitesList.ToArray();

        Debug.Log($"Found {allSatellites.Length} valid satellite positions");
    }
    #endregion Loading Satellites

    #region Mesh Generation
    Mesh GenerateBillboardQuad()
    {
        Mesh mesh = new Mesh();

        Vector3[] verts = new Vector3[4]
        {
            new Vector3(-quadSize, -quadSize, 0),
            new Vector3(quadSize, -quadSize, 0),
            new Vector3(-quadSize, quadSize, 0),
            new Vector3(quadSize, quadSize, 0)
        };
        mesh.vertices = verts;

        int[] tris = new int[6]
          {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
          };
        mesh.triangles = tris;

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
    #endregion
}
