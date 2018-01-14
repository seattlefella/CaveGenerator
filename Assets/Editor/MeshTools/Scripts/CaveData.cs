using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Assets.Editor.MeshTools.Scripts
{
    // Data structure used in the persistence  scriptableObject store
    // Note:  Unity requires that this must be in it's own file.
    public class CaveData : ScriptableObject
    {

        // The map height and with out the border
        [Range(1, 255)]
        public int Width = 72;

        [Range(1, 255)]
        public int Height = 128;

        // The conversion to world units
        public float WidthWorldUnits = 1f;
        public float HeightWorldUnits = 1f;
        public float WallHeightWorldUnits = 1f;

        // We will limit our border to a narrow range
        [Range(1, 20)]
        public int BorderSize = 1;

        // The map that will be created.
        public int[,] Map;

        // Our random number seed
        public string Seed;
        public bool UseRandomSeed = true;

        // cellular automata's has a number of thresholds  this is just the first order
        [Range(0, 100)]
        public int RandomFillPercent = 53;

        public string CaveName = "Cave";
        public bool CreateAtOrgin = true;

        public Material CaveMaterial;
        public Material WallMaterial;
        public Material GroundMaterial;



    }
}

