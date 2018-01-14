using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using  UnityEditor;



// TODO: Create the ground mesh in code - 4 vertex
// TODO: FIX 2D collider

namespace Assets.Editor.MeshTools.Scripts
{
    public class CaveMapCreator : UnityEditor.Editor
    {

        // The map height and with out the border
        [Range(1, 255)]
        public int Width = 72;
        [Range(1, 255)]
        public int Height = 128;

        // The conversion to world units
        public float WidthWorldUnits;
        public float HeightWorldUnits;
        public float WallHeightWorldUnits;


        // We will limit our border to a narrow range
        [Range(1, 20)]
        public int BorderSize ;

        // The map that will be created.
        private int[,] _map;
        private bool _createAtOrigin;

        // Our random number seed
        public string Seed;
        public bool UseRandomSeed;

        // Cave game object properties only the name will be saved in the data store
        private string _caveName = "Cave";  // The name without an extension
        public GameObject Cave;             // The GameObject that will hold the cave, and the render components 
        public GameObject Wall;             // The GameObject that will hold the Walls, and the render components 
        public GameObject Ground;             // The GameObject that will hold the Walls, and the render components 
        public GameObject CaveGenerator ;   // The highest level of the GameObjects holding, wall, ground, cave
        public Material WallMaterial;       // The material to be used to render the cave walls
        public Material CaveMaterial;       // // The material to be used to render the cave structure
        public Material GroundMaterial;       // // The material to be used to render the cave ground structure



        // cellular automata's has a number of thresholds  this is just the first order
        public int RandomFillPercent;

        // Needed to get data into and out of the  data store.
        private SerializedProperty _widthProperty;
        private SerializedProperty _heightProperty;
        private SerializedProperty _widthWorldUnitsProperty;
        private SerializedProperty _heightWorldUnitsProperty;
        private SerializedProperty _wallHeightWorldUnitsProperty;
 

        private SerializedProperty _borderSizeProperty;
        private SerializedProperty _mapProperty;
        private SerializedProperty _seedProperty;
        private SerializedProperty _useRandomSeedProperty;
        private SerializedProperty _randomFillPercentProperty;
        private SerializedProperty _caveNameProperty;
        private SerializedProperty _createAtOriginProperty;

        public SerializedProperty WallMaterialProperty;
        public SerializedProperty CaveMaterialProperty;
        public SerializedProperty GroundMaterialProperty;

        [SerializeField]
        private Camera _cam;
        [SerializeField]
        private Camera _lastUsedCam;

        // The path to the storage object for cave data
        private string _storageObjectPath = MeshCommon.PathStorageObject;
        private CaveData _caveData;
        private CaveMeshCreator _caveMeshCreator;


