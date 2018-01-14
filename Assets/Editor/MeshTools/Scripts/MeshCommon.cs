using UnityEditor;
using UnityEngine;
using  System.Collections.Generic;

namespace Assets.Editor.MeshTools.Scripts
{
    public interface ICreateMesh
    {
        void DrawEditor(SerializedObject serializedMeshData);
        void CreateMesh();
    }

    public enum AnchorPoint
    {
        TopLeft,
        TopHalf,
        TopRight,
        RightHalf,
        BottomRight,
        BottomHalf,
        BottomLeft,
        LeftHalf,
        Center
    }

    public enum Orientation
    {
        Horizontal,
        Vertical
    }
    public class Anchor
    {
        public Vector2 AnchorOffset { get; set; }
        public string AnchorId { get; set; }

        public Anchor(AnchorPoint anchor, float widthInWorldUnits, float heightInWorldUnits)
        {

            switch (anchor)
            {
                case AnchorPoint.TopLeft:
                    AnchorOffset = new Vector2(-widthInWorldUnits / 2.0f, heightInWorldUnits / 2.0f);
                    AnchorId = "TL";
                    break;
                case AnchorPoint.TopHalf:
                    AnchorOffset = new Vector2(0.0f, heightInWorldUnits / 2.0f);
                    AnchorId = "TH";
                    break;
                case AnchorPoint.TopRight:
                    AnchorOffset = new Vector2(widthInWorldUnits / 2.0f, heightInWorldUnits / 2.0f);
                    AnchorId = "TR";
                    break;
                case AnchorPoint.RightHalf:
                    AnchorOffset = new Vector2(widthInWorldUnits / 2.0f, 0.0f);
                    AnchorId = "RH";
                    break;
                case AnchorPoint.BottomRight:
                    AnchorOffset = new Vector2(widthInWorldUnits / 2.0f, -heightInWorldUnits / 2.0f);
                    AnchorId = "BR";
                    break;
                case AnchorPoint.BottomHalf:
                    AnchorOffset = new Vector2(0.0f, -heightInWorldUnits / 2.0f);
                    AnchorId = "BH";
                    break;
                case AnchorPoint.BottomLeft:
                    AnchorOffset = new Vector2(-widthInWorldUnits / 2.0f, -heightInWorldUnits / 2.0f);
                    AnchorId = "BL";
                    break;
                case AnchorPoint.LeftHalf:
                    AnchorOffset = new Vector2(-widthInWorldUnits / 2.0f, 0.0f);
                    AnchorId = "LH";
                    break;
                case AnchorPoint.Center:
                default:
                    AnchorOffset = Vector2.zero;
                    AnchorId = "C";
                    break;
            }
        }

    }


    // The editor window will have a number of tabs for the different  types of mesh
    public enum Tabs
    {
        LandMass,
        Cave,
        Plane,
    }



    public static class MeshCommon
    {
        // The various paths we will need within the Editor directory to find/save assets
        public const string PathMaterial = "Assets/Editor/MeshTools/Material/";
        public const string PathMesh = "Assets/Editor/MeshTools/Mesh/";
        public const string PathStorageObject = "Assets/Editor/MeshTools/ObjectStorage/";
        public const string PathPrefab = "Assets/PreFabs/Cave.prefab";

        public const string DefaultMeshName = "SimpleMesh";
        public const string DefaultMeshAssetName = "SimpleMeshAsset";
        public const string DefaultGoName = "MyGameObj";
        public const string DefaultExtension = ".asset";
        public const string DefaultCaveName = "Cave";


        public static T CreateScriptableAsset<T>(string path)
where T : ScriptableObject
        {
            T dataClass = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(dataClass, path);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = dataClass;
            return dataClass;
        }

        public static GameObject CreateUniqueGameObject(string objName = DefaultGoName, bool addRenderComponent = true, bool unique = true)
        {
            // We must create a game object that will hold a mesh
            var gObj = new GameObject();

            if (addRenderComponent)
            {
                gObj.AddComponent<MeshFilter>();
                gObj.AddComponent<MeshRenderer>();
            }

            if (GameObject.Find(objName) == null || !unique)
            {
                gObj.name = objName;
            }
            else
            {
                var seq = 1;
                var mx = objName;

                while (GameObject.Find(mx) != null)
                {
                    mx = objName + "(" + seq++ + ")";
                }
                gObj.name = mx;
            }


            return gObj;
        }

        public static string CreateUniqueMeshAssetName(string meshName = DefaultMeshName, string meshAssetName = DefaultMeshAssetName, string meshPath = PathMesh, string ext = DefaultExtension)
        {
            // The mesh asset name we create will need a name and if it already exists the name must be sequenced.
            var temp = meshPath + meshAssetName + ext;
            var m = (Mesh)AssetDatabase.LoadAssetAtPath(meshPath + meshAssetName + ext, typeof(Mesh));
            if (m != null)
            {
                // The asset already exists so we should add a sequence number to the name.
                var seq = 1;
                var mx = meshPath + meshAssetName + "V";

                // Find a unique sequence number
                while (m != null)
                {
                    meshAssetName = mx + seq++;
                    m = (Mesh)AssetDatabase.LoadAssetAtPath(meshAssetName + ext, typeof(Mesh));
                }
                return (meshAssetName + ext);

            }

            else
            {
                return temp;
            }
        }

        /// <summary>
        /// Return a list of all of the members of the given enum
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> GetListFromEnum<T>()
        {
            List<T> enumList = new List<T>();

            System.Array enums = System.Enum.GetValues(typeof(T));
            foreach (T e in enums)
            {
                enumList.Add(e);
            }
            return enumList;
        }

         public static GameObject GetChildGameObject(GameObject fromGameObject, string withName)
        {
            //Author: Isaac Dart, June-13.
 
            Transform[] ts = fromGameObject.transform.GetComponentsInChildren<Transform>(true);

             foreach (Transform t in ts)
             {
                  if (t.gameObject.name == withName)
                    return t.gameObject;               

             }

           return null;
        }


    }

}
