using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Assets.Editor.MeshTools.Scripts
{
    /// <summary>
    ///     This class will create meshes on a constructed grid of Square types.
    ///     The four corners of each Square are control nodes that will be set to a 1 or 0
    ///     based on the map that was created using the cellular automata's algorithm.
    /// </summary>
    public class CaveMeshCreator : UnityEditor.Editor
    {


        /// A square grid has taken the processed map of {0,1}'s and formed it into a rectangular array
        /// of squares, each square consuming 4 values from the map array.
        /// Each squareGrid has 4x control nodes and 4x interstitial nodes.
        public SquareGrid squareGrid;

        // The actual walls we will be placing into Unity3d based on our SquareGrid;
        public MeshFilter Walls;

        public MeshFilter Cave;
        public float WallHeightWorldUnits = 1;

        // This is used to allow a 2d player to walk about
        public bool Is2D;

        /// Every mesh in unity3D is made up of two lists - one to hold all of the vertices's, 
        /// which contain position information and the other to form a list of the three vertices's that make up any give triangle.
        private List<Vector3> _vertices;
        private List<int> _triangles;

        // The key will be a vertex index IE. the index of the vertex array, the return will be a list of all of the triangles that vertex is part of.
        private Dictionary<int, List<Triangle>> _triangleDictionary = new Dictionary<int, List<Triangle>>();

        // outlines holds a list of all of the unique outlines (out side edges) to be found within the squareGrid
        // Each individual outline is nothing more than a list of vertexIndex(s) that form a loop that consists of outside edges only.
        private List<List<int>> _outlines = new List<List<int>>();

        // The hash table holds the checked/not-checked state of each vertex in the squarGrid array.
        // A vertex need only be checked once.  A hashSet is a fast way to do this.
        private HashSet<int> _checkedVertices = new HashSet<int>();


        public void GenerateMesh(int[,] map, Vector3 squareSize, string caveName, GameObject genCaveGameObj, float wallHeightWorldUnits)
        {

            _triangleDictionary.Clear();
            _outlines.Clear();
            _checkedVertices.Clear();
            WallHeightWorldUnits = wallHeightWorldUnits;


            // This grid will hold the resulting set of control squares showing us where to put walls
            squareGrid = new SquareGrid(map, squareSize);

            // These two lists make up the actual mesh
            _vertices = new List<Vector3>();
            _triangles = new List<int>();

            for (var x = 0; x < squareGrid.Squares.GetLength(0); x++)
            {
                for (var y = 0; y < squareGrid.Squares.GetLength(1); y++)
                {
                    TriangulateSquare(squareGrid.Squares[x, y]);
                }
            }

            // The actual creation of the mesh in Unity3d  Note:  The mesh is actually a Unity3d Component.
            // The gameObjet is created by CaveMapCreator
            var mesh = new Mesh();
            Cave = MeshCommon.GetChildGameObject(genCaveGameObj, caveName + "Base").GetComponent<MeshFilter>();

            if (Cave == null)
                Debug.Log("The cave object, MeshFilter could not be found");
            else
                Cave.mesh = mesh;

            Walls = MeshCommon.GetChildGameObject(genCaveGameObj, caveName + "Wall").GetComponent<MeshFilter>();
            if (Walls == null)
                Debug.Log("The Wall object, MeshFilter could not be found");


            // the mesh methods require an array and not a list
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.RecalculateNormals();
            if (Cave != null) Cave.mesh = mesh;

            // We now want to apply a texture to the image as such we must generate the UV map
            var tileAmount = 10;
            var uvs = new Vector2[_vertices.Count];
            for (var i = 0; i < _vertices.Count; i++)
            {
                var percentX = Mathf.InverseLerp(-map.GetLength(0) / 2.0f * squareSize.x, map.GetLength(0) / 2.0f * squareSize.x, _vertices[i].x) * tileAmount;
                var percentY = Mathf.InverseLerp(-map.GetLength(0) / 2.0f * squareSize.z, map.GetLength(0) / 2.0f * squareSize.z, _vertices[i].z) * tileAmount;
                uvs[i] = new Vector2(percentX, percentY);
            }

            mesh.uv = uvs;

            // Better recalculate the normals
            mesh.RecalculateNormals();

            // Save the mesh as an asset at the path given in the editor menu
            AssetDatabase.CreateAsset(mesh, MeshCommon.CreateUniqueMeshAssetName(caveName));
            AssetDatabase.SaveAssets();


            if (Is2D)
            {
                Generate2DColliders();
            }
            else {
                // Now we create the actual walls that will be seen in Unity3D game space
                CreateWallMesh();
            }

        }

        private void CreateWallMesh()
        {

            CalculateMeshOutlines();

            var wallVertices = new List<Vector3>();
            var wallTriangles = new List<int>();
            var wallMesh = new Mesh();
            float wallHeight = WallHeightWorldUnits;

            foreach (var outline in _outlines)
            {
                for (var i = 0; i < outline.Count - 1; i++)
                {
                    var startIndex = wallVertices.Count;

                    // We must add the 4 vertices's for the wall mesh to a new list for conversion to an array
                    // when we create the actual mesh.
                    wallVertices.Add(_vertices[outline[i]]);                             // Top left
                    wallVertices.Add(_vertices[outline[i + 1]]);                         // Top right
                    wallVertices.Add(_vertices[outline[i]] - Vector3.up * wallHeight);   // bottom left
                    wallVertices.Add(_vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

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
            Walls.mesh = wallMesh;

            // Better recalculate the normals
            wallMesh.RecalculateNormals();

            // Save the mesh as an asset at the path given in the editor menu
            AssetDatabase.CreateAsset(wallMesh, MeshCommon.CreateUniqueMeshAssetName());
            AssetDatabase.SaveAssets();


            if (Walls.gameObject.GetComponent<MeshCollider>() == null)
            {
                var wallCollider = Walls.gameObject.AddComponent<MeshCollider>();
            }

            else
            {
                DestroyImmediate(Walls.gameObject.GetComponent<MeshCollider>());
                var wallCollider = Walls.gameObject.AddComponent<MeshCollider>();
                wallCollider.sharedMesh = wallMesh;
            }
        }

        //TODO: fix the 2D Collider
        private void Generate2DColliders()
        {

            //var currentColliders = gameObject.GetComponents<EdgeCollider2D>();
            //for (var i = 0; i < currentColliders.Length; i++)
            //{
            //    Destroy(currentColliders[i]);
            //}

            //CalculateMeshOutlines();

            //foreach (var outline in _outlines)
            //{
            //    var edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
            //    var edgePoints = new Vector2[outline.Count];

            //    for (var i = 0; i < outline.Count; i++)
            //    {
            //        edgePoints[i] = new Vector2(_vertices[outline[i]].x, _vertices[outline[i]].z);
            //    }
            //    edgeCollider.points = edgePoints;
            //}

        }

        public void TriangulateSquare(Square square)
        {
            switch (square.Configuration)
            {
                case 0:
                    break;

                // 1 points:
                case 1:
                    MeshFromPoints(square.CenterLeft, square.CenterBottom, square.BottomLeft);
                    break;
                case 2:
                    MeshFromPoints(square.BottomRight, square.CenterBottom, square.CenterRight);
                    break;
                case 4:
                    MeshFromPoints(square.TopRight, square.CenterRight, square.CenterTop);
                    break;
                case 8:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterLeft);
                    break;

                /// only two control node have been selected in this square
                // 2 points:
                case 3:
                    MeshFromPoints(square.CenterRight, square.BottomRight, square.BottomLeft, square.CenterLeft);
                    break;
                case 6:
                    MeshFromPoints(square.CenterTop, square.TopRight, square.BottomRight, square.CenterBottom);
                    break;
                case 9:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterBottom, square.BottomLeft);
                    break;
                case 12:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.CenterRight, square.CenterLeft);
                    break;
                case 5:
                    MeshFromPoints(square.CenterTop, square.TopRight, square.CenterRight, square.CenterBottom, square.BottomLeft, square.CenterLeft);
                    break;
                case 10:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterRight, square.BottomRight, square.CenterBottom, square.CenterLeft);
                    break;

                /// only three control nodes have been selected in this square
                // 3 point:
                case 7:
                    MeshFromPoints(square.CenterTop, square.TopRight, square.BottomRight, square.BottomLeft, square.CenterLeft);
                    break;
                case 11:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterRight, square.BottomRight, square.BottomLeft);
                    break;
                case 13:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.CenterRight, square.CenterBottom, square.BottomLeft);
                    break;
                case 14:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.CenterBottom, square.CenterLeft);
                    break;

                /// All four control nodes have been selected in this square
                case 15:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.BottomLeft);

                    // In this case all 4 nodes are active an as such non of the verticies can be part of 
                    //an outside edge.  So let's just mark the hash table to save us from checking them later.
                    _checkedVertices.Add(square.TopLeft.VertexIndex);
                    _checkedVertices.Add(square.TopRight.VertexIndex);
                    _checkedVertices.Add(square.BottomRight.VertexIndex);
                    _checkedVertices.Add(square.BottomLeft.VertexIndex);
                    break;
            }
        }

        private void MeshFromPoints(params Node[] points)
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

        private void CreateTriangle(Node a, Node b, Node c)
        {
            // Unity3D requires that a mesh is made up of two lists - one a list of triangles 
            // the other a list of vertexIndexes
            _triangles.Add(a.VertexIndex);
            _triangles.Add(b.VertexIndex);
            _triangles.Add(c.VertexIndex);

            // Now we add the vertex of every triangle created into the dictionary
            // This will allow us later to look up for any given vertexIndex what triangle it is associated with
            var triangle = new Triangle(a.VertexIndex, b.VertexIndex, c.VertexIndex);
            AddTriangleToDictionary(triangle.VertexIndexA, triangle);
            AddTriangleToDictionary(triangle.VertexIndexB, triangle);
            AddTriangleToDictionary(triangle.VertexIndexC, triangle);
        }

        // Step through every vertex in the mesh and test if it is connected to another vertex as an outside edge
        // if it is follow the path all the way around until it closes the loop by meeting himself.
        // save the complete outside path as a list of vertexes.
        private void CalculateMeshOutlines()
        {
            for (var vertexIndex = 0; vertexIndex < _vertices.Count; vertexIndex++)
            {
                if (!_checkedVertices.Contains(vertexIndex))
                {
                    var newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                    if (newOutlineVertex != -1)
                    {
                        _checkedVertices.Add(vertexIndex);

                        var newOutline = new List<int>();

                        // We now know that this vertexIndex is on an outside edge, we must create a list
                        // so we can hold all points on this outside edge and add this new list to our collection 
                        // of all outline edges.
                        newOutline.Add(vertexIndex);
                        _outlines.Add(newOutline);

                        // Use the recursive method FollowOutLIne() to find all of the nodes in the outside edge
                        //storing the results in outlines[outlines.Count - 1] which is a list containing the current
                        // vertex(s) associated with this specific outside edge.
                        FollowOutline(newOutlineVertex, _outlines.Count - 1);

                        //outlines[outlines.Count - 1] returns a list .Add adds to that list element
                        // The code path will not return to this point until FollowOutline has found and saved all of
                        // the vertex along the out side edge.
                        _outlines[_outlines.Count - 1].Add(vertexIndex);
                    }
                }
            }
        }

        private void FollowOutline(int vertexIndex, int outlineIndex)
        {
            _outlines[outlineIndex].Add(vertexIndex);
            _checkedVertices.Add(vertexIndex);
            var nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

            if (nextVertexIndex != -1)
            {
                FollowOutline(nextVertexIndex, outlineIndex);
            }
        }

        private int GetConnectedOutlineVertex(int vertexIndex)
        {
            // This is the list of all triangles associated with the given vertexIndex.
            var trianglesContainingVertex = _triangleDictionary[vertexIndex];

            // step through all the triangles that the given vertex is connected to
            for (var i = 0; i < trianglesContainingVertex.Count; i++)
            {
                // Step through the 3x vertex(s) that the given triangle contains.
                var triangle = trianglesContainingVertex[i];
                for (var j = 0; j < 3; j++)
                {
                    // vertexB is the second vertex that we want to test the orginal vertexIndex against
                    var vertexB = triangle[j];
                    if (vertexB != vertexIndex && !_checkedVertices.Contains(vertexB))
                    {
                        if (IsOutlineEdge(vertexIndex, vertexB))
                        {
                            return vertexB;
                        }
                    }
                }
            }
            return -1;
        }

        // This method will test if the two given vertex(s) are part of an outline edge.
        private bool IsOutlineEdge(int vertexA, int vertexB)
        {
            /// The dictionary, which was created when all triangles were created for the mesh,
            /// contains a list of all vertex's and the triangles that they are in.
            /// To be an edge each vertex must be part of one and only one triangle.
            /// Step 1: retrieve of the dictionary the list of triangles that are associated with
            /// the given vertex.
            /// Step 2: Test to see if vertexIntexB is contained within the triangle that has vertexIntexA as a member.
            var trianglesContainingVertexA = _triangleDictionary[vertexA];
            var sharedTriangleCount = 0;

            for (var i = 0; i < trianglesContainingVertexA.Count; i++)
            {
                if (trianglesContainingVertexA[i].Contains(vertexB))
                {
                    // increment the count, we must know just how many triangles they share!
                    sharedTriangleCount++;
                    if (sharedTriangleCount > 1)
                    {
                        // If the vertcies share more than one triangle there is no need to continue
                        // we know that they cannot be an outline edge.
                        break;
                    }
                }
            }

            return sharedTriangleCount == 1;
        }

        private void AddTriangleToDictionary(int vertexIndexKey, Triangle triangle)
        {
            // First check to see if the vertexIndexKey is already being held in the dictionary.
            if (_triangleDictionary.ContainsKey(vertexIndexKey))
            {
                // If so this vertexIndexKey is part of another triangle and we should add that to 
                // the list of triangles associated with vertexIndexKey
                _triangleDictionary[vertexIndexKey].Add(triangle);
            }
            else
            {
                // If not, we should create a new dictionary entry and add this triangle to it as the
                // first triangle in this list of triangles for this vertex.
                var triangleList = new List<Triangle>();
                triangleList.Add(triangle);
                _triangleDictionary.Add(vertexIndexKey, triangleList);
            }
        }

        private struct Triangle
        {
            /// <summary>
            ///  A vertexIndex is simply the position of the given vertex within the vertex array.
            /// </summary>
            public int VertexIndexA;
            public int VertexIndexB;
            public int VertexIndexC;

            private int[] _verticies;

            public Triangle(int a, int b, int c)
            {
                VertexIndexA = a;
                VertexIndexB = b;
                VertexIndexC = c;

                _verticies = new int[3];
                _verticies[0] = a;
                _verticies[1] = b;
                _verticies[2] = c;
            }

            /// an indexer to allow us to access the Triangle vertices's like an array
            public int this[int i]
            {
                get
                {
                    return _verticies[i];
                }
            }

            public bool Contains(int vertexIndex)
            {
                // This method is useful to see if the given vertexIndex is contained within the triangle.
                return (VertexIndexA == vertexIndex) || (VertexIndexB == vertexIndex) || (VertexIndexC == vertexIndex);
            }
        }


        private void AssignVertices(Node[] points)
        {
            for (var i = 0; i < points.Length; i++)
            {
                if (points[i].VertexIndex == -1)
                {
                    points[i].VertexIndex = _vertices.Count;
                    _vertices.Add(points[i].Position);
                }
            }
        }


        public class SquareGrid
        {
            public Square[,] Squares;

            public SquareGrid(int[,] map, Vector3 squareSize)
            {
                var nodeCountX = map.GetLength(0);
                var nodeCountY = map.GetLength(1);
                var mapWidth = nodeCountX * squareSize.x;
                var mapHeight = nodeCountY * squareSize.z;


                var controlNodes = new ControlNode[nodeCountX, nodeCountY];

                //
                // Each  ControlNode must be assigned a position in the game space.  We off set the x and z by half the width to center the map.
                // As (0,0,0) is in the screen center.
                // Note: squareSize is distance, in world units between squares.  The map size = squareSize*map.GetLength(x)
                 // verticies.Add(new Vector3((_vSizeX) * x + _offset.x, 0, (_vSizeY) * y + _offset.y) - _origin);               
                for (var x = 0; x < nodeCountX; x++)

                {
                    for (var y = 0; y < nodeCountY; y++)
                    {
                        var pos = new Vector3(-mapWidth / 2 + x * squareSize.x + squareSize.x / 2, 0, -mapHeight / 2 + y * squareSize.z + squareSize.z / 2);
                        controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                    }
                }

                // We now build a 2-dimensional grid of squares which will be used to create the actual cave mesh
                Squares = new Square[nodeCountX - 1, nodeCountY - 1];
                for (var x = 0; x < nodeCountX - 1; x++)
                {
                    for (var y = 0; y < nodeCountY - 1; y++)
                    {
                        // The top line here is a clockwise rotation starting from the upper left.
                        // What is shown in the example is counter clockwise starting on the lower right.
                        // this does not seem consistent with his top left corner rule.
                        // for now I will comment out the clockwise assignment.
                        // squares[x, y] = new Square(controlNodes[x, 0], controlNodes[x + 1, y], controlNodes[x + 1, y + 1], controlNodes[x, y + 1]);
                        Squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1], controlNodes[x + 1, y], controlNodes[x, y]);
                    }
                }
            }
        }

        public class Square
        {
            public ControlNode TopLeft, TopRight, BottomLeft, BottomRight;
            public Node CenterTop, CenterLeft, CenterBottom, CenterRight;

            // This int will hold which control nodes are active or not
            public int Configuration;

            public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomRight, ControlNode bottomLeft)
            {
                TopLeft = topLeft;
                TopRight = topRight;
                BottomRight = bottomRight;
                BottomLeft = bottomLeft;

                CenterTop = TopLeft.Right;
                CenterRight = BottomRight.Above;
                CenterBottom = BottomLeft.Right;
                CenterLeft = BottomLeft.Above;

                // There are 16 possible configurations of squares.  The identification of a specific configuration
                // is critical for mesh creation
                // 
                if (TopLeft.Active)
                    Configuration += 8;
                if (TopRight.Active)
                    Configuration += 4;
                if (BottomRight.Active)
                    Configuration += 2;
                if (BottomLeft.Active)
                    Configuration += 1;
            }
        }

        public class ControlNode : Node
        {
            public bool Active;
            public Node Above, Right;
            public ControlNode(Vector3 pos, bool active, Vector3 squareSize) : base(pos)
            {
                Active = active;
                Above = new Node(Position + Vector3.forward * squareSize.z / 2f);
                Right = new Node(Position + Vector3.right * squareSize.x / 2f);
            }
        }

        public class Node
        {
            public Vector3 Position;

            // The vertexIndex will be set to the array index of the vertex within the mesh.vertexList array
            public int VertexIndex = -1;

            public Node(Vector3 pos)
            {
                Position = pos;
            }
        }


        private void TbdOnDrawGizmos()
        {
            if (squareGrid != null)
            {
                for (var x = 0; x < squareGrid.Squares.GetLength(0); x++)
                {
                    for (var y = 0; y < squareGrid.Squares.GetLength(1); y++)
                    {

                        Gizmos.color = (squareGrid.Squares[x, y].TopLeft.Active) ? Color.black : Color.white;
                        Gizmos.DrawCube(squareGrid.Squares[x, y].TopLeft.Position, Vector3.one * .4f);

                        Gizmos.color = (squareGrid.Squares[x, y].TopRight.Active) ? Color.black : Color.white;
                        Gizmos.DrawCube(squareGrid.Squares[x, y].TopRight.Position, Vector3.one * .4f);

                        Gizmos.color = (squareGrid.Squares[x, y].BottomRight.Active) ? Color.black : Color.white;
                        Gizmos.DrawCube(squareGrid.Squares[x, y].BottomRight.Position, Vector3.one * .4f);

                        Gizmos.color = (squareGrid.Squares[x, y].BottomLeft.Active) ? Color.black : Color.white;
                        Gizmos.DrawCube(squareGrid.Squares[x, y].BottomLeft.Position, Vector3.one * .4f);


                        Gizmos.color = Color.grey;
                        Gizmos.DrawCube(squareGrid.Squares[x, y].CenterTop.Position, Vector3.one * .15f);
                        Gizmos.DrawCube(squareGrid.Squares[x, y].CenterRight.Position, Vector3.one * .15f);
                        Gizmos.DrawCube(squareGrid.Squares[x, y].CenterBottom.Position, Vector3.one * .15f);
                        Gizmos.DrawCube(squareGrid.Squares[x, y].CenterLeft.Position, Vector3.one * .15f);

                    }
                }
            }
        }





    }
}