        private void GenerateMap()
        {
            GetCaveData();

            // We must set up a set of game objects to hold the cave.  A parent and two children
            // CaveGenerator as GameObject  with no render component
            //      Cave  with render components
            //      Walls with render components
            CaveGenerator = MeshCommon.CreateUniqueGameObject(_caveName + "Generator", false);
            Cave = MeshCommon.CreateUniqueGameObject(_caveName + "Base", true, false);
            Wall = MeshCommon.CreateUniqueGameObject(_caveName + "Wall", true, false);
            Ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Ground.name = _caveName + "Ground";
            Ground.transform.localScale = new Vector3(WidthWorldUnits/58,1,HeightWorldUnits/97);
 //           Ground.transform.localScale = new Vector3(WidthWorldUnits , 1, HeightWorldUnits );
            Ground.transform.localPosition = new Vector3(0,-WallHeightWorldUnits,0);
            

            Cave.GetComponent<MeshRenderer>().material = CaveMaterial;
            Wall.GetComponent<MeshRenderer>().material = WallMaterial;
            Ground.GetComponent<MeshRenderer>().material = GroundMaterial;

            // We will place the newly created game object, that holds our mesh at either the origin or just in front of the camera.
            if (!_createAtOrigin && _cam)
            {
                Cave.transform.position = _cam.transform.position + _cam.transform.forward*100.0f;
                Wall.transform.position = _cam.transform.position + _cam.transform.forward * 100f;
                Ground.transform.position = _cam.transform.position + _cam.transform.forward * 100.0f - new Vector3(0, WallHeightWorldUnits, 0); 
                CaveGenerator.transform.position = _cam.transform.position + _cam.transform.forward * 100.0f;
            }

            else
            {
                Cave.transform.position = Vector3.zero;
                Wall.transform.position = Vector3.zero;
                Ground.transform.position = new Vector3(0, -WallHeightWorldUnits, 0);
                CaveGenerator.transform.position = Vector3.zero;
            }
        
            // Parent the Cave and walls to the Cave Generator
            Cave.transform.parent = CaveGenerator.transform;
            Wall.transform.parent = CaveGenerator.transform;
            Ground.transform.parent = CaveGenerator.transform;


            // Note that the map is created without a border it will be added later.
            _map = new int[Width, Height];

            // Let's fill the map with a "random set" of 1's and 0's
            RandomFillMap();

            // cellular automata's has a number of smoothing functions, in this example we have taken the most basic
            // SmoothMap function.
            for (var i = 0; i < 5; i++)
            {
                SmoothMap();
            }

            // Identify all regions and eliminate those that are too small or too big.
            ProcessMap();

            // We will now create a new grid that will contain the to be added border plus the generated map.
            var borderedMap = new int[Width + BorderSize * 2, Height + BorderSize * 2];

            for (var x = 0; x < borderedMap.GetLength(0); x++)
            {
                for (var y = 0; y < borderedMap.GetLength(1); y++)
                {
                    if (x >= BorderSize && x < Width + BorderSize && y >= BorderSize && y < Height + BorderSize)
                    {
                        borderedMap[x, y] = _map[x - BorderSize, y - BorderSize];
                    }
                    else
                    {
                        borderedMap[x, y] = 1;
                    }
                }
            }

            // Time to create the mesh, placing the results in the game object called Cave
;           // _caveMeshCreator.GenerateMesh(borderedMap, 1, _caveName, CaveGenerator);

            var scaling = new Vector3(WidthWorldUnits / borderedMap.GetLength(0),1, HeightWorldUnits / borderedMap.GetLength(1));
            _caveMeshCreator.GenerateMesh(borderedMap, scaling, _caveName, CaveGenerator, WallHeightWorldUnits);

            // create the ground plane here
            var newGroundMesh = GenerateGroundPlane(Ground, borderedMap, scaling, _caveName,  WallHeightWorldUnits);
            MeshCommon.GetChildGameObject(CaveGenerator, _caveName + "Ground").GetComponent<MeshFilter>().mesh = newGroundMesh;

            // Save the creation to a prefab
            PrefabUtility.CreatePrefab("Assets/PreFabs/Cave.prefab", CaveGenerator, ReplacePrefabOptions.Default);
        }



        private Mesh GenerateGroundPlane(GameObject ground, int[,] borderedMap, Vector3 scaling, string caveName, float wallHeightWorldUnits)
        {
            var mesh = new Mesh();
            var x = borderedMap.GetLength(0);
            var y = borderedMap.GetLength(1);
            var v = new List<Vector3>();
            var t = new List<int>();

            //v.Add(new Vector3(-x * scaling.x / 2, 0 , y * scaling.y/2));
            //v.Add(new Vector3( x * scaling.x / 2, 0,  y * scaling.y / 2));
            //v.Add(new Vector3( x * scaling.x / 2, 0, -y * scaling.y / 2));
            //v.Add(new Vector3(-x * scaling.x / 2, 0, -y * scaling.y / 2));
            v.Add(new Vector3(-30, 0,  50));
            v.Add(new Vector3( 30, 0,  50));
            v.Add(new Vector3( 30, 0, -50));
            v.Add(new Vector3(-30, 0, -50));


            t.Add(0);
            t.Add(1);
            t.Add(2);

            t.Add(0);
            t.Add(2);
            t.Add(3);

            mesh.vertices  = v.ToArray();
            mesh.triangles = t.ToArray();
            mesh.RecalculateNormals();

            return mesh;


        }


