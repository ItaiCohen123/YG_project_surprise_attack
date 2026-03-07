using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Surprise_Attack_test
{
    internal class Algorithms
    {
        public const int UP = 1;
        public const int DOWN = 2;
        public const int LEFT = 3;
        public const int RIGHT = 4;
        public const int LEFT_UP = 10;
        public const int RIGHT_UP = 20;
        public const int RIGHT_DOWN = 30;
        public const int LEFT_DOWN = 40;






        public static void RunGenerationACO(int antCount, TerrainGraph graph, PositionInfo startPos, PositionInfo targetPos)
        {
            List<Ant> generationAnts = new List<Ant>();

            for(int i = 0; i < antCount; i++)
            {
                Ant currAnt = new Ant(startPos);
                generationAnts.Add(currAnt);


            }

            foreach(Ant ant in generationAnts)
            {
                while (!ant.hadReachedTarget)
                {
                    Edge nextPos = CalculateNextMove(ant, graph);
                    ant.currentPosition = nextPos.target;
                    ant.AddEdgeToRoute(nextPos);

                    if(ant.currentPosition == targetPos)
                    {
                        ant.hadReachedTarget = true;
                    }
                }
                ant.UpdatePheromone();
            }





        }

        
        private static Edge CalculateNextMove(Ant ant, TerrainGraph graph)
        {
            double totalHeuristic = CalculateTotalPheromone(ant, graph)/CalculateTotalWeight(ant, graph);
            double currentHeuristic;
            double probability;
            foreach(Edge edge in graph.terrainGraph[ant.currentPosition])
            {
                if (!ant.nodesVisited.Contains(edge.target))
                {
                    currentHeuristic = edge.pheromone / edge.weight;
                    probability = currentHeuristic / totalHeuristic;

                    if (ChooseEdge(probability))
                        return edge;
                }
            }


            return CalculateNextMove(ant, graph);
        }
        private static bool ChooseEdge(double probability)
        {
            return true;
        }
        private static double CalculateTotalPheromone(Ant ant, TerrainGraph graph)
        {
            double totalPheromone = 0;

            foreach(Edge edge in graph.terrainGraph[ant.currentPosition])
            {
                totalPheromone += edge.pheromone;
            }

            return totalPheromone;
        }
        private static double CalculateTotalWeight(Ant ant, TerrainGraph graph)
        {
            double totalWeight = 0;

            foreach (Edge edge in graph.terrainGraph[ant.currentPosition])
            {
                totalWeight += edge.weight;
            }

            return totalWeight;
        }

        // This function implements the Viewshed algorithm
        // Calculate for each camera, which positions it can see and generate a boolean array
        // with [x,y] = true --> position x, y is not safe
        // and [x, y] = false --> position x, y is safe
        public static TerrainMap ViewShedGlobal(TerrainMap map)
        {

            map.ResetMapSafety();


            for(int i = 0; i < map.currNumCameras; i++)
            {
                map = ViewshedSingleCam(map.cameraList[i], map); // returns global map + the new not safe places by the latest camera

            }

            return map;
        }


        
        // This function calculates a viewshed Map for a single camera
        // Returns the map from before but with the added calculation of this camera
        // *** need to find the optimal algorithm for this problem
        public static TerrainMap ViewshedSingleCam(Camera camera, TerrainMap map)
        {
            int camHeight = map.terrainHeightsMap[camera.row, camera.col].height;


            map = ViewshedSingleDirection(camera.row - 1, camera.col, camHeight, map, UP, Camera.radius);
            map = ViewshedSingleDirection(camera.row + 1, camera.col, camHeight, map, DOWN, Camera.radius);
            map = ViewshedSingleDirection(camera.row, camera.col - 1, camHeight, map, LEFT, Camera.radius);
            map = ViewshedSingleDirection(camera.row, camera.col + 1, camHeight, map, RIGHT, Camera.radius);
            map = ViewshedSingleDirection(camera.row - 1, camera.col - 1, camHeight, map, LEFT_UP, Camera.radius);
            map = ViewshedSingleDirection(camera.row + 1, camera.col - 1, camHeight, map, LEFT_DOWN, Camera.radius);
            map = ViewshedSingleDirection(camera.row - 1, camera.col + 1, camHeight, map, RIGHT_UP, Camera.radius);
            map = ViewshedSingleDirection(camera.row + 1, camera.col + 1, camHeight, map, RIGHT_DOWN, Camera.radius);


            return map;
        }
        
        private static TerrainMap ViewshedSingleDirection(int row, int col,int camHeight, TerrainMap map, int direction, int distance)
        {

            if (!map.InBounds(row, col))
                return map;
            if (map.terrainHeightsMap[row, col].height > camHeight)
                return map;
            if(distance == 0)
                return map;


            map.terrainHeightsMap[row, col].isSafe = false;

            return RecCallByDirection(row, col, camHeight, map, direction, distance - 1);




        }
        private static TerrainMap RecCallByDirection(int row, int col, int camHeigth, TerrainMap map, int direction, int distance)
        {

            switch(direction)
            {

                case UP:
                    map =  ViewshedSingleDirection(row - 1, col, camHeigth, map, direction, distance); break;
                case DOWN:
                    map =  ViewshedSingleDirection(row + 1, col, camHeigth, map, direction, distance); break;
                case LEFT:
                    map =  ViewshedSingleDirection(row, col - 1, camHeigth, map, direction, distance); break;
                case RIGHT:
                    map =  ViewshedSingleDirection(row, col + 1, camHeigth, map, direction, distance); break;
                case LEFT_UP:
                    map = ViewshedSingleDirection(row - 1, col - 1, camHeigth, map, direction, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, RIGHT))
                        map = ViewshedSingleDirection(row - 1, col, camHeigth, map, UP, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, DOWN))
                        map =  ViewshedSingleDirection(row, col - 1, camHeigth, map, LEFT, distance); break;
                case RIGHT_UP:
                    map = ViewshedSingleDirection(row - 1, col + 1, camHeigth, map, direction, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, LEFT))
                        map = ViewshedSingleDirection(row - 1, col, camHeigth, map, UP, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, DOWN))
                        map =  ViewshedSingleDirection(row, col + 1, camHeigth, map, RIGHT, distance); break;
                case RIGHT_DOWN:
                    map = ViewshedSingleDirection(row + 1, col + 1, camHeigth, map, direction, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, LEFT))
                        map = ViewshedSingleDirection(row + 1, col, camHeigth, map, DOWN, distance);
                    if(!CheckVisionBlock(row, col, map, camHeigth, UP))
                        map = ViewshedSingleDirection(row, col + 1, camHeigth, map, RIGHT, distance); break;
                    
                case LEFT_DOWN:
                    map = ViewshedSingleDirection(row + 1, col - 1, camHeigth, map, direction, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, RIGHT))
                        map = ViewshedSingleDirection(row + 1, col, camHeigth, map, DOWN, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, UP))
                        map = ViewshedSingleDirection(row, col - 1, camHeigth, map, LEFT, distance); break;
                default:
                    break;

            }



            return map;
        }
        private static bool CheckVisionBlock(int row, int col, TerrainMap map, int camHeight, int direction)
        {

            switch (direction)
            {

                case UP:
                    if(camHeight < map.terrainHeightsMap[row - 1, col].height)
                    {
                        return true;
                    }
                    return false;
                case DOWN:
                    if (camHeight < map.terrainHeightsMap[row + 1, col].height)
                    {
                        return true;
                    }
                    return false;
                case LEFT:
                    if (camHeight < map.terrainHeightsMap[row, col - 1].height)
                    {
                        return true;
                    }
                    return false;
                case RIGHT:
                    if (camHeight < map.terrainHeightsMap[row, col + 1].height)
                    {
                        return true;
                    }
                    return false;
                default:
                    break;

            }

            return true;

        }

     




    }
}
