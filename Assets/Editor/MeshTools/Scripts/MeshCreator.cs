using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.MeshTools.Scripts
{

    // TODO: Convert the mesh gen logic into a algorithm pattern


    [System.Serializable]
    public class MeshCreator : UnityEditor.Editor, ICreateMesh
    {
        [SerializeField]
        private int _widthSegment;
        [SerializeField]
        private int _heightSegment;

        [SerializeField]
        private float _widthWorldUnits;
        [SerializeField]
        private float _heightWorldUnits;

        [SerializeField]
        private bool _twoSided;

        [SerializeField]
        private GameObject _plane;
        [SerializeField]
        private string _planeName;

        [SerializeField]
        private string _meshName;
        [SerializeField]
        private string _meshPath;
        [SerializeField]
        private Material _meshMaterial;
        [SerializeField]
        private bool _addCollider;
        [SerializeField]
        private bool _hexMesh;


        [SerializeField]
        private bool _createAtOrigin;
        [SerializeField]
        private Orientation _orientation;
        [SerializeField]
        private AnchorPoint _anchor;

        [SerializeField]
        private Camera _cam;
        [SerializeField]
        private Camera _lastUsedCam;

        // --------------------Calculated values needed throughout the class------------------
        // Every segment has two vertices's  so total count is segments + 1
        private int _vCountX;
        private int _vCountY;

        // The size of every segment in world space units
        private float _vSizeX;
        private float _vSizeY;

        // UV factor is equal to....
        private float _uvFactorX;
        private float _uvFactorY;

        // We will use the origin as the offset to put the center of the mesh at 0,0,0
        private Vector3 _origin;

        //The tangent associated with every vertex
        private Vector4 _tangent = new Vector4(1f, 0f, 0f, -1f);

        // The mesh will need a user selectable anchor point
        private Anchor _getAnchor;
        private Vector2 _offset;
        private string _offsetId;

        // The different file paths that are needed
        private string _storageObjectPath = "Assets/Editor/MeshTools/ObjectStorage/MeshData.asset";

        // The list's that will contain all of the vertices's data
        private List<Vector3> verticies = new List<Vector3>();
        private List<Vector4> tangents = new List<Vector4>();
        private List<Vector2> uVs = new List<Vector2>();

        // The list that will contain the triangle data for the mesh
        private List<int> triangles = new List<int>();

        [SerializeField]
        private MeshData _meshData;
        private SerializedObject _serializedMeshData;
        private SerializedProperty _widthSegmentProperty;
        private SerializedProperty _heightSegmentProperty;
        private SerializedProperty _widthWorldUnitsProperty;
        private SerializedProperty _heightWorldUnitsProperty;
        private SerializedProperty _twoSidedProperty;
        private SerializedProperty _planeProperty;
        private SerializedProperty _planeNameProperty;
        private SerializedProperty _meshNameProperty;
        private SerializedProperty _meshPathProperty;
        private SerializedProperty _meshMaterialProperty;
        private SerializedProperty _addColliderProperty;
        private SerializedProperty _createAtOriginProperty;
        private SerializedProperty _orientationProperty;
        private SerializedProperty _anchorProperty;
        private SerializedProperty _hexMeshProperty;

        private void OnEnable()
        {
            _meshData = (MeshData)EditorGUIUtility.Load(_storageObjectPath);
            GetMeshData();
        }

        void GetCamera()
        {
            _cam = Camera.current;
            // Hack because camera.current doesn't return editor camera if scene view doesn't have focus
            if (!_cam)
                _cam = _lastUsedCam;
            else
                _lastUsedCam = _cam;
        }


        public void CreateMesh()
        {
            GetMeshData();
            // The list that will contain all of the vertices's must be reset every time we wish to create a new mesh.
            verticies.Clear();
            tangents.Clear();
            uVs.Clear();
            triangles.Clear();

            // Every segment has two vertices's  so total count is segments + 1
            _vCountX = _widthSegment + 1;
            _vCountY = _heightSegment + 1;

            // The size of every segment in world space units
            _vSizeX = _widthWorldUnits / _widthSegment;
            _vSizeY = _heightWorldUnits / _heightSegment;

            // UV factor is equal to....
            _uvFactorX = 1.0f / _widthSegment;
            _uvFactorY = 1.0f / _heightSegment;

            // The mesh will need a user selectable anchor point
            _getAnchor = new Anchor(_anchor, _widthWorldUnits, _heightWorldUnits);
            _offset = _getAnchor.AnchorOffset;
            _offsetId = _getAnchor.AnchorId;

            // We will use the origin as the offset to put the center of the mesh at 0,0,0
            _origin = new Vector3(_widthWorldUnits / 2f, 0, _heightWorldUnits / 2f);

            _plane = MeshCommon.CreateUniqueGameObject(_planeName);


            // We will place the newly created game object, that holds our mesh at either the origin or just in front of the camera.
            if (!_createAtOrigin && _cam)
                _plane.transform.position = _cam.transform.position + _cam.transform.forward * 5.0f;
            else
                _plane.transform.position = Vector3.zero;

            //// The mesh we create will need a name and if it was not give use the default SimpleMesh.
            var planeMesh = new Mesh { name = !string.IsNullOrEmpty(_meshName) ? _meshName : "SimpleMesh" };

            // When we save the mesh as an asset it can and will have a different name than the plane mesh
            // Here we create the root path + name
            var meshAssetName = _meshName + _vCountX + "x" + _vCountY + "W" + _widthWorldUnits + "L" + _heightWorldUnits + (_orientation == Orientation.Horizontal ? "H" : "V") + _offsetId;

            // Return a unique version of the meshAssetName root
            meshAssetName = MeshCommon.CreateUniqueMeshAssetName(_meshName, meshAssetName, _meshPath);

            if (!_hexMesh)
            {
                // Fill the vertex list with all of the mesh's vertices's, tangents and uVs as defined for a plane mesh..
                CreatePlaneVertex();

                // Now create the triangles for a plane mesh
                CreatePlaneTriangles();
            }
            else
            {
                // Fill the vertex list with all of the mesh's vertices's, tangents and uVs as defined for a plane mesh..
                CreateHexVertex();

                // Now create the triangles for a plane mesh
                CreateHexTriangles();
            }


            // Move all of the mesh data from Lists to arrays for Unity3D
            planeMesh.vertices = verticies.ToArray();
            planeMesh.triangles = triangles.ToArray();
            planeMesh.uv = uVs.ToArray();
            planeMesh.tangents = tangents.ToArray();

            // Better recalculate the normals
            planeMesh.RecalculateNormals();

            // Load the calculated values into the mesh that was created
            _plane.GetComponent<MeshFilter>().mesh = planeMesh;
            _plane.GetComponent<MeshRenderer>().material = _meshMaterial;

            // Save the mesh as an asset at the path given in the editor menu
            AssetDatabase.CreateAsset(planeMesh, meshAssetName);
            AssetDatabase.SaveAssets();

        }

        public void GetMeshData()
        // called by MeshEditor.OnGui  
        {
            _widthSegment = _meshData.WidthSegment;
            _heightSegment = _meshData.HeightSegment;
            _widthWorldUnits = _meshData.WidthWorldUnits;
            _heightWorldUnits = _meshData.HeightWorldUnits;
            _twoSided = _meshData.TwoSided;
            _planeName = _meshData.PlaneName;
            _meshName = _meshData.MeshName;
            _meshPath = _meshData.MeshPath;
            _meshMaterial = _meshData.MeshMaterial;
            _addCollider = _meshData.AddCollider;
            _createAtOrigin = _meshData.CreateAtOrigin;
            _orientation = _meshData.Orientation;
            _anchor = _meshData.Anchor;
            _hexMesh = _meshData.HexMesh;
        }
        public void CreatePlaneVertex()
        {

            // Fill the vertex list with all of the mesh's vertices's
            for (float y = 0; y < _vCountY; y++)
            {
                for (float x = 0; x < _vCountX; x++)
                {
                    verticies.Add(new Vector3((_vSizeX) * x + _offset.x, 0, (_vSizeY) * y + _offset.y) - _origin);
                    tangents.Add(_tangent);
                    uVs.Add(new Vector2(x * _uvFactorX, y * _uvFactorY));
                }
            }
        }

        public void CreatePlaneTriangles()
        {

            for (var y = 0; y < _heightSegment; y++)
            {
                for (var x = 0; x < _widthSegment; x++)
                {
                    triangles.Add(_vCountX * y + x);
                    triangles.Add(_vCountX * (y + 1) + x);
                    triangles.Add(_vCountX * y + 1 + x);

                    triangles.Add(_vCountX * (y + 1) + x);
                    triangles.Add(_vCountX * (y + 1) + 1 + x);
                    triangles.Add(_vCountX * y + 1 + x);
                }
                //-----------------

                //-----------------
                // Note if two sided a two sided shader must be used.  Culling off and all that
                if (!_twoSided) continue;
                {
                    for (var x = 0; x < _widthSegment; x++)
                    {
                        triangles.Add(_vCountX * y + x);
                        triangles.Add(_vCountX * y + 1 + x);
                        triangles.Add(_vCountX * (y + 1) + x);

                        triangles.Add(_vCountX * (y + 1) + x);
                        triangles.Add(_vCountX * y + 1 + x);
                        triangles.Add(_vCountX * (y + 1) + 1 + x);

                    }
                }
            }


        }

        public void CreateHexVertex()
        {
            for (float y = 0.0f; y < _vCountY; y++)
            {
                for (float x = 0.0f; x < _vCountX; x++)
                {
                    if (_orientation == Orientation.Horizontal)
                    {
                        //                     verticies.Add(new Vector3(x * _vSizeX - widthInWorldUnits / 2f - anchorOffset.x - (y % 2 - 1) * _vSizeX * .5f, 0.0f, y * _vSizeY - heightInWorldUnits / 2f - anchorOffset.y));
                        verticies.Add(new Vector3(x * _vSizeX + _offset.x + (y % 2 - 1) * _vSizeX * .5f, 0.0f, y * _vSizeY + _offset.y) - _origin);
                    }
                    else
                    {
                        //                     verticies.Add(new Vector3(x * _vSizeX - widthInWorldUnits / 2f - anchorOffset.x - (y % 2 - 1) * _vSizeX * .5f, y * _vSizeY - heightInWorldUnits / 2f - anchorOffset.y, 0.0f));
                        verticies.Add(new Vector3(x * _vSizeX + _offset.x + (y % 2 - 1) * _vSizeX * .5f, y * _vSizeY + _offset.y, 0.0f) - _origin);
                    }
                    tangents.Add(_tangent);
                    uVs.Add(new Vector2(x * _uvFactorX, y * _uvFactorY));

                }
            }

        }

        public void CreateHexTriangles()
        {

            for (var y = 0; y < _heightSegment; y++)
            {
                for (var x = 0; x < _widthSegment; x++)
                {
                    triangles.Add(_vCountX * y + x);
                    triangles.Add(_vCountX * (y + 1) + x);
                    triangles.Add(_vCountX * y + 1 + x);

                    triangles.Add(_vCountX * (y + 1) + x);
                    triangles.Add(_vCountX * (y + 1) + 1 + x);
                    triangles.Add(_vCountX * y + 1 + x);
                }
                //-----------------

                //-----------------
                // Note if two sided a two sided shader must be used.  Culling off and all that
                if (!_twoSided) continue;
                {
                    for (var x = 0; x < _widthSegment; x++)
                    {
                        triangles.Add(_vCountX * y + x);
                        triangles.Add(_vCountX * y + 1 + x);
                        triangles.Add(_vCountX * (y + 1) + x);

                        triangles.Add(_vCountX * (y + 1) + x);
                        triangles.Add(_vCountX * y + 1 + x);
                        triangles.Add(_vCountX * (y + 1) + 1 + x);

                    }
                }
            }


        }
        public void DrawEditor(SerializedObject serializedMeshData)
        {
            _widthSegmentProperty = serializedMeshData.FindProperty("WidthSegment");
            _heightSegmentProperty = serializedMeshData.FindProperty("HeightSegment");
            _widthWorldUnitsProperty = serializedMeshData.FindProperty("WidthWorldUnits");
            _heightWorldUnitsProperty = serializedMeshData.FindProperty("HeightWorldUnits");

            _twoSidedProperty = serializedMeshData.FindProperty("TwoSided");
            _meshNameProperty = serializedMeshData.FindProperty("MeshName");
            _planeNameProperty = serializedMeshData.FindProperty("PlaneName");
            _meshPathProperty = serializedMeshData.FindProperty("MeshPath");

            _meshMaterialProperty = serializedMeshData.FindProperty("MeshMaterial");
            _addColliderProperty = serializedMeshData.FindProperty("AddCollider");
            _createAtOriginProperty = serializedMeshData.FindProperty("CreateAtOrigin");
            _orientationProperty = serializedMeshData.FindProperty("Orientation");
            _anchorProperty = serializedMeshData.FindProperty("Anchor");
            _hexMeshProperty = serializedMeshData.FindProperty("HexMesh");

            // Make sure the serialized Objects are in sync with the values in the editor
            serializedMeshData.Update();


            EditorGUILayout.BeginVertical();
            EditorGUILayout.IntSlider(_widthSegmentProperty, 0, 100, new GUIContent("Width (segments)"));
            EditorGUILayout.IntSlider(_heightSegmentProperty, 0, 100, new GUIContent("Height (segments)"));
            EditorGUILayout.Slider(_widthWorldUnitsProperty, 0, 100, new GUIContent("Width (World Units)"));
            EditorGUILayout.Slider(_heightWorldUnitsProperty, 0, 100, new GUIContent("Height (World Units)"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(_planeNameProperty, new GUIContent("Plane Name"));
            EditorGUILayout.PropertyField(_meshPathProperty, new GUIContent("Mesh Path"));
            EditorGUILayout.PropertyField(_meshNameProperty, new GUIContent("Mesh Name"));
            EditorGUILayout.PropertyField(_addColliderProperty, new GUIContent("Add Collider?"));
            EditorGUILayout.PropertyField(_twoSidedProperty, new GUIContent("Two Sided?"));
            EditorGUILayout.PropertyField(_hexMeshProperty, new GUIContent("Hex Mesh"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(_createAtOriginProperty, new GUIContent("Create at Origin?"));
            EditorGUILayout.PropertyField(_orientationProperty, new GUIContent("Orientation:"));
            EditorGUILayout.PropertyField(_anchorProperty, new GUIContent("Anchor Plan at:"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(_meshMaterialProperty, new GUIContent("Mesh Material"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            var buttonCreateMesh = GUILayout.Button("Create Mesh");

            // Apply changes to the serialized properties
            serializedMeshData.ApplyModifiedProperties();

            if (buttonCreateMesh)
            {
                GetCamera();
                CreateMesh();
            }
            // TODO: Add a display for the mesh and/or UV map to the editor or should that be in a custom inspector?
        }

    }



} // Name Space End