        // <summary>
        // ProcessMap will remove walls and rooms that do not meet a minimum specification
        // </summary>
        private void ProcessMap()
        {

            // Filter wall regions
            // In this case GetRegions(1) will return a list of lists each of which will contain the tiles that the given 
            // wall is made up of.
            var wallRegions = GetRegions(1);
            var wallThresholdSize = 50;

            // Step through each regions list of tiles
            foreach (var wallRegion in wallRegions)
            {
                // test the total count against the threshold if it is not met remove the wall
                if (wallRegion.Count < wallThresholdSize)
                {
                    foreach (var tile in wallRegion)
                    {
                        // The wall region was found to be too small so add the 
                        // space back to the room.
                        _map[tile.TileX, tile.TileY] = 0;
                    }
                }
            }

            // Filter room regions
            // In this case GetRegions(0) will return a list of lists each of which will contain the tiles that the given 
            // room is made up of.
            var roomRegions = GetRegions(0);

            // The number of tiles the room is made up of
            var roomThresholdSize = 50;

            // Let us keep a list of all of the rooms that survive the culling
            var survivingRooms = new List<Room>();

            // Step through each regions list of tiles
            foreach (var roomRegion in roomRegions)
            {
                // test the total count against the threshold if it is not met remove the room
                if (roomRegion.Count < roomThresholdSize)
                {
                    foreach (var tile in roomRegion)
                    {
                        // The room region was found to be too small so add the 
                        // space back to the wall.
                        _map[tile.TileX, tile.TileY] = 1;
                    }
                }

                else {
                    // The room was not filtered out so we should add it to the list of valid rooms
                    survivingRooms.Add(new Room(roomRegion, _map));
                }
            }

            // We need to know which is the largest room so we may tag it as the main room.
            survivingRooms.Sort();
            survivingRooms[0].IsMainRoom = true;
            survivingRooms[0].IsAccessibleFromMainRoom = true;

            // Now we have a list of all the valid rooms, lets check that they are connected and if not connect them
            // to the closest room available.
            ConnectClosestRooms(survivingRooms);
        }

        /// <summary>
        /// This method will step through the list of valid rooms and find the two tiles that represent the closest 
        /// points between the two rooms
        /// </summary>

