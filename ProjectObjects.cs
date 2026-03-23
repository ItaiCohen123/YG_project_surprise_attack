using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Surprise_Attack_test
{

    /// <summary>
    /// Internal placeholder class for general project objects.
    /// </summary>
    internal class ProjectObjects
    {
    }

    /// <summary>
    /// Represents the navigational graph built from the terrain map, containing the vertices (positions) and edges (paths).
    /// </summary>
    public class TerrainGraph
    {
        /// <summary>A dictionary mapping each position to its available neighboring edges.</summary>
        public Dictionary<PositionInfo, List<Edge>> terrainGraph;

        /// <summary>A list containing every single edge currently existing in the graph.</summary>
        public List<Edge> allEdges;

        /// <summary>The massive penalty value added to paths that are not safe (e.g., restricted or guarded areas).</summary>
        public const double PENALTY_VALUE = 150000000; // place holder!!!

        /// <summary>The base horizontal distance between two adjacent grid nodes.</summary>
        public const double DISTANCE_ADJACENT = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="TerrainGraph"/> class and builds the graph based on the given map.
        /// </summary>
        /// <param name="map">The map used to generate the graph.</param>
        public TerrainGraph(TerrainMap map)
        {
            this.terrainGraph = new Dictionary<PositionInfo, List<Edge>>();
            this.allEdges = new List<Edge>();
            BuildGraph(map);

        }

        /// <summary>
        /// Iterates through every cell in the terrain map and registers it as a node in the graph, calculating paths.
        /// </summary>
        /// <param name="map">The terrain map to convert into a graph.</param>
        private void BuildGraph(TerrainMap map)
        {
            int rows = TerrainMap.MAP_LENGTH;
            int cols = TerrainMap.MAP_WIDTH;
            PositionInfo currPos;



            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    currPos = map.terrainHeightsMap[row, col];
                    this.terrainGraph.Add(currPos, new List<Edge>());

                    AddAllEdges(currPos, map);



                }
            }




        }

        /// <summary>
        /// Finds all valid neighboring positions for a given node and creates connecting edges.
        /// </summary>
        /// <param name="from">The starting position node.</param>
        /// <param name="map">The main terrain map to check for bounds and validity.</param>
        private void AddAllEdges(PositionInfo from, TerrainMap map)
        {
            Edge currEdge;
            PositionInfo targetPos;
            int currRow = from.yCord;
            int currCol = from.xCord;


            for (int diffRow = -1; diffRow <= 1; diffRow++)
            {
                for (int diffCol = -1; diffCol <= 1; diffCol++)
                {
                    if (diffCol == 0 && diffRow == 0)
                        continue;

                    int row = currRow + diffRow;
                    int col = currCol + diffCol;
                    if (map.InBounds(row, col))
                    {
                        targetPos = map.terrainHeightsMap[row, col];
                        currEdge = new Edge(targetPos, from, CalculateWeight(targetPos, from));
                        this.terrainGraph[from].Add(currEdge);
                        this.allEdges.Add(currEdge);


                    }
                }
            }



        }

        /// <summary>
        /// Calculates the traversal weight (distance/cost) between two points considering height differences and safety penalties.
        /// </summary>
        /// <param name="to">The target destination position.</param>
        /// <param name="from">The starting position.</param>
        /// <returns>The calculated edge weight.</returns>
        private double CalculateWeight(PositionInfo to, PositionInfo from)
        {
            int heightTo = to.height;
            int heightFrom = from.height;
            int penaltyCheck = to.isSafe ? 0 : 1;

            // pythagorean theorem to calculate distance a^2 + b^2 = c^2
            // horizontalDist ^ 2 + verticalDist ^ 2 = distance ^ 2
            double horizontalDistanceSQ = Math.Pow(DISTANCE_ADJACENT, 2);
            double verticalDistanceSQ = Math.Pow(heightTo - heightFrom, 2); // Square the distance so there will not be negative distance;


            double distance = Math.Sqrt(horizontalDistanceSQ + verticalDistanceSQ);

            return distance * (1 + PENALTY_VALUE * penaltyCheck);

        }


    }

    /// <summary>
    /// Represents the physical terrain, storing heights, obstacles, cameras, and key locations.
    /// </summary>
    public class TerrainMap
    {
        /// <summary>Identifier for standard grass terrain.</summary>
        public const int GRASS = 0;
        /// <summary>Identifier for mountain terrain.</summary>
        public const int MOUNTAIN = 1;
        /// <summary>Identifier for a camera obstacle.</summary>
        public const int CAMERA = 2;

        /// <summary>The decrement value used when drawing slopes of mountains.</summary>
        public const int MOUNTAIN_DEC = 20;
        /// <summary>The length (number of rows) of the map.</summary>
        public static int MAP_LENGTH = 50; // *** Place holder
        /// <summary>The width (number of columns) of the map.</summary>
        public static int MAP_WIDTH = 70; // *** Place holder
        /// <summary>The vision radius for standard cameras.</summary>
        public const int CAMERA_RADIUS = 7;
        /// <summary>The absolute maximum number of cameras allowed on the map.</summary>
        public static int MAX_CAMERA_NUM = MAP_LENGTH * MAP_WIDTH; // *** Place holder

        /// <summary>A 2D array storing the height and status data for every coordinate.</summary>
        public PositionInfo[,] terrainHeightsMap;
        /// <summary>A list of all active cameras on the map.</summary>
        public List<Camera> cameraList;
        /// <summary>The designated starting position for the ants.</summary>
        public PositionInfo startPos;
        /// <summary>The designated target/destination position for the ants.</summary>
        public PositionInfo targetPos;

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="TerrainMap"/> class.
        /// </summary>
        public TerrainMap()
        {

            this.terrainHeightsMap = new PositionInfo[MAP_LENGTH, MAP_WIDTH];
            this.cameraList = new List<Camera>();
            this.startPos = null;
            this.targetPos = null;

        }

        /// <summary>
        /// Overwrites the current map state with data loaded from a saved file.
        /// </summary>
        /// <param name="saveMapData">The saved map data to load.</param>
        public void LoadMap(SaveMapData saveMapData)
        {
            InitiateFlatMap();


            TerrainMap.MAP_WIDTH = saveMapData.width;
            TerrainMap.MAP_LENGTH = saveMapData.length;

            this.cameraList = saveMapData.cameras;

            for (int row = 0; row < TerrainMap.MAP_LENGTH; row++)
            {
                for (int col = 0; col < TerrainMap.MAP_WIDTH; col++)
                {
                    this.terrainHeightsMap[row, col] = saveMapData.mapHeights[row][col];
                }
            }

            this.startPos = this.terrainHeightsMap[saveMapData.startPos.yCord, saveMapData.startPos.xCord];
            this.targetPos = this.terrainHeightsMap[saveMapData.targetPos.yCord, saveMapData.targetPos.xCord];

        }

        /// <summary>
        /// Creates a new camera at the specified coordinates and adds it to the map.
        /// </summary>
        /// <param name="row">The row index for the new camera.</param>
        /// <param name="col">The column index for the new camera.</param>
        public void AddCamera(int row, int col)
        {
            Camera newCam = new Camera(row, col);
            AddCamera(newCam);

        }

        /// <summary>
        /// Adds a predefined camera object to the map and updates the terrain tile type.
        /// </summary>
        /// <param name="newCam">The camera to be added.</param>
        public void AddCamera(Camera newCam)
        {
            int row = newCam.row;
            int col = newCam.col;
            this.cameraList.Add(newCam);
            this.terrainHeightsMap[row, col].lastType = this.terrainHeightsMap[row, col].typeOfTerrain;
            this.terrainHeightsMap[row, col].typeOfTerrain = CAMERA;
            this.terrainHeightsMap[row, col].isSafe = false;
            this.terrainHeightsMap[row, col].cam = newCam;


        }

        /// <summary>
        /// Removes a camera from the map and restores the underlying terrain type.
        /// </summary>
        /// <param name="camera">The camera to remove.</param>
        public void DeleteCamera(Camera camera)
        {

            this.cameraList.Remove(camera);

            this.terrainHeightsMap[camera.row, camera.col].typeOfTerrain = this.terrainHeightsMap[camera.row, camera.col].lastType;
            this.terrainHeightsMap[camera.row, camera.col].cam = null;


        }

        /// <summary>
        /// Clears all map objects and resets all terrain heights to create a flat, blank map.
        /// </summary>
        public void InitiateFlatMap()
        {
            this.startPos = null;
            this.targetPos = null;
            this.cameraList.Clear();
            for (int row = 0; row < this.terrainHeightsMap.GetLength(0); row++)
            {
                for (int col = 0; col < this.terrainHeightsMap.GetLength(1); col++)
                {
                    this.terrainHeightsMap[row, col] = new PositionInfo(row, col, 0); // create a flat map with all heights = 0


                }
            }

        }

        /// <summary>
        /// Recursively draws a mountain with a circular footprint at a specific position, radius, and height.
        /// </summary>
        /// <param name="mountainMaxHight">The starting center height of the mountain.</param>
        /// <param name="centerRowPos">The row position of the center.</param>
        /// <param name="centerColPos">The column position of the center.</param>
        /// <param name="radius">The radius/spread of the mountain.</param>
        // function to add a circle like mountain in a certain position, radius and height
        public void AddCircleMountainAtPos(int mountainMaxHight, int centerRowPos, int centerColPos, int radius)
        {

            if (radius == 0 || !InBounds(centerRowPos, centerColPos))
                return;

            if (this.terrainHeightsMap[centerRowPos, centerColPos].height >= mountainMaxHight)
                return;

            this.terrainHeightsMap[centerRowPos, centerColPos].height = mountainMaxHight;
            if (this.terrainHeightsMap[centerRowPos, centerColPos].typeOfTerrain != CAMERA)
                this.terrainHeightsMap[centerRowPos, centerColPos].typeOfTerrain = MOUNTAIN;
            else
                this.terrainHeightsMap[centerRowPos, centerColPos].lastType = MOUNTAIN;


            CallOtherDirections(mountainMaxHight, centerRowPos, centerColPos, radius);


        }

        /// <summary>
        /// Checks whether the given coordinates are within the boundaries of the map grid.
        /// </summary>
        /// <param name="row">The row index to check.</param>
        /// <param name="col">The column index to check.</param>
        /// <returns>True if the coordinates are inside the map; otherwise, false.</returns>
        public bool InBounds(int row, int col)
        {
            if (row >= MAP_LENGTH || col >= MAP_WIDTH || row < 0 || col < 0)
                return false;

            return true;
        }

        /// <summary>
        /// Resets the safety status of all tiles on the map to safe.
        /// </summary>
        public void ResetMapSafety()
        {

            for (int row = 0; row < this.terrainHeightsMap.GetLength(0); row++)
            {
                for (int col = 0; col < this.terrainHeightsMap.GetLength(1); col++)
                {
                    this.terrainHeightsMap[row, col].isSafe = true;


                }
            }



        }

        /// <summary>
        /// Evaluates if placing a new camera at the specified coordinates is valid (e.g., does not immediately expose the start or target positions).
        /// </summary>
        /// <param name="camY">The row coordinate for the camera.</param>
        /// <param name="camX">The column coordinate for the camera.</param>
        /// <returns>True if the new camera placement is allowed; otherwise, false.</returns>
        public bool CheckNewCamAllowed(int camY, int camX)
        {
            // return true if new cam position allowed, false otherwise
            Camera newCam = new Camera(camY, camX);
            AddCamera(newCam);




            Algorithms.ViewshedSingleCam(newCam, this);
            if ((this.startPos != null && !this.startPos.isSafe) || (this.targetPos != null && !this.targetPos.isSafe))
            {
                DeleteCamera(newCam);
                Algorithms.ViewShedGlobal(this);
                return false;
            }

            return true;



        }

        /// <summary>
        /// Helper function for mountain generation that recursively calls for height updates in all 8 surrounding directions.
        /// </summary>
        /// <param name="mountainMaxHight">The height of the current center point.</param>
        /// <param name="centerRowPos">The row position.</param>
        /// <param name="centerColPos">The column position.</param>
        /// <param name="radius">The remaining radius to expand.</param>
        private void CallOtherDirections(int mountainMaxHight, int centerRowPos, int centerColPos, int radius)
        {

            AddCircleMountainAtPos(mountainMaxHight - MOUNTAIN_DEC, centerRowPos + 1, centerColPos + 1, radius - 1);
            AddCircleMountainAtPos(mountainMaxHight - MOUNTAIN_DEC, centerRowPos, centerColPos + 1, radius - 1);
            AddCircleMountainAtPos(mountainMaxHight - MOUNTAIN_DEC, centerRowPos + 1, centerColPos, radius - 1);
            AddCircleMountainAtPos(mountainMaxHight - MOUNTAIN_DEC, centerRowPos - 1, centerColPos + 1, radius - 1);
            AddCircleMountainAtPos(mountainMaxHight - MOUNTAIN_DEC, centerRowPos + 1, centerColPos - 1, radius - 1);
            AddCircleMountainAtPos(mountainMaxHight - MOUNTAIN_DEC, centerRowPos, centerColPos - 1, radius - 1);
            AddCircleMountainAtPos(mountainMaxHight - MOUNTAIN_DEC, centerRowPos - 1, centerColPos - 1, radius - 1);
            AddCircleMountainAtPos(mountainMaxHight - MOUNTAIN_DEC, centerRowPos - 1, centerColPos, radius - 1);

        }






    }

    /// <summary>
    /// Represents a single tile/node on the map, holding its coordinates, height, type, and status.
    /// </summary>
    public class PositionInfo
    {
        /// <summary>Gets or sets the height of the tile.</summary>
        public int height { get; set; }
        /// <summary>Gets or sets the X (column) coordinate.</summary>
        public int xCord { get; set; }
        /// <summary>Gets or sets the Y (row) coordinate.</summary>
        public int yCord { get; set; }
        /// <summary>Gets or sets the current type of terrain on this tile.</summary>
        public int typeOfTerrain { get; set; }
        /// <summary>Gets or sets the previous terrain type (used to restore terrain when obstacles are removed).</summary>
        public int lastType { get; set; }
        /// <summary>Gets or sets whether this tile is hidden from cameras and safe for ants to traverse.</summary>
        public bool isSafe { get; set; }
        /// <summary>Gets or sets whether this tile is the designated starting point.</summary>
        public bool isStartingPos { get; set; }
        /// <summary>Gets or sets whether this tile is the designated target/end point.</summary>
        public bool isTargetPos { get; set; }
        /// <summary>Gets or sets the camera instance if one is located on this tile.</summary>
        public Camera cam { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionInfo"/> class with basic coordinates and height.
        /// </summary>
        /// <param name="y">The Y (row) coordinate.</param>
        /// <param name="x">The X (column) coordinate.</param>
        /// <param name="height">The elevation/height of the tile.</param>
        public PositionInfo(int y, int x, int height)
        {
            this.yCord = y;
            this.xCord = x;
            this.height = height;
            this.typeOfTerrain = TerrainMap.GRASS;
            this.lastType = TerrainMap.GRASS;
            this.isSafe = true;
            this.isStartingPos = false;
            this.isTargetPos = false;
            this.cam = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PositionInfo"/> class with all properties specified.
        /// </summary>
        /// <param name="y">The Y coordinate.</param>
        /// <param name="x">The X coordinate.</param>
        /// <param name="height">The elevation.</param>
        /// <param name="typeOfTerrain">The active terrain type.</param>
        /// <param name="lastType">The previous terrain type.</param>
        /// <param name="isSafe">The safety status against camera vision.</param>
        /// <param name="isStartingPos">True if this is the start position.</param>
        /// <param name="isTargetPos">True if this is the target position.</param>
        /// <param name="cam">The camera object placed here, if any.</param>
        public PositionInfo(int y, int x, int height, int typeOfTerrain, int lastType, bool isSafe, bool isStartingPos, bool isTargetPos, Camera cam)
        {
            this.yCord = y;
            this.xCord = x;
            this.height = height;
            this.typeOfTerrain = typeOfTerrain;
            this.lastType = lastType;
            this.isSafe = isSafe;
            this.isStartingPos = isStartingPos;
            this.isTargetPos = isTargetPos;
            this.cam = cam;
        }

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="PositionInfo"/> class.
        /// </summary>
        public PositionInfo()
        {

        }


    }

    /// <summary>
    /// Represents an enemy camera obstacle placed on the terrain map that projects a viewshed.
    /// </summary>
    public class Camera
    {

        /// <summary>Gets or sets the row position of the camera.</summary>
        public int row { get; set; }
        /// <summary>Gets or sets the column position of the camera.</summary>
        public int col { get; set; }

        /// <summary>The base radius for how far the camera can see.</summary>
        public static int radius = TerrainMap.CAMERA_RADIUS; // *** Place holder, check  "future_ideas/מצלמות " for more information


        /// <summary>
        /// Initializes a new instance of the <see cref="Camera"/> class at origin (0,0).
        /// </summary>
        public Camera()
        {
            this.row = 0;
            this.col = 0;

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Camera"/> class at specific coordinates.
        /// </summary>
        /// <param name="row">The row index where the camera is placed.</param>
        /// <param name="col">The column index where the camera is placed.</param>
        public Camera(int row, int col)
        {
            this.row = row;
            this.col = col;

        }

        /// <summary>
        /// Returns a string representation of the camera's location.
        /// </summary>
        /// <returns>A string detailing the camera's row and column.</returns>
        public String ToString()
        {
            return "Row: " + row + " Column: " + col;
        }




    }

    /// <summary>
    /// Represents a traversable path between two adjacent <see cref="PositionInfo"/> nodes in the terrain graph.
    /// </summary>
    public class Edge
    {
        /// <summary>The target node that this edge connects to.</summary>
        public PositionInfo target;
        /// <summary>The starting node that this edge stems from.</summary>
        public PositionInfo from;
        /// <summary>The cost/distance of traversing this edge.</summary>
        public double weight;
        /// <summary>The current pheromone level deposited on this edge.</summary>
        public double pheromone;

        /// <summary>The default starting pheromone level given to a newly created edge.</summary>
        public const double INITIAL_PHEROMONE = Ant.MAX_PHEROMONE_VALUE * 0.95; // PLACE HOLDER!!!

        /// <summary>
        /// Initializes a new instance of the <see cref="Edge"/> class.
        /// </summary>
        /// <param name="target">The destination node.</param>
        /// <param name="from">The origin node.</param>
        /// <param name="weight">The calculated cost of this path.</param>
        public Edge(PositionInfo target, PositionInfo from, double weight)
        {
            this.target = target;
            this.from = from;
            this.weight = weight;
            this.pheromone = INITIAL_PHEROMONE;
        }


    }

    /// <summary>
    /// Represents a single ant agent used to traverse the terrain graph in the ACO algorithm.
    /// </summary>
    public class Ant
    {


        /// <summary>Multiplier used when updating pheromones based on the ant's performance.</summary>
        public const double PHEROMONE_COEFFICIENT = 250000; // Not the final value
        /// <summary>The rate at which pheromones decay on edges after every generation.</summary>
        public const double PHEROMONE_EVAPORATION_VALUE = 0.96; // Not the final value
        /// <summary>The maximum allowed limit for pheromones on any single edge.</summary>
        public const double MAX_PHEROMONE_VALUE = 44;
        /// <summary>The minimum allowed threshold for pheromones to prevent paths from becoming completely ignored.</summary>
        public const double MIN_PHEROMONE_VALUE = 0.5;
        /// <summary>The number of ants spawned per standard generation.</summary>
        public const int ANT_COUNT_GEN = 16;

        /// <summary>The current active position of the ant in the graph.</summary>
        public PositionInfo currentPosition;
        /// <summary>Indicates whether the ant successfully found the target.</summary>
        public bool hadReachedTarget;
        /// <summary>A historical list of all edges the ant has traversed.</summary>
        public List<Edge> edgesVisited;
        /// <summary>A historical list of all nodes the ant has stepped on.</summary>
        public List<PositionInfo> nodesVisited;
        /// <summary>The cumulative weight/distance the ant has traveled.</summary>
        public double distanceCovered;


        /// <summary>
        /// Initializes a new instance of the <see cref="Ant"/> class at the given starting position.
        /// </summary>
        /// <param name="startPos">The initial starting node.</param>
        public Ant(PositionInfo startPos)
        {
            this.currentPosition = startPos;
            this.hadReachedTarget = false;
            this.edgesVisited = new List<Edge>();
            this.nodesVisited = new List<PositionInfo>();
            this.distanceCovered = 0;
        }

        /// <summary>
        /// Records a traversal over an edge, updating the ant's path history and cumulative distance.
        /// </summary>
        /// <param name="edge">The edge that was just traversed.</param>
        // Adds an edge the the route of the ant, also considers the weight of the edge and adds it to the total distance covered
        public void AddEdgeToRoute(Edge edge)
        {

            this.edgesVisited.Add(edge);
            this.nodesVisited.Add(edge.from);
            this.distanceCovered += edge.weight;


        }

        /// <summary>
        /// Deposits pheromones along the ant's completed route, scaled by how efficient the route was.
        /// </summary>
        // Iterates through the entire edge route an ant that reached the target took
        // and adds pheromones according the quality of the route.
        public void UpdatePheromone()
        {

            double pheromoneIncrease = PHEROMONE_COEFFICIENT / this.distanceCovered;

            foreach (Edge edge in edgesVisited)
            {
                edge.pheromone += pheromoneIncrease;

                if (edge.pheromone > MAX_PHEROMONE_VALUE)
                    edge.pheromone = MAX_PHEROMONE_VALUE * 0.9;

            }




        }

        /// <summary>
        /// Decays the pheromone values across all edges in the entire graph to simulate time passing.
        /// </summary>
        /// <param name="graph">The terrain graph containing the edges to evaporate.</param>
        // Iterates through all the edges in the graph and decreases the pheromone value by a constant.
        // should be called after each generation
        public static void EvaporatePheromone(TerrainGraph graph)
        {

            foreach (Edge edge in graph.allEdges)
            {
                edge.pheromone *= PHEROMONE_EVAPORATION_VALUE;
                if (edge.pheromone < MIN_PHEROMONE_VALUE)
                    edge.pheromone = MIN_PHEROMONE_VALUE;
            }



        }

        /// <summary>
        /// Analyzes a list of ants and identifies the one that covered the shortest distance.
        /// </summary>
        /// <param name="ants">The list of ants to evaluate.</param>
        /// <returns>The ant that found the most optimal route.</returns>
        public static Ant BestAnt(List<Ant> ants)
        {
            Ant bestAnt = ants[0];
            double bestDistance = double.MaxValue;
            foreach (Ant ant in ants)
            {
                if (ant.distanceCovered < bestDistance)
                {
                    bestDistance = ant.distanceCovered;
                    bestAnt = ant;
                }
            }

            return bestAnt;


        }


    }

    /// <summary>
    /// A structured data container designed for serializing and deserializing terrain maps to/from files.
    /// </summary>
    public class SaveMapData
    {
        /// <summary>Gets or sets the width of the saved map.</summary>
        public int width { get; set; }
        /// <summary>Gets or sets the length of the saved map.</summary>
        public int length { get; set; }
        /// <summary>Gets or sets the jagged array containing all the position tile data.</summary>
        public PositionInfo[][] mapHeights { get; set; }
        /// <summary>Gets or sets the list of cameras saved on the map.</summary>
        public List<Camera> cameras { get; set; }
        /// <summary>Gets or sets the starting position data.</summary>
        public PositionInfo startPos { get; set; }
        /// <summary>Gets or sets the target position data.</summary>
        public PositionInfo targetPos { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveMapData"/> class by copying data directly from an active terrain map.
        /// </summary>
        /// <param name="map">The active map to serialize.</param>
        public SaveMapData(TerrainMap map)
        {
            this.width = TerrainMap.MAP_WIDTH;
            this.length = TerrainMap.MAP_LENGTH;
            this.startPos = map.startPos;
            this.targetPos = map.targetPos;
            this.cameras = map.cameraList;
            this.mapHeights = new PositionInfo[this.length][];

            for (int row = 0; row < this.length; row++)
            {
                this.mapHeights[row] = new PositionInfo[this.width];
                for (int col = 0; col < this.width; col++)
                {
                    this.mapHeights[row][col] = map.terrainHeightsMap[row, col];
                }
            }



        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveMapData"/> class with explicitly provided parameters.
        /// </summary>
        /// <param name="width">The width of the map.</param>
        /// <param name="length">The length of the map.</param>
        /// <param name="startPos">The starting coordinate node.</param>
        /// <param name="targetPos">The target coordinate node.</param>
        /// <param name="cameras">The list of active cameras.</param>
        /// <param name="mapHeights">The jagged array representing the grid data.</param>
        public SaveMapData(int width, int length, PositionInfo startPos, PositionInfo targetPos, List<Camera> cameras, PositionInfo[][] mapHeights)
        {
            this.width = width;
            this.length = length;
            this.startPos = startPos;
            this.targetPos = targetPos;
            this.cameras = cameras;
            this.mapHeights = new PositionInfo[this.length][];

            for (int row = 0; row < this.length; row++)
            {
                this.mapHeights[row] = new PositionInfo[this.width];
                for (int col = 0; col < this.width; col++)
                {
                    this.mapHeights[row][col] = mapHeights[row][col];
                }
            }
        }

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="SaveMapData"/> class (used during JSON deserialization).
        /// </summary>
        public SaveMapData()
        {

        }





    }

}