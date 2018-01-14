using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.MeshTools.Scripts
{
    public class MeshEditor : UnityEditor.EditorWindow
    {


        // Each tab will  need an instance of the TBDMesh class
        private MeshCreator _planeMesh ;
        private MeshData _meshData;
        private SerializedObject _serializedMeshData;
        private HexLatticeMesh _hexLatticeMesh;
        private CaveData _caveData;
        private CaveMapCreator _caveMapCreator;
        private CaveMeshCreator _caveMeshCreator;
        private SerializedObject _serializedCaveData;
        // The different file paths that are needed
        private string _storageObjectPath = "Assets/Editor/MeshTools/ObjectStorage/";


        // These are needed to name and track the selected the Editor Tabs
        private List<Tabs> _tabs;
        private List<string> _tabLabels;
        private Tabs _tabSelected;

        // The cached instance of this instance of the createMesh script
        private static MeshEditor _instance;

        public static void ShowMeshWindow()
        {
            _instance = (MeshEditor)EditorWindow.GetWindow(typeof(MeshEditor));
            _instance.titleContent = new GUIContent("Mesh Editor");
        }

        private void OnEnable()
        {
            var temp = _storageObjectPath + "MeshData.asset";
            _meshData = (MeshData)EditorGUIUtility.Load("Assets/Editor/MeshTools/ObjectStorage/MeshData.asset");
            _serializedMeshData = new SerializedObject(_meshData);

            temp = _storageObjectPath + "CaveData.asset";
            _caveData = (CaveData)EditorGUIUtility.Load("Assets/Editor/MeshTools/ObjectStorage/CaveData.asset");
            _serializedCaveData = new SerializedObject(_caveData);

            if (_tabs == null)
            {
                InitTabs();
            }

            if (_planeMesh == null)
            {
                _planeMesh = ScriptableObject.CreateInstance<MeshCreator>();
            }

            if (_caveMapCreator == null)
            {
                _caveMapCreator = ScriptableObject.CreateInstance<CaveMapCreator>();
            }
   
        }

        private void InitTabs()
        {
            _tabs = MeshCommon.GetListFromEnum<Tabs>();
            _tabLabels = new List<string>();

            foreach (var tab in _tabs)
            {
                _tabLabels.Add(tab.ToString());
            }
        }

        private void DrawTabs()
        {
            var index = (int) _tabSelected;
            index = GUILayout.Toolbar(index, _tabLabels.ToArray());
            _tabSelected = _tabs[index];
        }


        public void OnGUI()
        {
            DrawTabs();
            if (_tabSelected == Tabs.Plane)
            {
                // Every tab will call it's DrawEditor function which is held with each
                // create class
                _planeMesh.DrawEditor(_serializedMeshData);
            }

            if (_tabSelected == Tabs.Cave)
            {
                EditorGUILayout.LabelField("This is the cave generator");
                _caveMapCreator.DrawEditor(_serializedCaveData);
            }

            if (_tabSelected == Tabs.LandMass)
            {
                EditorGUILayout.LabelField("This is the land mass generator");
            }

        }
        private void Update()
        {
            // Debug.Log("OnGUI called...");
        }


    }
}
