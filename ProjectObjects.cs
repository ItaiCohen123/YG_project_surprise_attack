using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
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
        public double penaltyValue;

        public TerrainGraph(TerrainMap map, double penaltyValue)
        {
            this.penaltyValue = penaltyValue;
            this.terrainGraph = new Dictionary<PositionInfo, List<Edge>>();
            BuildGraph(map);

        }
        private void BuildGraph(TerrainMap map)
        {

        } // need to implement


    }
    public class TerrainMap
    {
        public const int GRASS = 0;
        public const int MOUNTAIN = 1;
        public const int CAMERA = 2;
        

        public const int MOUNTAIN_DEC = 20;
        public const int MAP_LENGTH = 60; // *** Place holder
        public const int MAP_WIDTH = 80; // *** Place holder
        public const int CAMERA_RADIUS = 10;
        public const int MAX_CAMERA_NUM = MAP_LENGTH * MAP_WIDTH; // *** Place holder

        public PositionInfo[,] terrainHeightsMap;
        public Camera[] cameraList;
        public int currNumCameras;
        public PositionInfo startPos;
        

        public TerrainMap()
        {

            this.terrainHeightsMap = new PositionInfo[MAP_LENGTH, MAP_WIDTH];
            this.cameraList = new Camera[MAX_CAMERA_NUM];
            this.currNumCameras = 0;
            this.startPos = null;

        }
        public void AddCamera(int row, int col)
        {   
            Camera newCam = new Camera(row, col);
            this.cameraList[this.currNumCameras] = newCam;
            this.terrainHeightsMap[row, col].lastType = this.terrainHeightsMap[row, col].typeOfTerrain;
            this.terrainHeightsMap[row, col].typeOfTerrain = CAMERA;
            this.terrainHeightsMap[row, col].isSafe = false;
            this.terrainHeightsMap[row, col].cam = newCam;

            this.currNumCameras++;

        }
        public void AddCamera(Camera newCam)
        {
            int row = newCam.row;
            int col = newCam.col;
            this.cameraList[this.currNumCameras] = newCam;
            this.terrainHeightsMap[row, col].lastType = this.terrainHeightsMap[row, col].typeOfTerrain;
            this.terrainHeightsMap[row, col].typeOfTerrain = CAMERA;
            this.terrainHeightsMap[row, col].isSafe = false;
            this.terrainHeightsMap[row, col].cam = newCam;

            this.currNumCameras++;

        }
        public void DeleteCamera(Camera camera)
        {
            Camera[] newCameraList = new Camera[MAX_CAMERA_NUM];
            int j = 0;

            for(int i = 0; i < this.currNumCameras; i++)
            {

                if(camera != this.cameraList[i])
                {
                    newCameraList[j++] = this.cameraList[i];
                }


            }

            this.currNumCameras--;
            this.cameraList = newCameraList;

            this.terrainHeightsMap[camera.row, camera.col].typeOfTerrain = this.terrainHeightsMap[camera.row, camera.col].lastType;
            this.terrainHeightsMap[camera.row, camera.col].cam = null;


        }       
        public void InitiateFlatMap()
        {
            this.startPos = null;
            this.currNumCameras = 0;
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

            if (this.startPos == null)
                return true;
            

            Algorithms.ViewshedSingleCam(newCam, this);
            if (!this.startPos.isSafe)
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
        public int height;
        public int xCord;
        public int yCord;
        public int typeOfTerrain;
        public int lastType;
        public bool isSafe;
        public bool isStartingPos;
        public Camera cam;

        public PositionInfo(int y, int x, int height)
        {
            this.yCord = y;
            this.xCord = x;
            this.height = height;
            this.typeOfTerrain = TerrainMap.GRASS;
            this.lastType = TerrainMap.GRASS;
            this.isSafe = true;
            this.isStartingPos = false;
            this.cam = null;
        }



    }
    public class Camera
    {

        public int row;
        public int col;
        
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
        public double weight;
        public double pheromone;

        public const double INITIAL_PHEROMONE = 1; // PLACE HOLDER!!!

        public Edge(PositionInfo target, double weight)
        {
            this.target = target;
            this.weight = weight;
            this.pheromone = INITIAL_PHEROMONE;
        }
        

    }

    public class Ant {
    
    
    
    } // Continue later, also consider a different name for class. Probably have properties: x, y, hasReachedTarget and more...
}
