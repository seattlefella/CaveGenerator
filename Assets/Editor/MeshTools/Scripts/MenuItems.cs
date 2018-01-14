using UnityEditor;
using UnityEngine;

namespace Assets.Editor.MeshTools.Scripts
{
    /// <summary>
    /// This class will hold of the my custom menus
    /// </summary>
    public static class MenuItems 
    {


        // The different file paths that are needed
        private static string _storageObjectPath = "Assets/Editor/MeshTools/ObjectStorage/";

        // This item will create the pop up floating editor window with a 3 tabs
        [MenuItem("Tools/My Mesh Tools/Create Mesh")]
        private static void ShowMeshWindow()
        {
            MeshEditor.ShowMeshWindow();
        }


        [MenuItem("Tools/My Mesh Tools/Create Persistent Store/ Mesh Data")]
        private static void CreateMeshStore()
        {
            CreateStore(_storageObjectPath, "MeshData");
        }

        [MenuItem("Tools/My Mesh Tools/Create Persistent Store/ Cave Data")]
        private static void CreateCaveStore()
        {
            CreateStore(_storageObjectPath, "CaveData");
        }
        private static void CreateStore(string path, string name)
        {

            if (path != "" && name != "")
            {
                if (name == "MeshData")
                    MeshCommon.CreateScriptableAsset<MeshData>(path + name + ".asset");

                if (name == "CaveData")
                    MeshCommon.CreateScriptableAsset<CaveData>(path + name + ".asset");
            }

            else
            {
               
                Debug.Log("The path or name was not valid");
            }
        }


    }
}

