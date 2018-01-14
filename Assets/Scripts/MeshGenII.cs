using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
///     This class will create meshes on a constructed grid of Square types.
///     The four corners of each Square are control nodes that will be set to a 1 or 0
///     based on the map that was created using the cellular automate algorithm.
/// </summary>
public class MeshGenII : MonoBehaviour {


    /// A square grid has taken the processed map of {0,1}'s and formed it into a rectangular array
    /// of squares, each square consuming 4 values from the map array.
    /// Each squareGrid has 4x control nodes and 4x interstitial nodes.
    public SquareGrid squareGrid;

    // The actual walls we will be placing into Unity3d based on our SquareGrid;
    public MeshFilter walls;

    public MeshFilter cave;

    // This is used to allow a 2d player to walk about
    public bool is2D;

    /// Every mesh in unity3D is made up of two lists - one to hold all of the vertices's, 
    /// which contain position information and the other to form a list of the three vertices's that make up any give triangle.
    private List<Vector3>   vertices;
    private List<int>       triangles;

    // The key will be a vertex index ie. the index of the vertex array, the return will be a list of all of the triangles that vertex is part of.
    Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();

    // outlines holds a list of all of the unique outlines (out side edges) to be found within the squareGrid
    // Each individual outline is nothing more than a list of vertexIndex(s) that form a loop that consists of outside edges only.
    List<List<int>> outlines = new List<List<int>>();

    // The hash table holds the checked/not-checked state of each vertex in the squarGrid array.
    // A vertex need only be checked once.  A hashSet is a fast way to do this.
    HashSet<int> checkedVertices = new HashSet<int>();