        private void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
        {

            var roomListA = new List<Room>();
            var roomListB = new List<Room>();

            if (forceAccessibilityFromMainRoom)
            {
                foreach (var room in allRooms)
                {
                    if (room.IsAccessibleFromMainRoom)
                    {
                        roomListB.Add(room);
                    }
                    else {
                        roomListA.Add(room);
                    }
                }
            }
            else {
                roomListA = allRooms;
                roomListB = allRooms;
            }

            // We will be calculating the distance between all of the different combinations of rooms - initialize to the zero value
            var bestDistance = 0;

            // We must save the set of two tiles that represent the closest points of connection between the two given "best" rooms
            var bestTileA = new Coord();
            var bestTileB = new Coord();

            // The two rooms that are closest and should be connected.
            var bestRoomA = new Room();
            var bestRoomB = new Room();

            // A flag to mark that we have a possible connection
            var possibleConnectionFound = false;

            // Step through all rooms
            foreach (var roomA in roomListA)
            {

                if (!forceAccessibilityFromMainRoom)
                {
                    // Initialize the possible connection found flag to false as we are just starting
                    possibleConnectionFound = false;
                    if (roomA.ConnectedRooms.Count > 0)
                    {
                        continue;
                    }
                }

                //  We will compare all rooms to each other
                foreach (var roomB in roomListB)
                {
                    // Skip the trivial case where the rooms are identical.
                    if (roomA == roomB || roomA.IsConnected(roomB))
                    {
                        continue;
                    }


                    // Skip the case where the rooms were already connected.  At the start no rooms are connected but
                    // as we step through the various rooms connections will be made.
                    if (roomA.IsConnected(roomB))
                    {
                        // The rooms are already connected so we want do not want to flag them as a potential new connection
                        possibleConnectionFound = false;
                        break;
                    }

                    // Now that the rooms are vetted we will step through all of the edge tiles looking
                    // for the shortest connection between them.
                    for (var tileIndexA = 0; tileIndexA < roomA.EdgeTiles.Count; tileIndexA++)
                    {
                        for (var tileIndexB = 0; tileIndexB < roomB.EdgeTiles.Count; tileIndexB++)
                        {
                            // extract the exact coordinates for the two tiles being compared
                            var tileA = roomA.EdgeTiles[tileIndexA];
                            var tileB = roomB.EdgeTiles[tileIndexB];

                            // calculate the square of the distance between the two tiles
                            var distanceBetweenRooms = (int)(Mathf.Pow(tileA.TileX - tileB.TileX, 2) + Mathf.Pow(tileA.TileY - tileB.TileY, 2));


                            // At this point in the processing if there is a possible connection
                            // if it is a smaller distance we should save it as the best distance.
                            // The first time through the distance was initialized as zero and the possible connection flag
                            // was set as false so the if statement will capture the best tiles & rooms and set the flag to the 
                            // correct value.
                            if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                            {
                                bestDistance = distanceBetweenRooms;
                                possibleConnectionFound = true;
                                bestTileA = tileA;
                                bestTileB = tileB;
                                bestRoomA = roomA;
                                bestRoomB = roomB;
                            }
                        }
                    }
                }

                if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
                {
                    CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
                }
            }

            if (possibleConnectionFound && forceAccessibilityFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
                ConnectClosestRooms(allRooms, true);
            }

            if (!forceAccessibilityFromMainRoom)
            {
                ConnectClosestRooms(allRooms, true);
            }
        }


        private void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
        {
            Room.ConnectRooms(roomA, roomB);
            //       Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 100);

            var line = GetLine(tileA, tileB);
            foreach (var c in line)
            {
                DrawCircle(c, 5);
            }
        }

