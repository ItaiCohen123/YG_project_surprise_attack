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
    
    internal class ProjectObjects
    {
    }
    public class TerrainGraph
    {
        public Dictionary<PositionInfo, List<Edge>> terrainGraph;
        public List<Edge> allEdges;
        public const double PENALTY_VALUE = 150000000; // place holder!!!
        public const double DISTANCE_ADJACENT = 10;

        public TerrainGraph(TerrainMap map)
        {            
            this.terrainGraph = new Dictionary<PositionInfo, List<Edge>>();
            this.allEdges = new List<Edge>();
            BuildGraph(map);

        }
        private void BuildGraph(TerrainMap map)
        {
            int rows = TerrainMap.MAP_LENGTH;
            int cols = TerrainMap.MAP_WIDTH;
            PositionInfo currPos;
            
            

            for(int row = 0; row < rows; row++)
            {
                for(int col = 0; col < cols; col++)
                {
                    currPos = map.terrainHeightsMap[row, col];
                    this.terrainGraph.Add(currPos, new List<Edge>());

                    AddAllEdges(currPos, map);



                }
            }




        } 
        private void AddAllEdges(PositionInfo from, TerrainMap map)
        {
            Edge currEdge;
            PositionInfo targetPos;
            int currRow = from.yCord;
            int currCol = from.xCord;
          

            for(int diffRow = -1; diffRow <= 1; diffRow++) 
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
        private double CalculateWeight(PositionInfo to, PositionInfo from)
        {
            int heightTo = to.height;
            int heightFrom = from.height;
            int penaltyCheck = to.isSafe ? 0 : 1;

            // pythagorean theorem to calculate distance a^2 + b^2 = c^2
            // horizontalDist ^ 2 + verticalDist ^ 2 = distance ^ 2
            double horizontalDistanceSQ = Math.Pow(DISTANCE_ADJACENT, 2);
            double verticalDistanceSQ = Math.Pow(heightTo - heightFrom, 2); // Square the distance so there will not be negative distance;


            double distance = Math.Sqrt(horizontalDistanceSQ +  verticalDistanceSQ);

            return distance * (1 + PENALTY_VALUE * penaltyCheck); 

        }


    }
    public class TerrainMap
    {
        public const int GRASS = 0;
        public const int MOUNTAIN = 1;
        public const int CAMERA = 2;
        

        public const int MOUNTAIN_DEC = 20;
        public static int MAP_LENGTH = 50; // *** Place holder
        public static int MAP_WIDTH = 70; // *** Place holder
        public const int CAMERA_RADIUS = 7;
        public static int MAX_CAMERA_NUM = MAP_LENGTH * MAP_WIDTH; // *** Place holder

        public PositionInfo[,] terrainHeightsMap;
        public List<Camera> cameraList;
        public PositionInfo startPos;
        public PositionInfo targetPos;


        public TerrainMap()
        {

            this.terrainHeightsMap = new PositionInfo[MAP_LENGTH, MAP_WIDTH];
            this.cameraList = new List<Camera>();
            this.startPos = null;
            this.targetPos = null;

        }
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
        public void AddCamera(int row, int col)
        {   
            Camera newCam = new Camera(row, col);
           AddCamera(newCam);

        }
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
        public void DeleteCamera(Camera camera)
        {
           
            this.cameraList.Remove(camera);

            this.terrainHeightsMap[camera.row, camera.col].typeOfTerrain = this.terrainHeightsMap[camera.row, camera.col].lastType;
            this.terrainHeightsMap[camera.row, camera.col].cam = null;


        }       
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
        // function to add a circle like mountain in a certain position, radius and height
        public void AddCircleMountainAtPos(int mountainMaxHight, int centerRowPos, int centerColPos, int radius)
        {

            if (radius == 0 || !InBounds(centerRowPos, centerColPos)) 
                return;

            if (this.terrainHeightsMap[centerRowPos, centerColPos].height >= mountainMaxHight)                
                    return;

            this.terrainHeightsMap[centerRowPos, centerColPos].height = mountainMaxHight;
            if(this.terrainHeightsMap[centerRowPos, centerColPos].typeOfTerrain != CAMERA)
                this.terrainHeightsMap[centerRowPos, centerColPos].typeOfTerrain = MOUNTAIN;
            else
                this.terrainHeightsMap[centerRowPos, centerColPos].lastType = MOUNTAIN;


            CallOtherDirections(mountainMaxHight, centerRowPos, centerColPos, radius);

            
        } 
        public bool InBounds(int row, int col)
        {
            if(row >= MAP_LENGTH || col >= MAP_WIDTH || row < 0 || col < 0)
                return false;

            return true;
        }
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
    public class PositionInfo
    {
        public int height {  get; set; }
        public int xCord {  get; set; }
        public int yCord {  get; set; }
        public int typeOfTerrain{  get; set; }
        public int lastType {  get; set; }
        public bool isSafe {  get; set; }
        public bool isStartingPos {get; set; }
        public bool isTargetPos {get; set; }
        public Camera cam { get; set; }

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
        public PositionInfo()
        {

        }


    }
    public class Camera
    {

        public int row {  get; set; }
        public int col { get; set; }
        
        public static int radius = TerrainMap.CAMERA_RADIUS; // *** Place holder, check  "future_ideas/מצלמות " for more information


        public Camera()
        {
            this.row = 0;
            this.col = 0;
            
        }
        public Camera(int row, int col)
        {
            this.row = row;
            this.col = col;
            
        }
        public String ToString()
        {
            return "Row: " + row + " Column: " + col;
        }




    }
    public class Edge
    {
        public PositionInfo target;
        public PositionInfo from;
        public double weight;
        public double pheromone;

        public const double INITIAL_PHEROMONE = Ant.MAX_PHEROMONE_VALUE * 0.95; // PLACE HOLDER!!!

        public Edge(PositionInfo target,PositionInfo from, double weight)
        {
            this.target = target;
            this.from = from;
            this.weight = weight;
            this.pheromone = INITIAL_PHEROMONE;
        }
        

    }
    public class Ant {


        public const double PHEROMONE_COEFFICIENT = 250000; // Not the final value
        public const double PHEROMONE_EVAPORATION_VALUE = 0.96; // Not the final value
        public const double MAX_PHEROMONE_VALUE = 44;
        public const double MIN_PHEROMONE_VALUE = 0.5;
        public const int ANT_COUNT_GEN = 16;

        public PositionInfo currentPosition;
        public bool hadReachedTarget;
        public List<Edge> edgesVisited;
        public List<PositionInfo> nodesVisited;
        public double distanceCovered;


        public Ant(PositionInfo startPos)
        {
            this.currentPosition = startPos;
            this.hadReachedTarget = false;
            this.edgesVisited = new List<Edge>();
            this.nodesVisited = new List<PositionInfo>();
            this.distanceCovered = 0;
        }
        // Adds an edge the the route of the ant, also considers the weight of the edge and adds it to the total distance covered
        public void AddEdgeToRoute(Edge edge)
        {

            this.edgesVisited.Add(edge);
            this.nodesVisited.Add(edge.from);
            this.distanceCovered += edge.weight;


        }
        // Iterates through the entire edge route an ant that reached the target took
        // and adds pheromones according the quality of the route.
        public void UpdatePheromone()
        {
           
            double pheromoneIncrease = PHEROMONE_COEFFICIENT / this.distanceCovered;

            foreach(Edge edge in edgesVisited)
            {
                edge.pheromone += pheromoneIncrease;

                if (edge.pheromone > MAX_PHEROMONE_VALUE)
                    edge.pheromone = MAX_PHEROMONE_VALUE * 0.9;

            }

            


        }

        // Iterates through all the edges in the graph and decreases the pheromone value by a constant.
        // should be called after each generation
        public static void EvaporatePheromone(TerrainGraph graph)
        {

            foreach(Edge edge in graph.allEdges)
            {
                edge.pheromone *= PHEROMONE_EVAPORATION_VALUE;
                if (edge.pheromone < MIN_PHEROMONE_VALUE)
                    edge.pheromone = MIN_PHEROMONE_VALUE;
            }



        }

        public static Ant BestAnt(List<Ant> ants)
        {
            Ant bestAnt = ants[0];
            double bestDistance = double.MaxValue;
            foreach (Ant ant in ants)
            {
                if(ant.distanceCovered < bestDistance)
                {
                    bestDistance = ant.distanceCovered;
                    bestAnt = ant;
                }
            }

            return bestAnt;


        }
    
    
    } 
    public class SaveMapData
    {
        public int width {  get; set; }
        public int length {  get; set; }
        public PositionInfo[][] mapHeights {  get; set; }
        public List<Camera> cameras {  get; set; }
        public PositionInfo startPos {  get; set; }
        public PositionInfo targetPos { get; set; }

        public SaveMapData(TerrainMap map)
        {
            this.width = TerrainMap.MAP_WIDTH;
            this.length = TerrainMap.MAP_LENGTH;
            this.startPos = map.startPos;
            this.targetPos = map.targetPos;
            this.cameras = map.cameraList;
            this.mapHeights = new PositionInfo[this.length][];

            for(int row = 0; row < this.length; row++)
            {
                this.mapHeights[row] = new PositionInfo[this.width];
                for(int col = 0; col < this.width; col++)
                {
                    this.mapHeights[row][col] = map.terrainHeightsMap[row, col];
                }
            }

            

        }
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
        public SaveMapData()
        {

        }





    }
   
}
