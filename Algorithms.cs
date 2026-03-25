using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Surprise_Attack_test
{
    /// <summary>
    /// Provides extension methods for the standard generic List collection.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Shuffles the elements of the list randomly using the Fisher-Yates shuffle algorithm.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to be shuffled.</param>
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

    /// <summary>
    /// Contains the core mathematical, pathfinding (ACO), and field-of-view algorithms for the simulation.
    /// </summary>
    internal class Algorithms
    {
        /// <summary>Directional constant for moving Up.</summary>
        public const int UP = 1;
        /// <summary>Directional constant for moving Down.</summary>
        public const int DOWN = 2;
        /// <summary>Directional constant for moving Left.</summary>
        public const int LEFT = 3;
        /// <summary>Directional constant for moving Right.</summary>
        public const int RIGHT = 4;
        /// <summary>Directional constant for moving Top-Left (Diagonal).</summary>
        public const int LEFT_UP = 10;
        /// <summary>Directional constant for moving Top-Right (Diagonal).</summary>
        public const int RIGHT_UP = 20;
        /// <summary>Directional constant for moving Bottom-Right (Diagonal).</summary>
        public const int RIGHT_DOWN = 30;
        /// <summary>Directional constant for moving Bottom-Left (Diagonal).</summary>
        public const int LEFT_DOWN = 40;

        /// <summary>The current generation number in the ACO simulation.</summary>
        public static int genCount = 1;

        /// <summary>A historical record of the total distance covered in each generation.</summary>
        public static List<double> distances = new List<double>();

        /// <summary>The generation number that produced the shortest route.</summary>
        public static int bestGen = 0;

        /// <summary>The generation number that produced the longest/worst route.</summary>
        public static int worstGen = 0;

        /// <summary>The shortest total distance recorded across all generations.</summary>
        public static double bestGenDist = Double.MaxValue;

        /// <summary>The longest total distance recorded across all generations.</summary>
        public static double worstGenDist = 0;

        /// <summary>A collection containing all ants ever created in the simulation.</summary>
        public static List<Ant> allAnts = new List<Ant>();

        /// <summary>
        /// Runs a split generation where half the ants start at the start position and half start at the target position.
        /// </summary>
        /// <param name="antCount">The total number of ants for this generation.</param>
        /// <param name="graph">The terrain graph used for pathfinding.</param>
        /// <param name="startPos">The main starting position.</param>
        /// <param name="targetPos">The destination position (used as a starting position for the reverse ants).</param>
        /// <returns>A combined list of all ants from both the forward and reverse generations.</returns>
        public static List<Ant> RunSplitGenerationACO(int antCount, TerrainGraph graph, PositionInfo startPos, PositionInfo targetPos)
        {

            List<Ant> normalGenAnts = RunGenerationACO(antCount / 2, graph, startPos, targetPos);
            List<Ant> reverseGenAnts = RunGenerationACO(antCount / 2, graph, targetPos, startPos);

            normalGenAnts.AddRange(reverseGenAnts);

            return normalGenAnts;

        }

        /// <summary>
        /// Simulates a single complete generation of ants traversing from the start to the target.
        /// </summary>
        /// <param name="antCount">The number of ants to deploy in this generation.</param>
        /// <param name="graph">The terrain graph providing valid paths.</param>
        /// <param name="startPos">The position where all ants begin.</param>
        /// <param name="targetPos">The destination position all ants are trying to reach.</param>
        /// <returns>A list of ants representing the completed generation.</returns>
        public static List<Ant> RunGenerationACO(int antCount, TerrainGraph graph, PositionInfo startPos, PositionInfo targetPos)
        {
            List<Ant> generationAnts = new List<Ant>();

            double totalDist = 0;

            for (int i = 0; i < antCount; i++)
            {
                Ant currAnt = new Ant(startPos);
                generationAnts.Add(currAnt);
                allAnts.Add(currAnt);

            }

            foreach (Ant ant in generationAnts)
            {
                while (!ant.hadReachedTarget)
                {
                    Edge nextPos = CalculateNextMove(ant, graph);
                    ant.currentPosition = nextPos.target;
                    ant.AddEdgeToRoute(nextPos);

                    if (ant.currentPosition == targetPos)
                    {
                        ant.hadReachedTarget = true;
                    }

                }
                totalDist += ant.distanceCovered;
            }

            HandleEndOfGen(graph, generationAnts);

            return generationAnts;

        }

        /// <summary>
        /// Calculates and selects the next edge for an ant to travel based on pheromone levels and weights.
        /// </summary>
        /// <param name="ant">The ant currently making a movement decision.</param>
        /// <param name="graph">The graph outlining all connected paths.</param>
        /// <returns>The optimal edge chosen for the next move.</returns>
        private static Edge CalculateNextMove(Ant ant, TerrainGraph graph)
        {
            double totalHeuristic = CalculateTotalPheromone(ant, graph) / Math.Sqrt(CalculateTotalWeight(ant, graph));
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
                    if (probability > maxProb)
                    {
                        maxProb = probability;
                        maxEdge = edge;
                    }

                    if (ChooseEdge(probability))
                    {

                        return edge;
                    }
                }
            }


            return maxEdge;
        }

        /// <summary>
        /// Determines whether an edge is selected based on a calculated probability.
        /// </summary>
        /// <param name="probability">The probability (between 0 and 1) of selecting the edge.</param>
        /// <returns>True if the edge is randomly chosen based on the probability, otherwise false.</returns>
        private static bool ChooseEdge(double probability)
        {
            Random rand = new Random();
            return probability > rand.NextDouble() ? true : false;
        }

        /// <summary>
        /// Calculates the total amount of pheromones present on all valid, safe edges connected to the ant's current position.
        /// </summary>
        /// <param name="ant">The ant currently evaluating its surroundings.</param>
        /// <param name="graph">The terrain graph.</param>
        /// <returns>The sum of all pheromones on adjacent safe edges.</returns>
        private static double CalculateTotalPheromone(Ant ant, TerrainGraph graph)
        {
            double totalPheromone = 0;

            foreach (Edge edge in graph.terrainGraph[ant.currentPosition])
            {
                if (edge.target.isSafe == true)
                    totalPheromone += edge.pheromone;
            }

            return totalPheromone;
        }

        /// <summary>
        /// Calculates the total weight of all valid, safe edges connected to the ant's current position.
        /// </summary>
        /// <param name="ant">The ant currently evaluating its surroundings.</param>
        /// <param name="graph">The terrain graph.</param>
        /// <returns>The sum of all weights on adjacent safe edges.</returns>
        private static double CalculateTotalWeight(Ant ant, TerrainGraph graph)
        {
            double totalWeight = 0;

            foreach (Edge edge in graph.terrainGraph[ant.currentPosition])
            {
                if (edge.target.isSafe == true)
                    totalWeight += edge.weight;
            }

            return totalWeight;
        }

        /// <summary>
        /// Manages post-generation tasks including pheromone evaporation and depositing new pheromones for the best performing ant.
        /// </summary>
        /// <param name="graph">The terrain graph where pheromones will be updated.</param>
        /// <param name="generationAnts">The list of ants from the generation that just completed.</param>
        private static void HandleEndOfGen(TerrainGraph graph, List<Ant> generationAnts)
        {
            Ant bestAntGen;
            Ant.EvaporatePheromone(graph);

            bestAntGen = Ant.BestAnt(generationAnts);

            bestAntGen.UpdatePheromone(graph);

        }

        /// <summary>
        /// Updates the global statistics tracking distance metrics across generations.
        /// </summary>
        /// <param name="totalDist">The total distance recorded in the current generation.</param>
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

        /// <summary>
        /// Implements the global viewshed algorithm to calculate visible (unsafe) areas for all cameras on the map.
        /// </summary>
        /// <param name="map">The map containing cameras and terrain.</param>
        /// <returns>The updated map where visible positions are marked as unsafe.</returns>
        public static TerrainMap ViewShedGlobal(TerrainMap map)
        {

            map.ResetMapSafety();


            for (int i = 0; i < map.cameraList.Count; i++)
            {
                map = ViewshedSingleCam(map.cameraList[i], map); // returns global map + the new not safe places by the latest camera

            }

            return map;
        }

        /// <summary>
        /// Calculates the viewshed (field of view) for a single camera across all 8 directions.
        /// </summary>
        /// <param name="camera">The camera to calculate the view for.</param>
        /// <param name="map">The terrain map to analyze.</param>
        /// <returns>The updated terrain map with the unsafe positions marked for this specific camera.</returns>
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

        /// <summary>
        /// Recursively calculates the viewshed for a specific direction stemming from the camera.
        /// </summary>
        /// <param name="row">The current row being checked.</param>
        /// <param name="col">The current column being checked.</param>
        /// <param name="camHeight">The height of the camera (determines if line of sight is blocked).</param>
        /// <param name="map">The terrain map being modified.</param>
        /// <param name="direction">The direction the viewshed is expanding.</param>
        /// <param name="distance">The remaining distance/radius the camera can see.</param>
        /// <returns>The terrain map updated with unsafe nodes along this directional path.</returns>
        private static TerrainMap ViewshedSingleDirection(int row, int col, int camHeight, TerrainMap map, int direction, int distance)
        {

            if (!map.InBounds(row, col))
                return map;
            if (map.terrainHeightsMap[row, col].height > camHeight)
                return map;
            if (distance == 0)
                return map;

            map.terrainHeightsMap[row, col].isSafe = false;

            return RecCallByDirection(row, col, camHeight, map, direction, distance - 1);

        }

        /// <summary>
        /// Helper function that dictates the recursive expansion of the camera's viewshed based on the specified direction.
        /// </summary>
        /// <param name="row">The current row of the viewshed edge.</param>
        /// <param name="col">The current column of the viewshed edge.</param>
        /// <param name="camHeigth">The height of the camera taking the view.</param>
        /// <param name="map">The terrain map.</param>
        /// <param name="direction">The direction of the current recursive branch.</param>
        /// <param name="distance">The remaining distance the camera can see.</param>
        /// <returns>The map updated by further recursive calls in the view cone.</returns>
        private static TerrainMap RecCallByDirection(int row, int col, int camHeigth, TerrainMap map, int direction, int distance)
        {

            switch (direction)
            {

                case UP:
                    map = ViewshedSingleDirection(row - 1, col, camHeigth, map, direction, distance); return map;
                case DOWN:
                    map = ViewshedSingleDirection(row + 1, col, camHeigth, map, direction, distance); return map;
                case LEFT:
                    map = ViewshedSingleDirection(row, col - 1, camHeigth, map, direction, distance); return map;
                case RIGHT:
                    map = ViewshedSingleDirection(row, col + 1, camHeigth, map, direction, distance); return map;
                case LEFT_UP:
                    map = ViewshedSingleDirection(row - 1, col - 1, camHeigth, map, direction, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, RIGHT))
                        map = ViewshedSingleDirection(row - 1, col, camHeigth, map, UP, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, DOWN))
                        map = ViewshedSingleDirection(row, col - 1, camHeigth, map, LEFT, distance); return map;
                case RIGHT_UP:
                    map = ViewshedSingleDirection(row - 1, col + 1, camHeigth, map, direction, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, LEFT))
                        map = ViewshedSingleDirection(row - 1, col, camHeigth, map, UP, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, DOWN))
                        map = ViewshedSingleDirection(row, col + 1, camHeigth, map, RIGHT, distance); return map;
                case RIGHT_DOWN:
                    map = ViewshedSingleDirection(row + 1, col + 1, camHeigth, map, direction, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, LEFT))
                        map = ViewshedSingleDirection(row + 1, col, camHeigth, map, DOWN, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, UP))
                        map = ViewshedSingleDirection(row, col + 1, camHeigth, map, RIGHT, distance); return map;

                case LEFT_DOWN:
                    map = ViewshedSingleDirection(row + 1, col - 1, camHeigth, map, direction, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, RIGHT))
                        map = ViewshedSingleDirection(row + 1, col, camHeigth, map, DOWN, distance);
                    if (!CheckVisionBlock(row, col, map, camHeigth, UP))
                        map = ViewshedSingleDirection(row, col - 1, camHeigth, map, LEFT, distance); return map;

            }

            return map;
        }

        /// <summary>
        /// Checks if a terrain tile is taller than the camera, effectively blocking the line of sight in a specific direction.
        /// </summary>
        /// <param name="row">The current row being analyzed.</param>
        /// <param name="col">The current column being analyzed.</param>
        /// <param name="map">The terrain map containing height data.</param>
        /// <param name="camHeight">The height of the camera taking the view.</param>
        /// <param name="direction">The direction relative to the current cell to check for an obstruction.</param>
        /// <returns>True if the line of sight is blocked by a taller object, otherwise false.</returns>
        private static bool CheckVisionBlock(int row, int col, TerrainMap map, int camHeight, int direction)
        {

            switch (direction)
            {

                case UP:
                    if (camHeight < map.terrainHeightsMap[row - 1, col].height)
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
            }

            return true;

        }

        /// <summary>
        /// Resets all global algorithm statistics back to their default starting states.
        /// </summary>
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