using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MapGenerator : MonoBehaviour
{

	// The map height and with  out the border
	[Range(1,255)]
	public int width = 72;
	[Range(1, 255)]
	public int height = 128;

	// We will limit our border to a narrow range
	[Range(1, 20)]
	public int borderSize = 1;

	// The map that will be created.
	int[,] map;

	// Our random number seed
	public string seed;
	public bool useRandomSeed;

	// cellular automata's has a number of thresholds  this is just the first order
	[Range(0, 100)]
	public int randomFillPercent;


	void Start()
	{
		GenerateMap();
	}

	void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			GenerateMap();
		}
	}

	void GenerateMap()
	{

		// Note that the map is created without a border it will be added later.
		map = new int[width, height];

		// Let's fill the map with a "random set" of 1's and 0's
		RandomFillMap();

		// cellular automata's has a number of smoothing functions, in this example we have taken the most basic
		// SmoothMap function.
		for (int i = 0; i < 5; i++)
		{
			SmoothMap();
		}

		// Identify all regions and eliminate those that are too small or too big.
		ProcessMap();

		// We will now create a new grid that will contain the to be added border plus the generated map.
		int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

		for (int x = 0; x < borderedMap.GetLength(0); x++)
		{
			for (int y = 0; y < borderedMap.GetLength(1); y++)
			{
				if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
				{
					borderedMap[x, y] = map[x - borderSize, y - borderSize];
				}
				else
				{
					borderedMap[x, y] = 1;
				}
			}
		}

		// Scripts are components on a game object so we need a reference to it
		MeshGenII meshGen = GetComponent<MeshGenII>();
		//        MeshGenerator meshGen = GetComponent<MeshGenerator>();
		meshGen.GenerateMesh(borderedMap, 1);
	}

	/// <summary>
	/// ProcessMap will remove walls and rooms that do not meet a minimum specification
	/// </summary>
	void ProcessMap()
	{

		// Filter wall regions
		// In this case GetRegions(1) will return a list of lists each of which will contain the tiles that the given 
		// wall is made up of.
		List<List<Coord>> wallRegions = GetRegions(1);
		int wallThresholdSize = 50;

		// Step through each regions list of tiles
		foreach (List<Coord> wallRegion in wallRegions)
		{
			// test the total count against the threshold if it is not met remove the wall
			if (wallRegion.Count < wallThresholdSize)
			{
				foreach (Coord tile in wallRegion)
				{
					// The wall region was found to be too small so add the 
					// space back to the room.
					map[tile.tileX, tile.tileY] = 0;
				}
			}
		}


		// Filter room regions
		// In this case GetRegions(0) will return a list of lists each of which will contain the tiles that the given 
		// room is made up of.
		List<List<Coord>> roomRegions = GetRegions(0);

		// The number of tiles the room is made up of
		int roomThresholdSize = 50;

		// Let us keep a list of all of the rooms that survive the culling
		List<Room> survivingRooms = new List<Room>();

		// Step through each regions list of tiles
		foreach (List<Coord> roomRegion in roomRegions)
		{
			// test the total count against the threshold if it is not met remove the room
			if (roomRegion.Count < roomThresholdSize)
			{
				foreach (Coord tile in roomRegion)
				{
					// The room region was found to be too small so add the 
					// space back to the wall.
					map[tile.tileX, tile.tileY] = 1;
				}
			}

			else {
				// The room was not filtered out so we should add it to the list of valid rooms
				survivingRooms.Add(new Room(roomRegion, map));
			}
		}

		// We need to know which is the largest room so we may tag it as the main room.
		survivingRooms.Sort();
		survivingRooms[0].isMainRoom = true;
		survivingRooms[0].isAccessibleFromMainRoom = true;

		// Now we have a list of all the valid rooms, lets check that they are connected and if not connect them
		// to the closest room available.
		ConnectClosestRooms(survivingRooms);
	}

	/// <summary>
	/// This method will step through the list of valid rooms and find the two tiles that represent the closest 
	/// points between the two rooms
	/// </summary>
	/// <param name="allRooms"></param>
	void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false)
	{

		List<Room> roomListA = new List<Room>();
		List<Room> roomListB = new List<Room>();

		if (forceAccessibilityFromMainRoom)
		{
			foreach (Room room in allRooms)
			{
				if (room.isAccessibleFromMainRoom)
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

		// We will be calculating the distance between all of the different combinations of rooms - intilize to the zero value
		int bestDistance = 0;

		// We must save the set of two tiles that represent the closest points of connection between the two given "best" rooms
		Coord bestTileA = new Coord();
		Coord bestTileB = new Coord();

		// The two rooms that are closest and should be connected.
		Room bestRoomA = new Room();
		Room bestRoomB = new Room();

		// A flag to mark that we have a possible connection
		bool possibleConnectionFound = false;

		// Step through all rooms
		foreach (Room roomA in roomListA)
		{
			
			if (!forceAccessibilityFromMainRoom)
			{
				// Initialize the possible connection found flag to false as we are just starting
				possibleConnectionFound = false;
				if (roomA.connectedRooms.Count > 0)
				{
					continue;
				}
			}

			//  We will compare all rooms to each other
			foreach (Room roomB in roomListB)
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
				for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
				{
					for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
					{
						// extract the exact coordinates for the two tiles being compared
						Coord tileA = roomA.edgeTiles[tileIndexA];
						Coord tileB = roomB.edgeTiles[tileIndexB];

						// calc the square of the distance between the two tiles
						int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));


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


	void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
	{
		Room.ConnectRooms(roomA, roomB);
 //       Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 100);

		List<Coord> line = GetLine(tileA, tileB);
		foreach (Coord c in line)
		{
			DrawCircle(c, 5);
		}
	}

	void DrawCircle(Coord c, int r)
	{
		for (int x = -r; x <= r; x++)
		{
			for (int y = -r; y <= r; y++)
			{
				if (x * x + y * y <= r * r)
				{
					int drawX = c.tileX + x;
					int drawY = c.tileY + y;
					if (IsInMapRange(drawX, drawY))
					{
						map[drawX, drawY] = 0;
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
	List<Coord> GetLine(Coord from, Coord to)
	{
		List<Coord> line = new List<Coord>();

		int x = from.tileX;
		int y = from.tileY;

		int dx = to.tileX - from.tileX;
		int dy = to.tileY - from.tileY;

		bool inverted = false;
		int step = Math.Sign(dx);
		int gradientStep = Math.Sign(dy);

		int longest = Mathf.Abs(dx);
		int shortest = Mathf.Abs(dy);

		if (longest < shortest)
		{
			inverted = true;
			longest = Mathf.Abs(dy);
			shortest = Mathf.Abs(dx);

			step = Math.Sign(dy);
			gradientStep = Math.Sign(dx);
		}

		int gradientAccumulation = longest / 2;
		for (int i = 0; i < longest; i++)
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

	Vector3 CoordToWorldPoint(Coord tile)
	{

		// This assumes each unit of the map coordinates are  one unit in world space.
		// this must be modified to scale to the general case of where a unit of the map
		// is an arbatrary 
		// Vector3 pos = new Vector3(-mapWidth/2 + x * squareSize + squareSize/2, 0, -mapHeight/2 + y * squareSize + squareSize/2);
		return new Vector3(-width / 2 + .5f + tile.tileX, 2, -height / 2 + .5f + tile.tileY);
	}

	/// <summary>
	/// Return the regions of a given type
	/// </summary>
	/// <param name="tileType">  0 = open 1 = wall </param>
	/// <returns> Get regions will return a list of regions of the selected type - Wall or Room</returns>
	List<List<Coord>> GetRegions(int tileType)
	{
		// regions is a list of all of the regions of the given type (wall or room) in the map.
		List<List<Coord>> regions = new List<List<Coord>>();

		// mapFlags will hold a flag indiating weither or not we have inspected the given tile.
		// This is an important part of the flooding algorthim as it will prevent us from double
		// counting regions.
		int[,] mapFlags = new int[width, height];

		// We will step through every tile in the map taking care in what we processes
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				// If the given tile has been inspected or it is of the wrong type skip it.
				if (mapFlags[x, y] == 0 && map[x, y] == tileType)
				{
					// But, if we have not inspeted the given tile and it is of the correct type
					// we must create a new region and fill it with all of the tile that belong to it.
					// this will be done in the GetRegionTiles(x,y) method.
					List<Coord> newRegion = GetRegionTiles(x, y);

					// Add the newly created region to our list
					regions.Add(newRegion);

					// We must not re-inspect a tile within a region so here we will set all of them to inspected.
					foreach (Coord tile in newRegion)
					{
						mapFlags[tile.tileX, tile.tileY] = 1;
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
	List<Coord> GetRegionTiles(int startX, int startY)
	{
		// We will need a list to hold all of the tiles that make up this region
		List<Coord> tiles = new List<Coord>();

		// We must keep track of what cells we tested within the region.  Note this is different 
		// from the mapFlags variable used within the calling function
		int[,] mapFlags = new int[width, height];

		// The initial tile type will be taken from the type of the seed tile.
		int tileType = map[startX, startY];

		// This algorthim is well suited for a Queue instead of a list
		// Note we will be placing a Coord struct into the queue as each tile
		// has two coordinates that define it.
		Queue<Coord> queue = new Queue<Coord>();

		// Put the starting tile into the queue and mark it as inspected.
		queue.Enqueue(new Coord(startX, startY));
		mapFlags[startX, startY] = 1;

		// Keep processing until the queue is empty than return the list of tiles
		while (queue.Count > 0)
		{
			// Every tile has both an x,y coordinat thus we will use a struct to hold this data for each tile.
			// Pull the next tile which is of type Coord out of the Queue  that is  tile is the tile currently being tested.
			Coord tile = queue.Dequeue();

			// Add this tile to the list containing all of the tiles for the region
			tiles.Add(tile);

			// In this algorthim we will test the tiles immeadiatly N, S, E, W from the starting tile
			// Marking them as we inspect them and placing valid tiles that are found back into the queue 
			// For further testing and processing.
			for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
			{
				for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
				{
					// Given the for loop counters we must test is the resulting coordinate is actually within the 
					// Maps range.  Further we do not want to test the diagonals that is take only tiles that are on 
					// the x or y test point coordinates.  The result will be tiles that are {N,S,E,W+center}
					if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX))
					{
						// Now that we know the tile is in the map and is one of the 5 tiles that we want
						// We must test to see if we have looked at them and if the surrounding tile is of the corret type
						// if it is not we will skip it and if it is we will put the tile on the queue for the next iteration.
						if (mapFlags[x, y] == 0 && map[x, y] == tileType)
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
	bool IsInMapRange(int x, int y)
	{
		return x >= 0 && x < width && y >= 0 && y < height;
	}

	void RandomFillMap()
	{
		if (useRandomSeed)
		{
			seed = Time.time.ToString();
		}

		System.Random pseudoRandom = new System.Random(seed.GetHashCode());

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
				{
					map[x, y] = 1;
				}
				else {
					map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
				}
			}
		}
	}

	void SmoothMap()
	{
		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < height; y++)
			{
				int neighbourWallTiles = GetSurroundingWallCount(x, y);

				if (neighbourWallTiles > 4)
					map[x, y] = 1;
				else if (neighbourWallTiles < 4)
					map[x, y] = 0;

			}
		}
	}

	int GetSurroundingWallCount(int gridX, int gridY)
	{
		int wallCount = 0;
		for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
		{
			for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
			{
				if (IsInMapRange(neighbourX, neighbourY))
				{
					if (neighbourX != gridX || neighbourY != gridY)
					{
						wallCount += map[neighbourX, neighbourY];
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
	struct Coord
	{
		public int tileX;
		public int tileY;

		public Coord(int x, int y)
		{
			tileX = x;
			tileY = y;
		}
	}

	/// <summary>
	/// The room class will hold all of the geometric information, tiles, types, locations that define the 
	/// given room.
	/// </summary>
	class Room : IComparable<Room>
	{
		public List<Coord> tiles;
		public List<Coord> edgeTiles;
		public List<Room> connectedRooms;
		public int roomSize;
		public bool isAccessibleFromMainRoom;
		public bool isMainRoom;

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
			tiles = roomTiles;

			// The total number of tiles that make up the room;  Allowing us to 
			// filter out smaller rooms later if we wish
			roomSize = tiles.Count;

			// The list of rooms the given room is connected to.
			connectedRooms = new List<Room>();

			// We need a list to hold all of the tiles that form the outer locus of the room.
			edgeTiles = new List<Coord>();

			// Now step through each tile and check to see if it is an edge
			foreach (Coord tile in tiles)
			{
				// We test each tile that is adjacent {N,S,E,W} .
				for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
				{
					for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
					{
						//  No need to check the diag. tiles
						if (x == tile.tileX || y == tile.tileY)
						{
							if (map[x, y] == 1)
							{
								edgeTiles.Add(tile);
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
			if (!isAccessibleFromMainRoom)
			{
				isAccessibleFromMainRoom = true;
				foreach (Room connectedRoom in connectedRooms)
				{
					connectedRoom.SetAccessibleFromMainRoom();
				}
			}
		}

		/// <summary>
		/// This method marks the two given rooms as conneted and tests to see if either one is connected to the main room
		/// if it is conneted to the main room mark the other room as also connected.
		/// </summary>
		/// <param name="roomA"></param>
		/// <param name="roomB"></param>
		public static void ConnectRooms(Room roomA, Room roomB)
		{
			// Since room A & B are connected if either one is connected to the main room they both should be marked as connected 
			// to the main room
			if (roomA.isAccessibleFromMainRoom)
			{
				roomB.SetAccessibleFromMainRoom();
			}
			else if (roomB.isAccessibleFromMainRoom)
			{
				roomA.SetAccessibleFromMainRoom();
			}
			roomA.connectedRooms.Add(roomB);
			roomB.connectedRooms.Add(roomA);
		}

		/// <summary>
		/// This method simply tests if the given room is currently in the rooms list of connected rooms
		/// </summary>
		/// <param name="otherRoom"></param>
		/// <returns>The room your are testing to see if it in the current room</returns>
		public bool IsConnected(Room otherRoom)
		{
			// As connectedRooms is a list it has a .Contains method
			return connectedRooms.Contains(otherRoom);
		}

		public int CompareTo(Room otherRoom)
		{
			return otherRoom.roomSize.CompareTo(roomSize);
		}

	}
}