using UnityEngine;

namespace Assets.Editor.MeshTools.Scripts
{
    // Data structure used in the persistence based scriptableObject store
    // Note:  Unity requires that this must be in it's own file.
    public class MeshData : ScriptableObject
    {
        [Header("Mesh Dimensions")]
        public int WidthSegment = 2;
        public int HeightSegment = 2;
        public float WidthWorldUnits = 1.0f;
        public float HeightWorldUnits = 1.0f;

        [Header("Plane Details")]
        public string PlaneName = "Plane";
        public string MeshName = "PlaneMesh";
        public string MeshPath = "Assets/Editor/";

 //       public Material MeshMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Editor/MeshTools/Material/Ground.mat");
        public Material MeshMaterial;
        public bool TwoSided = false;
        public bool AddCollider = true;
        public bool CreateAtOrigin = true;
        public bool HexMesh = false;

        [Header("Placement")]
        public Orientation Orientation;
        public AnchorPoint Anchor;

        // TODO: GameObjects cannot be stored in a scriptable store.  Use instantiate instead.  Remove these from the store.
        [Header("Game Objects")]
        public GameObject Plane;
        public Camera Cam;
        public Camera LastUsedCam;


    }
}

