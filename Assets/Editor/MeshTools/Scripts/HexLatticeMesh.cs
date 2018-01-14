/*
Unify wiki script, it's included for conveniently making a HexLattice of any detail level
Author: Michael Garforth.
Extended by Juan Manuel Palacios
Extended by Frogsbo, to make hexagon lattice.
/\/\/\
\/\/\/ 
/\/\/\ Isosceles triangle height is sqrt(1^2 - .5^2), 
sqrt(.75), 0.8660254037844386.  ~ 100/86.6
so to have a square HexLattice plane, use rows/columns like 50/43 and 32/28 38/33 35/30
(see table addendum)
*/



using UnityEngine;
using UnityEditor;
using System.Collections;


public class HexLatticeMesh 
{

    public enum Orientation
    {
        Horizontal,
        Vertical
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


    private GUIContent widthInVerticiesTT = new GUIContent("Width  (segments)", "Expressed in vertcies");
    private int widthInVerticies = 32;

    private GUIContent heightInVerticiesTT = new GUIContent("Height  (segments)", "Expressed in vertcies");
    private int heightInVerticies = 16;

    private GUIContent widthInWorldUnitsTT = new GUIContent("Width  (world Units)", "Expressed in world units");
    private float widthInWorldUnits = 1.0f;

    private GUIContent heightInWorldUnitsTT = new GUIContent("Height  (world units)", "Expressed in world units");
    private float heightInWorldUnits = 1.0f;

    public Orientation orientation = Orientation.Horizontal;
    public AnchorPoint anchor = AnchorPoint.Center;
    private bool twoSided = false;
    public bool addCollider = false;
    public bool createAtOrigin = true;
    public string optionalName;

    static Camera cam;
    static Camera lastUsedCam;


    static void GetCamera()
    {
        cam = Camera.current;
        // Hack because camera.current doesn't return editor camera if scene view doesn't have focus
        if (!cam)
            cam = lastUsedCam;
        else
            lastUsedCam = cam;
        ScriptableWizard.DisplayWizard("Create HexLattice", typeof(CreateHexLattice));
    }