        private void DrawCircle(Coord c, int r)
        {
            for (var x = -r; x <= r; x++)
            {
                for (var y = -r; y <= r; y++)
                {
                    if (x * x + y * y <= r * r)
                    {
                        var drawX = c.TileX + x;
                        var drawY = c.TileY + y;
                        if (IsInMapRange(drawX, drawY))
                        {
                            _map[drawX, drawY] = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Bresenham%27s_line_algorithm
        /// This is an implementation of Bresenham's line algorithm - computational efficient, int math and bit shifting only!
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private List<Coord> GetLine(Coord from, Coord to)
        {
            var line = new List<Coord>();

            var x = from.TileX;
            var y = from.TileY;

            var dx = to.TileX - from.TileX;
            var dy = to.TileY - from.TileY;

            var inverted = false;
            var step = Math.Sign(dx);
            var gradientStep = Math.Sign(dy);

            var longest = Mathf.Abs(dx);
            var shortest = Mathf.Abs(dy);

            if (longest < shortest)
            {
                inverted = true;
                longest = Mathf.Abs(dy);
                shortest = Mathf.Abs(dx);

                step = Math.Sign(dy);
                gradientStep = Math.Sign(dx);
            }

            var gradientAccumulation = longest / 2;
            for (var i = 0; i < longest; i++)
            {
                line.Add(new Coord(x, y));

                if (inverted)
                {
                    y += step;
                }
                else {
                    x += step;
                }

                gradientAccumulation += shortest;
                if (gradientAccumulation >= longest)
                {
                    if (inverted)
                    {
                        x += gradientStep;
                    }
                    else {
                        y += gradientStep;
                    }
                    gradientAccumulation -= longest;
                }
            }

            return line;
        }

        private Vector3 CoordToWorldPoint(Coord tile)
        {

            // This assumes each unit of the map coordinates are  one unit in world space.
            // this must be modified to scale to the general case of where a unit of the map
            // is an arbitrary 
            // Vector3 pos = new Vector3(-mapWidth/2 + x * squareSize + squareSize/2, 0, -mapHeight/2 + y * squareSize + squareSize/2);
            return new Vector3(-Width / 2.0f + .5f + tile.TileX, 2, -Height / 2.0f + .5f + tile.TileY);
        }

        /// <summary>
        /// Return the regions of a given type
        /// </summary>
        /// <param name="tileType">  0 = open 1 = wall </param>
        /// <returns> Get regions will return a list of regions of the selected type - Wall or Room</returns>
        private List<List<Coord>> GetRegions(int tileType)
        {
            // regions is a list of all of the regions of the given type (wall or room) in the map.
            var regions = new List<List<Coord>>();

            // mapFlags will hold a flag indicating wither or not we have inspected the given tile.
            // This is an important part of the flooding algorithm as it will prevent us from double
            // counting regions.
            var mapFlags = new int[Width, Height];

            // We will step through every tile in the map taking care in what we processes
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    // If the given tile has been inspected or it is of the wrong type skip it.
                    if (mapFlags[x, y] == 0 && _map[x, y] == tileType)
                    {
                        // But, if we have not inspected the given tile and it is of the correct type
                        // we must create a new region and fill it with all of the tile that belong to it.
                        // this will be done in the GetRegionTiles(x,y) method.
                        var newRegion = GetRegionTiles(x, y);

                        // Add the newly created region to our list
                        regions.Add(newRegion);

                        // We must not re-inspect a tile within a region so here we will set all of them to inspected.
                        foreach (var tile in newRegion)
                        {
                            mapFlags[tile.TileX, tile.TileY] = 1;
                        }
                    }
                }
            }

            return regions;
        }

        /// <summary>
        /// Place all of the tiles that make up a region into a list based on a single starting tile.
        /// </summary>
        /// <param name="startX">An int that gives the x-coordinate of the tile we are inspecting.</param>
        /// <param name="startY">An int that gives the x-coordinate of the tile we are inspecting.</param>
        /// <returns>A list of tiles that make up the given region</returns>
        private List<Coord> GetRegionTiles(int startX, int startY)
        {
            // We will need a list to hold all of the tiles that make up this region
            var tiles = new List<Coord>();

            // We must keep track of what cells we tested within the region.  Note this is different 
            // from the mapFlags variable used within the calling function
            var mapFlags = new int[Width, Height];

            // The initial tile type will be taken from the type of the seed tile.
            var tileType = _map[startX, startY];

            // This algorthim is well suited for a Queue instead of a list
            // Note we will be placing a Coord struct into the queue as each tile
            // has two coordinates that define it.
            var queue = new Queue<Coord>();

            // Put the starting tile into the queue and mark it as inspected.
            queue.Enqueue(new Coord(startX, startY));
            mapFlags[startX, startY] = 1;

            // Keep processing until the queue is empty than return the list of tiles
            while (queue.Count > 0)
            {
                // Every tile has both an x,y coordinate thus we will use a struct to hold this data for each tile.
                // Pull the next tile which is of type Coord out of the Queue  that is  tile is the tile currently being tested.
                var tile = queue.Dequeue();

                // Add this tile to the list containing all of the tiles for the region
                tiles.Add(tile);

                // In this algorithm we will test the tiles immediately N, S, E, W from the starting tile
                // Marking them as we inspect them and placing valid tiles that are found back into the queue 
                // For further testing and processing.
                for (var x = tile.TileX - 1; x <= tile.TileX + 1; x++)
                {
                    for (var y = tile.TileY - 1; y <= tile.TileY + 1; y++)
                    {
                        // Given the for loop counters we must test is the resulting coordinate is actually within the 
                        // Maps range.  Further we do not want to test the diagonals that is take only tiles that are on 
                        // the x or y test point coordinates.  The result will be tiles that are {N,S,E,W+center}
                        if (IsInMapRange(x, y) && (y == tile.TileY || x == tile.TileX))
                        {
                            // Now that we know the tile is in the map and is one of the 5 tiles that we want
                            // We must test to see if we have looked at them and if the surrounding tile is of the correct type
                            // if it is not we will skip it and if it is we will put the tile on the queue for the next iteration.
                            if (mapFlags[x, y] == 0 && _map[x, y] == tileType)
                            {
                                // Mark the tile as having been inspected
                                mapFlags[x, y] = 1;
                                queue.Enqueue(new Coord(x, y));
                            }
                        }
                    }
                }
            }

            return tiles;
        }

        /// <param name="x">The x-coordinate that is being checked</param>
        /// <param name="y">The y-coordinate that is being checked</param>
        /// <returns>a bool 1 = in range 0 outside of the valid map range</returns>
        private bool IsInMapRange(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        private void RandomFillMap()
        {
            if (UseRandomSeed)
            {
                //  Note: Seed = Time.time.ToString(); Does not work in the editor as it is tied to play mode frame rate.
                Seed = EditorApplication.timeSinceStartup.ToString(CultureInfo.CurrentCulture);
            }

            var pseudoRandom = new System.Random(Seed.GetHashCode());

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                    {
                        _map[x, y] = 1;
                    }
                    else
                    {
                        _map[x, y] = (pseudoRandom.Next(0, 100) < RandomFillPercent) ? 1 : 0;
                    }
                }
            }
        }

        private void SmoothMap()
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    var neighbourWallTiles = GetSurroundingWallCount(x, y);

                    if (neighbourWallTiles > 4)
                        _map[x, y] = 1;
                    else if (neighbourWallTiles < 4)
                        _map[x, y] = 0;

                }
            }
        }

