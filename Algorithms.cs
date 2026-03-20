using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Surprise_Attack_test
{
    public static class ListExtensions
    {
        public static void Shuffle<T>(this List<T> list)
        {
            Random random = new Random(); 
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
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
        public static int genCount = 1;
        public static List<double> distances = new List<double>();
        public static int bestGen = 0;
        public static int worstGen = 0;
        public static double bestGenDist = Double.MaxValue;
        public static double worstGenDist = 0;
        public static List<Ant> allAnts = new List<Ant>();



        public static List<Ant> RunSplitGenerationACO(int antCount, TerrainGraph graph, PositionInfo startPos, PositionInfo targetPos)
        {

            List<Ant> normalGenAnts = RunGenerationACO(antCount/2, graph, startPos, targetPos);
            List<Ant> reverseGenAnts = RunGenerationACO(antCount/2, graph, targetPos, startPos);

            normalGenAnts.AddRange(reverseGenAnts);

            return normalGenAnts;



        }
        public static List<Ant> RunGenerationACO(int antCount, TerrainGraph graph, PositionInfo startPos, PositionInfo targetPos)
        {
            List<Ant> generationAnts = new List<Ant>();
            
            double totalDist = 0;

            for(int i = 0; i < antCount; i++)
            {
                Ant currAnt = new Ant(startPos);
                generationAnts.Add(currAnt);
                allAnts.Add(currAnt);


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
                totalDist += ant.distanceCovered;
            }

            HandleEndOfGen(graph, generationAnts);

            return generationAnts;

        }
        private static Edge CalculateNextMove(Ant ant, TerrainGraph graph)
        {
            double totalHeuristic = CalculateTotalPheromone(ant, graph)/Math.Sqrt(CalculateTotalWeight(ant, graph));
            double currentHeuristic;
            double probability;
            Edge maxEdge = graph.terrainGraph[ant.currentPosition][0];
            double maxProb = -1;
            graph.terrainGraph[ant.currentPosition].Shuffle();

            foreach (Edge edge in graph.terrainGraph[ant.currentPosition])
            {
                if (!ant.nodesVisited.Contains(edge.target) && edge.target.isSafe == true)
                {
                    currentHeuristic = edge.pheromone / Math.Sqrt(edge.weight);
                    probability = currentHeuristic / totalHeuristic;
                    if(probability > maxProb)
                    {
                        maxProb = probability;
                        maxEdge = edge;
                    }

                    if (ChooseEdge(probability))
                        return edge;
                }
            }

            
            return maxEdge;
        }
        private static bool ChooseEdge(double probability)
        {
            Random rand = new Random();
            return probability > rand.NextDouble()? true: false;
        }
        private static double CalculateTotalPheromone(Ant ant, TerrainGraph graph)
        {
            double totalPheromone = 0;

            foreach(Edge edge in graph.terrainGraph[ant.currentPosition])
            {
                if (edge.target.isSafe == true)
                    totalPheromone += edge.pheromone;
            }

            return totalPheromone;
        }
        private static double CalculateTotalWeight(Ant ant, TerrainGraph graph)
        {
            double totalWeight = 0;

            foreach (Edge edge in graph.terrainGraph[ant.currentPosition])
            {
                if(edge.target.isSafe == true)
                    totalWeight += edge.weight;
            }

            return totalWeight;
        }
        private static void HandleEndOfGen(TerrainGraph graph, List<Ant> generationAnts)
        {
            Ant bestAntGen;
            Ant.EvaporatePheromone(graph);

            bestAntGen = Ant.BestAnt(generationAnts);

            bestAntGen.UpdatePheromone();

        }
        private static void UpdateStats(double totalDist)
        {

            if (totalDist < bestGenDist)
            {
                bestGenDist = totalDist;
                bestGen = genCount;
            }
            else if (totalDist > worstGenDist)
            {
                worstGenDist = totalDist;
                worstGen = genCount;
            }



            Algorithms.genCount++;
            Algorithms.distances.Add(totalDist);

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
            map.terrainHeightsMap[camera.row, camera.col].isSafe = false;


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

        public static void ResetParameters()
        {
            genCount = 1;
            distances = new List<double>();
            bestGen = 0;
            worstGen = 0;
            bestGenDist = Double.MaxValue;
            worstGenDist = 0;
            allAnts = new List<Ant>();
        }

    }




}

