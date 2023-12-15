/// Written by Ethan Woods -> 016erw@gmail.com
/// The TerrainScaler class is responsible for horizontally and vertically scaling the terrain segments that we've created, automatically calculating and performing all of the transforms to ensure that all terrain edges line up

using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainScaler : MonoBehaviour
{
    // Scales are at baseline 1, therefore a value of 2 is double the size, etc.
    [SerializeField]
    private float terrainScaleHorizontal = 1,
        terrainScaleVertical = 1;

    // Toggleable booleans are an easy way to create an interactive inspector without having to actually build a custom panel
    [SerializeField]
    private bool scaleTerrainHorizontal = false,
        scaleTerrainVertical = false,
        resetTerrain = false;

    private ScalableTerrain[] terrainArray;

    private void Start()
    {
        terrainArray = GetTransforms(); // Storing terrain transforms in array
    }

    // Method for accessing registry key-value pairs
    public static (string, Vector3[]) GetRegistry(string key)
    {
        return (key, ValueRegistry[key]);
    }

    private void Update()
    {
        // If 'scaleTerrainHorizontal' boolean is selected, it's automatically set back to false and terrain is scaled according to float in 'terrainScaleHorizontal'
        if (scaleTerrainHorizontal)
        {
            scaleTerrainHorizontal = false;
            ScaleTerrainHorizontal(terrainScaleHorizontal);
        }

        // If 'scaleTerrainVertical' boolean is selected, it's automatically set back to false and terrain is scaled according to float in 'terrainScaleVertical'
        if (scaleTerrainVertical)
        {
            scaleTerrainVertical = false;
            ScaleTerrainVertical(terrainScaleVertical);
        }

        // If 'resetTerrain' boolean is selected, it's automatically set back to false and terrain is reset to values stored in registry
        if (resetTerrain)
        {
            resetTerrain = false;
            ResetTerrain();
        }
    }

    // Creates array of type 'ScalableTerrain' that stores current transforms of terrain objects
    private ScalableTerrain[] GetTransforms()
    {
        ScalableTerrain[] tempArray = new ScalableTerrain[gameObject.transform.childCount];
        for (int i = 0; i < tempArray.Length; i++) {
            Transform temp = gameObject.transform.GetChild(i);
            tempArray[i] = new ScalableTerrain(temp.gameObject);
        }
        return tempArray;
    }

    // Scales terrain horizontal size and position with scale factor of 'terrainScaleVertical' to accurately position terrain segments alongside each other
    private void ScaleTerrainHorizontal(float scale)
    {
        foreach (ScalableTerrain terrain in terrainArray)
        {
            terrain.TerrainController.terrainData.size = new Vector3(terrain.BaselineScale.x * terrainScaleHorizontal, terrain.TerrainController.terrainData.size.y, terrain.BaselineScale.z * terrainScaleHorizontal); ;
            terrain.TerrainObject.transform.position = new Vector3(terrain.BaselinePosition.x * terrainScaleHorizontal, terrain.BaselinePosition.y, terrain.BaselinePosition.z * terrainScaleHorizontal);
        }
    }

    // Scales terrain vertical size with scale factor of 'terrainScaleVertical'
    private void ScaleTerrainVertical(float scale)
    {
        foreach (ScalableTerrain terrain in terrainArray)
        {
            terrain.TerrainController.terrainData.size = new Vector3(terrain.TerrainController.terrainData.size.x, terrain.BaselineScale.y * terrainScaleVertical, terrain.TerrainController.terrainData.size.z);
        }
    }

    // Resets terrain to default dimensions and transforms
    private void ResetTerrain()
    {
        foreach (ScalableTerrain terrain in terrainArray)
        {
            terrain.TerrainController.terrainData.size = terrain.BaselineScale;
            terrain.TerrainObject.transform.position = terrain.BaselinePosition;
        }
    }

    /// Serves as a backup of imported values for each terrain to preserve known scale. 
    /// Could be improved to auto-record on import or by searching for all Unity terrains but not necessary so I'm not going to waste my time
    /// Each entry has a name corresponding to the terrain GameObject and a VectorConstructor() call to store position and scale vectors.
    /// This means that you have to organize the terrains into the grid and input the position values into the IDictionary manually prior to execution of scaling. 
    /// The scaling is all done in reference to these positions.
    private static IDictionary<string, Vector3[]> ValueRegistry = new Dictionary<string, Vector3[]>()
    {
        // Vector Constructor returns array of vectors of length 2, first vector is position, second is scale. ScalableTerrain constructor handles these values automatically
        { "tr", VectorConstructor(0, 65.5f, 0, 7110, 660.2433f, 7110) },
        { "tl", VectorConstructor(-7110f, -1.75f, 0, 7110, 338.7014f, 7110) },
        { "br", VectorConstructor(0, -2.6f, -7110, 7110, 369.4595f, 7110) },
        { "blbr", VectorConstructor(-3555, -12.33f, -7110, 3555, 65.28117f, 3555) },
        { "blbl", VectorConstructor(-7110, -11.63f, -7110, 3555, 21.44144f, 3555) },
        { "bltl", VectorConstructor(-7110, -10.8f, -3555, 3555, 59.69066f, 3555) },
        { "bltrtr", VectorConstructor(-1777.5f, 22.29f, -1777.5f, 1777.5f, 98.99033f, 1777.5f) },
        { "bltrtl", VectorConstructor(-3555, 7.4f, -1777.5f, 1777.5f, 45.97046f, 1777.5f) },
        { "bltrbl", VectorConstructor(-3555, -1.48f, -3555, 1777.5f, 53.24292f, 1777.5f) },
        { "bltrbr", VectorConstructor(-1777.5f, 20.18f, -3555, 1777.5f, 50.50944f, 1777.5f) }
    };

    // XYZ components of position and scale passed in, returns Vector3 array of length 2 built with these values. 
    private static Vector3[] VectorConstructor(float posX, float posY, float posZ, float scaleX, float scaleY, float scaleZ)
    {
        return new Vector3[] { new Vector3(posX, posY, posZ), new Vector3(scaleX, scaleY, scaleZ) };
    }
}

public class ScalableTerrain
{
    public GameObject TerrainObject;
    public Terrain TerrainController;
    public Vector3 BaselineScale;
    public Vector3 BaselinePosition;

    public ScalableTerrain(GameObject terrain)
    {
        TerrainController = terrain.GetComponent<Terrain>();
        TerrainObject = terrain;
        BaselinePosition = TerrainScaler.GetRegistry(TerrainObject.name).Item2[0];
        BaselineScale = TerrainScaler.GetRegistry(TerrainObject.name).Item2[1];
    }
}