        private int GetSurroundingWallCount(int gridX, int gridY)
        {
            var wallCount = 0;
            for (var neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
            {
                for (var neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
                {
                    if (IsInMapRange(neighbourX, neighbourY))
                    {
                        if (neighbourX != gridX || neighbourY != gridY)
                        {
                            wallCount += _map[neighbourX, neighbourY];
                        }
                    }
                    else {
                        wallCount++;
                    }
                }
            }

            return wallCount;
        }

        /// <summary>
        /// This structure will hold the coordinates of any given tile.
        /// </summary>
        private struct Coord
        {
            public int TileX;
            public int TileY;

            public Coord(int x, int y)
            {
                TileX = x;
                TileY = y;
            }
        }

        /// <summary>
        /// The room class will hold all of the geometric information, tiles, types, locations that define the 
        /// given room.
        /// </summary>
        private class Room : IComparable<Room>
        {
            public List<Coord> Tiles;
            public List<Coord> EdgeTiles;
            public List<Room> ConnectedRooms;
            public int RoomSize;
            public bool IsAccessibleFromMainRoom;
            public bool IsMainRoom;

            // We will need a blank constructor overload later
            public Room()
            {
            }

            // Every room is a collection of tiles that sit on the larger map.
            // This constructor requires a copy of the map of all tiles and those
            // tiles that make up the room.
            public Room(List<Coord> roomTiles, int[,] map)
            {
                // tiles will be the private variable the class uses to processes all of the room tiles.
                Tiles = roomTiles;

                // The total number of tiles that make up the room;  Allowing us to 
                // filter out smaller rooms later if we wish
                RoomSize = Tiles.Count;

                // The list of rooms the given room is connected to.
                ConnectedRooms = new List<Room>();

                // We need a list to hold all of the tiles that form the outer locus of the room.
                EdgeTiles = new List<Coord>();

                // Now step through each tile and check to see if it is an edge
                foreach (var tile in Tiles)
                {
                    // We test each tile that is adjacent {N,S,E,W} .
                    for (var x = tile.TileX - 1; x <= tile.TileX + 1; x++)
                    {
                        for (var y = tile.TileY - 1; y <= tile.TileY + 1; y++)
                        {
                            //  No need to check the diag. tiles
                            if (x == tile.TileX || y == tile.TileY)
                            {
                                if (map[x, y] == 1)
                                {
                                    EdgeTiles.Add(tile);
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// This method will add RoomA & RoomB to each others connectedRoom List.
            /// That list tells us all of the rooms that each room is connected to.
            /// </summary>
            /// <param name="roomA">First room to be tested</param>
            /// <param name="roomB">Second room to be tested</param>


            public void SetAccessibleFromMainRoom()
            {
                // If the room is not already marked as accessible from the main room this method will mark this instance of the room and all of the other
                // rooms it is connected to as connected to the main room.
                if (!IsAccessibleFromMainRoom)
                {
                    IsAccessibleFromMainRoom = true;
                    foreach (var connectedRoom in ConnectedRooms)
                    {
                        connectedRoom.SetAccessibleFromMainRoom();
                    }
                }
            }

            /// <summary>
            /// This method marks the two given rooms as connected and tests to see if either one is connected to the main room
            /// if it is connected to the main room mark the other room as also connected.
            /// </summary>
            /// <param name="roomA"></param>
            /// <param name="roomB"></param>
            public static void ConnectRooms(Room roomA, Room roomB)
            {
                // Since room A & B are connected if either one is connected to the main room they both should be marked as connected 
                // to the main room
                if (roomA.IsAccessibleFromMainRoom)
                {
                    roomB.SetAccessibleFromMainRoom();
                }
                else if (roomB.IsAccessibleFromMainRoom)
                {
                    roomA.SetAccessibleFromMainRoom();
                }
                roomA.ConnectedRooms.Add(roomB);
                roomB.ConnectedRooms.Add(roomA);
            }

            /// <summary>
            /// This method simply tests if the given room is currently in the rooms list of connected rooms
            /// </summary>
            /// <param name="otherRoom"></param>
            /// <returns>The room your are testing to see if it in the current room</returns>
            public bool IsConnected(Room otherRoom)
            {
                // As connectedRooms is a list it has a .Contains method
                return ConnectedRooms.Contains(otherRoom);
            }

            public int CompareTo(Room otherRoom)
            {
                return otherRoom.RoomSize.CompareTo(RoomSize);
            }

        }

        private void OnEnable()
        {


            _caveData = (CaveData)EditorGUIUtility.Load(_storageObjectPath + _caveName + "Data"+ MeshCommon.DefaultExtension);
            if (_caveMeshCreator == null)
            {
                _caveMeshCreator = ScriptableObject.CreateInstance<CaveMeshCreator>();
            }
            GetCaveData();
        }

        // Needed to get data into and out of the  data store.

        public void GetCaveData()
        // called by MeshEditor.OnGui  
        {
            Width = _caveData.Width;
            Height = _caveData.Height;
            BorderSize = _caveData.BorderSize;
            _map = _caveData.Map;
            Seed = _caveData.Seed;
            RandomFillPercent = _caveData.RandomFillPercent;
            UseRandomSeed = _caveData.UseRandomSeed;
            _caveName = _caveData.CaveName;
            _createAtOrigin = _caveData.CreateAtOrgin;
            WallMaterial = _caveData.WallMaterial;
            CaveMaterial = _caveData.CaveMaterial;
            GroundMaterial = _caveData.GroundMaterial;
            WidthWorldUnits = _caveData.WidthWorldUnits;
            HeightWorldUnits = _caveData.HeightWorldUnits;
            WallHeightWorldUnits = _caveData.WallHeightWorldUnits;

            DumpData();
        }

        // TODO: Remove all references to DumpData() when done development
        public void DumpData()
        {
            Debug.Log("Width: "+ Width);
            Debug.Log("Height: " + Height);

            Debug.Log("Width(world): " + WidthWorldUnits);
            Debug.Log("Height(world): " + HeightWorldUnits);
            Debug.Log("Wall Height(world): " + WallHeightWorldUnits);

            Debug.Log("BoarderSize: " + BorderSize);
            Debug.Log("_map: " + _map);
            Debug.Log("Random Fill %: " +RandomFillPercent );
            Debug.Log("Use Random Seed: " + UseRandomSeed);
            Debug.Log("_caveName: " + _caveName);
            Debug.Log("_createAtOrigin: " + _createAtOrigin);
            Debug.Log("Wall Material: " + WallMaterial);
            Debug.Log("Cave Material: " + CaveMaterial);
            Debug.Log("Ground Material: " + GroundMaterial);
        }
        void GetCamera()
        {
            _cam = Camera.main;
            Debug.Log("Here is the cam:"+_cam.ToString());
            // Hack because camera.current doesn't return editor camera if scene view doesn't have focus
            if (!_cam)
                _cam = _lastUsedCam;
            else
                _lastUsedCam = _cam;
        }
        public void DrawEditor(SerializedObject serializedMeshData)
        {
            _widthProperty = serializedMeshData.FindProperty("Width");
            _heightProperty = serializedMeshData.FindProperty("Height");

            _widthWorldUnitsProperty = serializedMeshData.FindProperty("WidthWorldUnits");
            _heightWorldUnitsProperty = serializedMeshData.FindProperty("HeightWorldUnits");
            _wallHeightWorldUnitsProperty = serializedMeshData.FindProperty("WallHeightWorldUnits");

            _borderSizeProperty = serializedMeshData.FindProperty("BorderSize");
            _mapProperty = serializedMeshData.FindProperty("Map");

            _seedProperty = serializedMeshData.FindProperty("Seed");
            _useRandomSeedProperty = serializedMeshData.FindProperty("UseRandomSeed");
            _randomFillPercentProperty = serializedMeshData.FindProperty("RandomFillPercent");
            _caveNameProperty = serializedMeshData.FindProperty("CaveName");
            _createAtOriginProperty = serializedMeshData.FindProperty("CreateAtOrgin");

            WallMaterialProperty = serializedMeshData.FindProperty("WallMaterial");
            CaveMaterialProperty = serializedMeshData.FindProperty("CaveMaterial");
            GroundMaterialProperty = serializedMeshData.FindProperty("GroundMaterial");


            // Make sure the serialized Objects are in sync with the values in the editor
            serializedMeshData.Update();


            EditorGUILayout.BeginVertical();
                EditorGUILayout.IntSlider(_widthProperty, 0, 100, new GUIContent("Width"));
                EditorGUILayout.IntSlider(_heightProperty, 0, 100, new GUIContent("Height"));

                EditorGUILayout.PropertyField(_widthWorldUnitsProperty,  new GUIContent("Width (World Units)"));
                EditorGUILayout.PropertyField(_heightWorldUnitsProperty, new GUIContent("Height (World Units"));
                EditorGUILayout.PropertyField(_wallHeightWorldUnitsProperty, new GUIContent("Wall Height (World Units"));

            EditorGUILayout.IntSlider(_borderSizeProperty, 0, 10, new GUIContent("Map Boarder"));
                EditorGUILayout.PropertyField(_caveNameProperty, new GUIContent("Cave Name"));
                EditorGUILayout.PropertyField(_createAtOriginProperty, new GUIContent("Create at Origin?"));
            //  EditorGUILayout.PropertyField(_mapProperty, new GUIContent("Map"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(_seedProperty, new GUIContent(" Seed"));
                EditorGUILayout.PropertyField(_useRandomSeedProperty, new GUIContent("Use a Random Seed"));
                EditorGUILayout.IntSlider(_randomFillPercentProperty,0,100, new GUIContent("Fill %"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical();
                EditorGUILayout.PropertyField(WallMaterialProperty, new GUIContent("Wall Material"));
                EditorGUILayout.PropertyField(CaveMaterialProperty, new GUIContent("Cave Top"));
                EditorGUILayout.PropertyField(GroundMaterialProperty, new GUIContent("Cave Floor"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            var buttonCreateCave = GUILayout.Button("Create Cave");

            // Apply changes to the serialized properties
            serializedMeshData.ApplyModifiedProperties();

            if (buttonCreateCave)
            {
                GetCamera();
                GenerateMap();
            }
            // TODO: Add a display for the mesh and/or UV map to the editor or should that be in a custom inspector?
        }
    }
}