    public void CreateMesh()
    {
        GameObject HexLattice = new GameObject();

        if (!string.IsNullOrEmpty(optionalName))
            HexLattice.name = optionalName;
        else
            HexLattice.name = "HexLattice";

        if (!createAtOrigin && cam)
            HexLattice.transform.position = cam.transform.position + cam.transform.forward * 5.0f;
        else
            HexLattice.transform.position = Vector3.zero;

        Vector2 anchorOffset;
        string anchorId;
        switch (anchor)
        {
            case AnchorPoint.TopLeft:
                anchorOffset = new Vector2(-widthInWorldUnits / 2.0f, heightInWorldUnits / 2.0f);
                anchorId = "TL";
                break;
            case AnchorPoint.TopHalf:
                anchorOffset = new Vector2(0.0f, heightInWorldUnits / 2.0f);
                anchorId = "TH";
                break;
            case AnchorPoint.TopRight:
                anchorOffset = new Vector2(widthInWorldUnits / 2.0f, heightInWorldUnits / 2.0f);
                anchorId = "TR";
                break;
            case AnchorPoint.RightHalf:
                anchorOffset = new Vector2(widthInWorldUnits / 2.0f, 0.0f);
                anchorId = "RH";
                break;
            case AnchorPoint.BottomRight:
                anchorOffset = new Vector2(widthInWorldUnits / 2.0f, -heightInWorldUnits / 2.0f);
                anchorId = "BR";
                break;
            case AnchorPoint.BottomHalf:
                anchorOffset = new Vector2(0.0f, -heightInWorldUnits / 2.0f);
                anchorId = "BH";
                break;
            case AnchorPoint.BottomLeft:
                anchorOffset = new Vector2(-widthInWorldUnits / 2.0f, -heightInWorldUnits / 2.0f);
                anchorId = "BL";
                break;
            case AnchorPoint.LeftHalf:
                anchorOffset = new Vector2(-widthInWorldUnits / 2.0f, 0.0f);
                anchorId = "LH";
                break;
            case AnchorPoint.Center:
            default:
                anchorOffset = Vector2.zero;
                anchorId = "C";
                break;
        }

        MeshFilter meshFilter = (MeshFilter)HexLattice.AddComponent(typeof(MeshFilter));
        HexLattice.AddComponent(typeof(MeshRenderer));

        string HexLatticeAssetName = HexLattice.name + widthInVerticies + "x" + heightInVerticies + "W" + widthInWorldUnits + "L" + heightInWorldUnits + (orientation == Orientation.Horizontal ? "H" : "V") + anchorId + ".asset";
        Mesh m = (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Editor/" + HexLatticeAssetName, typeof(Mesh));

        if (m == null)
        {
            m = new Mesh();
            m.name = HexLattice.name;

            int hCount2 = widthInVerticies + 1;
            int vCount2 = heightInVerticies + 1;
            int numTriangles = widthInVerticies * heightInVerticies * 6;
            int numVertices = hCount2 * vCount2;

            Vector3[] vertices = new Vector3[numVertices];
            Vector2[] uvs = new Vector2[numVertices];
            int[] triangles = new int[numTriangles];

            int index = 0;
            float uvFactorX = 1.0f / widthInVerticies;
            float uvFactorY = 1.0f / heightInVerticies;
            float scaleX = widthInWorldUnits / widthInVerticies;
            float scaleY = heightInWorldUnits / heightInVerticies;
            for (float y = 0.0f; y < vCount2; y++)
            {
                for (float x = 0.0f; x < hCount2; x++)
                {
                    if (orientation == Orientation.Horizontal)
                    {
                        vertices[index] = new Vector3(x * scaleX - widthInWorldUnits / 2f - anchorOffset.x - (y % 2 - 1) * scaleX * .5f, 0.0f, y * scaleY - heightInWorldUnits / 2f - anchorOffset.y);
                    }
                    else
                    {
                        vertices[index] = new Vector3(x * scaleX - widthInWorldUnits / 2f - anchorOffset.x - (y % 2 - 1) * scaleX * .5f, y * scaleY - heightInWorldUnits / 2f - anchorOffset.y, 0.0f);
                    }
                    uvs[index++] = new Vector2(x * uvFactorX, y * uvFactorY);
                }
            }

            index = 0;
            for (int y = 0; y < heightInVerticies; y++)
            {
                if (y % 2 == 0)
                {
                    for (int x = 0; x < widthInVerticies; x++)
                    {
                        triangles[index] = (y * hCount2) + x;
                        triangles[index + 1] = ((y + 1) * hCount2) + x;
                        triangles[index + 2] = ((y + 1) * hCount2) + x + 1;


                        triangles[index + 3] = (y * hCount2) + x;
                        triangles[index + 4] = ((y + 1) * hCount2) + x + 1;
                        triangles[index + 5] = (y * hCount2) + x + 1;
                        index += 6;
                    }
                }
                else
                {
                    for (int x = 0; x < widthInVerticies; x++)
                    {
                        triangles[index] = (y * hCount2) + x;
                        triangles[index + 1] = ((y + 1) * hCount2) + x;
                        triangles[index + 2] = (y * hCount2) + x + 1;

                        triangles[index + 3] = ((y + 1) * hCount2) + x;
                        triangles[index + 4] = ((y + 1) * hCount2) + x + 1;
                        triangles[index + 5] = (y * hCount2) + x + 1;
                        index += 6;
                    }
                }
            }

            m.vertices = vertices;
            m.uv = uvs;
            m.triangles = triangles;
            m.RecalculateNormals();
            ;

            AssetDatabase.CreateAsset(m, "Assets/Editor/" + HexLatticeAssetName);
            AssetDatabase.SaveAssets();
        }

        meshFilter.sharedMesh = m;
        m.RecalculateBounds();

        if (addCollider)
            HexLattice.AddComponent(typeof(BoxCollider));

        Selection.activeObject = HexLattice;
    }

    public void DrawEditor()
    {


        widthInVerticies = (int)EditorGUILayout.Slider(widthInVerticiesTT, widthInVerticies, 1f, 255f);
        heightInVerticies = (int)EditorGUILayout.Slider(heightInVerticiesTT, heightInVerticies, 1f, 255f);
        widthInWorldUnits = (int)EditorGUILayout.Slider(widthInWorldUnitsTT, widthInWorldUnits, 1f, 128f);
        heightInWorldUnits = (int)EditorGUILayout.Slider(heightInWorldUnitsTT, heightInWorldUnits, 1f, 128f);

        addCollider = (bool)EditorGUILayout.Toggle("Add a Collider", addCollider);
        twoSided = (bool)EditorGUILayout.Toggle("Make the Mesh two sided", twoSided);
        createAtOrigin = (bool)EditorGUILayout.Toggle("Create Mesh at the Origin", createAtOrigin);

        orientation = (Orientation)EditorGUILayout.EnumPopup("Mesh Orientation", orientation);
        anchor = (AnchorPoint)EditorGUILayout.EnumPopup("Mesh Anchor Point", anchor);

        optionalName = (string)EditorGUILayout.TextField("Mesh Name", optionalName);

        bool buttonTest = GUILayout.Button("Create Mesh");
        if (buttonTest)
        {
            HexLatticeMesh.GetCamera();
            this.CreateMesh();
        }

    }

}
/* LIST OF EXACT RATIOS OF LATTICE HEIGHT FOR GIVEN NUM OF CELLS:
1	 0,866
2	 1,732
3	 2,598
4	 3,464
5	 4,33
6	 5,196
7	 6,062------
8	 6,928-----
9	 7,794
10	 8,66
11	 9,526
12	 10,392
13	 11,258
14	 12,124---
15	 12,99-------------
16	 13,856
17	 14,722
18	 15,588
19	 16,454
20	 17,32
21	 18,186
22	 19,052-----------
23	 19,918-----------
24	 20,784
25	 21,65
26	 22,51
27	 23,382
28	 24,248
29	 25,114
30	 25,98------------
31	 26,846
32	 27,712
33	 28,578
34	 29,444
35	 30,31
36	 31,176
37	 32,042-----------
38	 32,908----------
39	 33,774
40	 34,64
41	 35,506
42	 36,372
43	 37,238
44	 38,104
45	 38,97-----------
46	 39,836
47	 40,702
48	 41,568
49	 42,434
50	 43,3
51	 44,166
52	 45,032----------
53	 45,898----------
54	 46,764
55	 47,63
56	 48,496
57	 49,362
58	 50,228
59	 51,094---------
60	 51,96---------
61	 52,826
62	 53,692
63	 54,558
64	 55,424
65	 56,29
66	 57,156
67	 58,022-----------
68	 58,888
69	 59,754
*/