    public void GenerateMesh(int[,] map, float squareSize)
    {

        triangleDictionary.Clear();
        outlines.Clear();
        checkedVertices.Clear();

        // This grid will hold the resulting set of control squares showing us where to put walls
        squareGrid = new SquareGrid(map, squareSize);

        // These two lists make up the actual mesh
        vertices = new List<Vector3>();
        triangles = new List<int>();

        for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
        {
            for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
            {
                TriangulateSquare(squareGrid.squares[x, y]);
            }
        }

        // The actual creation of the mesh in Unity3d  Note:  The mesh is actualy a Unity3d Component.
        Mesh mesh = new Mesh();
        cave.mesh = mesh;

        // the mesh methods require an array and not a list
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        // We now want to apply a texture to the image as such we must generate the UV map
        int tileAmount = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].x) * tileAmount;
            float percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].z) * tileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }
        mesh.uv = uvs;


        if (is2D)
        {
            Generate2DColliders();
        }
        else {
        // Now we create the actual walls that will be seen in Unity3D game space
            CreateWallMesh();
        }

    }

    void CreateWallMesh()
    {

        CalculateMeshOutlines();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = 5;

        foreach (List<int> outline in outlines)
        {
            for (int i = 0; i < outline.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;

                // We must add the 4 vertices's for the wall mesh to a new list for conversion to an array
                // when we create the actual mesh.
                wallVertices.Add(vertices[outline[i]]);                             // Top left
                wallVertices.Add(vertices[outline[i + 1]]);                         // Top right
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);   // bottom left
                wallVertices.Add(vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                // Each wall will be made up of two triangles
                // Triangle one - note: he is going counter clock wise "because we are in side"
                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                // Triangle two
                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
        }

        // Create the actual mesh in Unity3D game space.  Remember all meshes must be Arrays and not a C# list
        // - a vertices's and triangles array
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;



        if (walls.gameObject.GetComponent<MeshCollider>() == null)
        {
          //  Debug.Log("created mesh collider");
            MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider>();

        }

        else
        {
            Destroy(walls.gameObject.GetComponent<MeshCollider>());
            MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider>();
            wallCollider.sharedMesh = wallMesh;
        }
    }

    void Generate2DColliders()
    {

        EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();
        for (int i = 0; i < currentColliders.Length; i++)
        {
            Destroy(currentColliders[i]);
        }

        CalculateMeshOutlines();

        foreach (List<int> outline in outlines)
        {
            EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            Vector2[] edgePoints = new Vector2[outline.Count];

            for (int i = 0; i < outline.Count; i++)
            {
                edgePoints[i] = new Vector2(vertices[outline[i]].x, vertices[outline[i]].z);
            }
            edgeCollider.points = edgePoints;
        }

    }

    public void TriangulateSquare(Square square)
    {
        switch (square.configuration)
        {
            case 0:
                break;

            // 1 points:
            case 1:
                MeshFromPoints(square.centerLeft, square.centerBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centerBottom, square.centerRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centerRight, square.centerTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerLeft);
                break;

            /// only two control node have been selected in this square
            // 2 points:
            case 3:
                MeshFromPoints(square.centerRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 6:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.centerBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerLeft);
                break;
            case 5:
                MeshFromPoints(square.centerTop, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft, square.centerLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            /// only three control nodes have been selected in this square
            // 3 point:
            case 7:
                MeshFromPoints(square.centerTop, square.topRight, square.bottomRight, square.bottomLeft, square.centerLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centerTop, square.centerRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centerRight, square.centerBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centerBottom, square.centerLeft);
                break;

            /// All four control nodes have been selected in this square
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);

                // In this case all 4 nodes are active an as such non of the verticies can be part of 
                //an outside edge.  So let's just mark the hash table to save us from checking them later.
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                break;
        }
    }

    void MeshFromPoints(params Node[] points)
    {
        AssignVertices(points);

        if (points.Length >= 3)
            CreateTriangle(points[0], points[1], points[2]);
        if (points.Length >= 4)
            CreateTriangle(points[0], points[2], points[3]);
        if (points.Length >= 5)
            CreateTriangle(points[0], points[3], points[4]);
        if (points.Length >= 6)
            CreateTriangle(points[0], points[4], points[5]);

    }

    void CreateTriangle(Node a, Node b, Node c)
    {
        // Unity3D requires that a mesh is made up of two lists - one a list of triangles 
        // the other a list of vertexIndexes
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        // Now we add the vertex of every triangle created into the dictionary
        // This will allow us later to look up for any given vertexIndex what triangle it is associated with
        Triangle triangle = new Triangle(a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    // Step through every vertices's in the mesh and test if it is connected to another vertex as an outside edge
    // if it is follow the path all the way around until it closes the loop by meeting himself.
    // save the complete outside path as a list of vertexes.
    void CalculateMeshOutlines()
    {
        for (int vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
        {
            if (!checkedVertices.Contains(vertexIndex))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex != -1)
                {
                    checkedVertices.Add(vertexIndex);

                    List<int> newOutline = new List<int>();

                    // We now know that ths vertexIndex is on an outside endge, we must create a list
                    // so we can hold all points on this outside edge and add this new list to our collection 
                    // of all outline edges.
                    newOutline.Add(vertexIndex);
                    outlines.Add(newOutline);

                    // Use the recursive method FollowOutLIne() to find all of the nodes in the outside edge
                    //storing the results in outlines[outlines.Count - 1] which is a list containing the current
                    // vertex(s) associated with this specific outside edge.
                    FollowOutline(newOutlineVertex, outlines.Count - 1);

                    //outlines[outlines.Count - 1] returns a list .Add adds to that list element
                    // The code path will not return to this point until FollowOutline has found and saved all of
                    // the verticies along the out side edge.
                    outlines[outlines.Count - 1].Add(vertexIndex);
                }
            }
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex)
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if (nextVertexIndex != -1)
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }

    int GetConnectedOutlineVertex(int vertexIndex)
    {
        // This is the list of all triangles associated with the given vertexIndex.
        List<Triangle> trianglesContainingVertex = triangleDictionary[vertexIndex];

        /// step through all the triangles that the given vertex is connected to
        for(int i = 0; i < trianglesContainingVertex.Count; i++)
        {
            // Step through the 3x vertex(s) that the given triangle contains.
            Triangle triangle = trianglesContainingVertex[i];
            for(int j = 0; j < 3; j++)
            {
                // vertexB is the second vertex that we want to test the orginal vertexIndex against
                int vertexB = triangle[j];
                if(vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if(IsOutlineEdge(vertexIndex, vertexB))
                    {
                        return vertexB;
                    }
                }
            }
        }
        return -1;
    }

    // This method will test if the two given vertex(s) are part of an outline edge.
    bool IsOutlineEdge(int vertexA, int vertexB)
    {
        /// The dictionary, which was created when all triangles were created for the mesh,
        /// contains a list of all vertex's and the triangles that they are in.
        /// To be an edge each vertex must be part of one and only one triangle.
        /// Step 1: retrieve of the dictionary the list of triangles that are associated with
        /// the given vertex.
        /// Step 2: Test to see if vertexIntexB is contained witin the triangel that has vertexIntexA as a member.
        List<Triangle> trianglesContainingVertexA = triangleDictionary[vertexA];
        int sharedTriangleCount = 0;

        for (int i = 0; i < trianglesContainingVertexA.Count; i++)
        {
            if(trianglesContainingVertexA[i].Contains(vertexB))
            {
                // incrament the count, we must know just how many triangles they share!
                sharedTriangleCount++;
                if(sharedTriangleCount > 1)
                {
                    // If the vertcies share more than one triangle there is no need to continue
                    // we know that they cannot be an outline edge.
                    break;
                }
            }
        }

        return sharedTriangleCount == 1;
    }

    void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
    {
        // First check to see if the vertexIndexKey is already being held in the dictionary.
        if (triangleDictionary.ContainsKey(vertexIndexKey))
        {
            // If so this vertexIndexKey is part of another triangle and we should add that to 
            // the list of triangles associated with vertexIndexKey
            triangleDictionary[vertexIndexKey].Add(triangle);
        }
        else
        {
            // If not, we should create a new dictionary entry and add this triangle to it as the
            // first triangle in this list of triangles for this vertex.
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDictionary.Add(vertexIndexKey, triangleList);
        }
    }

    struct Triangle
    {
        /// <summary>
        ///  A vertexIndex is simply the position of the given vertex within the vertex array.
        /// </summary>
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;

        private int[] verticies;

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            verticies = new int[3];
            verticies[0] = a;
            verticies[1] = b;
            verticies[2] = c;
        }

      /// an indexer to allow us to access the Trangle verticies like an array
        public int this[int i]
        {
            get
            {
                return verticies[i];
            }
        }

        public bool Contains(int vertexIndex)
        {
            // This method is useful to see if the given vertexIndex is contained within the triangle.
            return (vertexIndexA == vertexIndex) || (vertexIndexB == vertexIndex) || (vertexIndexC == vertexIndex);
        }
    }


    void AssignVertices(Node[] points)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].vertexIndex == -1)
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }


    public class SquareGrid
    {
        public Square[,] squares;

        public SquareGrid(int[,] _map, float _squareSize)
        {
            int nodeCountX = _map.GetLength(0);
            int nodeCountY = _map.GetLength(1);
            float mapHeight = nodeCountY * _squareSize;
            float mapWidth = nodeCountX * _squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            ///
            /// Each  ControlNode must be assigned a position in the game space.  We off set the x and z by half the width to center the map.
            /// As (0,0,0) is in the screen center.
            /// 
            for (int x = 0; x < nodeCountX; x++)
            {
                for (int y = 0; y < nodeCountY; y++)
                {
 //                   Vector3 pos = new Vector3((-mapWidth / 2) + (x * _squareSize), 0, (mapHeight / 2) + (-y * _squareSize));
                    Vector3 pos = new Vector3(-mapWidth / 2 + x * _squareSize + _squareSize / 2, 0, -mapHeight / 2 + y * _squareSize + _squareSize / 2);
                    controlNodes[x, y] = new ControlNode(pos, _map[x, y] == 1, _squareSize);
                }
            }

            /// We now build a 2-dimimensional grid of squares which will be used to create the actual cave mesh
            squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (int x = 0; x < nodeCountX - 1; x++)
            {
                for (int y = 0; y < nodeCountY - 1; y++)
                {
                    /// The top line here is a clockwise rotation starting from the upper left.
                    /// What is shown in the example is counter clockwise starting on the lower right.
                    /// this does not seem consistant with his top left cornor rule.
                    /// for now I will comment out the clockwise assignment.
 //                   squares[x, y] = new Square(controlNodes[x, 0], controlNodes[x + 1, y], controlNodes[x + 1, y + 1], controlNodes[x, y + 1]);
                    squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                }
            }
        }
    }

    public class Square
    {
        public ControlNode topLeft, topRight, bottomLeft, bottomRight;
        public Node centerTop, centerLeft, centerBottom, centerRight;

        // This int will hold which control nodes are active or not
        public int configuration;

        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft)
    {
        topLeft = _topLeft;
        topRight = _topRight;
        bottomRight = _bottomRight;
        bottomLeft = _bottomLeft;

        centerTop = topLeft.right;
        centerRight = bottomRight.above;
        centerBottom = bottomLeft.right;
        centerLeft = bottomLeft.above;

        /// There are 16 possible configurations of squares.  The identification of a specific configuration
        /// is critical for mesh creation
        /// 
        if (topLeft.active)
            configuration += 8;
        if (topRight.active)
            configuration += 4;
        if (bottomRight.active)
            configuration += 2;
        if (bottomLeft.active)
            configuration += 1;
    }
}

    public class ControlNode : Node
    {
        public bool active;
        public Node above, right;
        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos)
        {
            active = _active;
            above = new Node(position + Vector3.forward * squareSize / 2f);
            right = new Node(position + Vector3.right * squareSize / 2f);
        }
    }

    public class Node
    {
        public Vector3 position;

        // The vertexIndex will be set to the array index of the vertex within the mesh.vertexList array
        public int vertexIndex = -1;

        public Node(Vector3 _pos)
        {
            position = _pos;
        }
    }



    void TBDOnDrawGizmos()
    {
        if (squareGrid != null)
        {
            for (int x = 0; x < squareGrid.squares.GetLength(0); x++)
            {
                for (int y = 0; y < squareGrid.squares.GetLength(1); y++)
                {

                    Gizmos.color = (squareGrid.squares[x, y].topLeft.active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.squares[x, y].topLeft.position, Vector3.one * .4f);

                    Gizmos.color = (squareGrid.squares[x, y].topRight.active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.squares[x, y].topRight.position, Vector3.one * .4f);

                    Gizmos.color = (squareGrid.squares[x, y].bottomRight.active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.squares[x, y].bottomRight.position, Vector3.one * .4f);

                    Gizmos.color = (squareGrid.squares[x, y].bottomLeft.active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.squares[x, y].bottomLeft.position, Vector3.one * .4f);


                    Gizmos.color = Color.grey;
                    Gizmos.DrawCube(squareGrid.squares[x, y].centerTop.position, Vector3.one * .15f);
                    Gizmos.DrawCube(squareGrid.squares[x, y].centerRight.position, Vector3.one * .15f);
                    Gizmos.DrawCube(squareGrid.squares[x, y].centerBottom.position, Vector3.one * .15f);
                    Gizmos.DrawCube(squareGrid.squares[x, y].centerLeft.position, Vector3.one * .15f);

                }
            }
        }
    }





}
