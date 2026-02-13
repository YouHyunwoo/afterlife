using System;
using System.Collections.Generic;
using DataStructure.Heap;
using UnityEngine;

namespace Afterlife.Model
{
    public class Node
    {
        public Vector2Int TilePosition;
        public float F; // f -> g + h, h -> heuristic distance
        public float G; // g -> step
    }

    public static class HeuristicCalculator
    {
        public static float ManhattanDistance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        public static float EuclideanDistance(Vector2Int a, Vector2Int b) => Vector2Int.Distance(a, b);
    }

    public class PathFinder
    {
        readonly Func<Vector2Int, int> costGetter;
        readonly Func<Vector2Int, Vector2Int, float> heuristicCalculator;

        Vector2Int[] neighbors;

        public PathFinder(Func<Vector2Int, int> costGetter, Func<Vector2Int, Vector2Int, float> heuristicCalculator = null)
        {
            this.costGetter = costGetter;
            this.heuristicCalculator = heuristicCalculator ?? HeuristicCalculator.ManhattanDistance;

            neighbors = new Vector2Int[4];
        }

        public bool FindPath(Vector2Int start, Vector2Int goal, out Vector2Int[] path)
        {
            path = FindPath(start, goal);
            return path[^1] == goal;
        }

        public Vector2Int[] FindPath(Vector2Int start, Vector2Int goal)
        {
            Dictionary<Vector2Int, float> open = new();
            HashSet<Vector2Int> closed = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();

            PriorityQueue<Node> queue = new();
            Node startNode = new()
            {
                TilePosition = start,
                F = heuristicCalculator.Invoke(start, goal),
                G = 0f,
            };
            queue.Enqueue(startNode, 0f);

            Node closestNode = startNode;

            while (queue.Count > 0)
            {
                Node node = queue.Dequeue();

                if (node.F - node.G < closestNode.F - closestNode.G)
                {
                    closestNode = node;
                }

                if (node.TilePosition == goal)
                {
                    return GeneratePath(cameFrom, goal);
                }

                closed.Add(node.TilePosition);

                GetNeighbors(node.TilePosition);
                foreach (var neighbor in neighbors)
                {
                    var cost = costGetter(neighbor);
                    if (cost < 0) { continue; }
                    if (closed.Contains(neighbor)) { continue; }

                    float neighborG = node.G + 1 + cost;
                    float neighborH = heuristicCalculator.Invoke(neighbor, goal);
                    float neighborF = neighborG + neighborH;

                    if (open.ContainsKey(neighbor) && open[neighbor] <= neighborF) { continue; }

                    open[neighbor] = neighborF;
                    cameFrom[neighbor] = node.TilePosition;

                    Node neighborNode = new()
                    {
                        TilePosition = neighbor,
                        F = neighborF,
                        G = neighborG,
                    };

                    queue.Enqueue(neighborNode, neighborF);
                }
            }

            var path = GeneratePath(cameFrom, closestNode.TilePosition);

            return path;
        }

        Vector2Int[] GeneratePath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int goal)
        {
            List<Vector2Int> path = new();
            Vector2Int currentLocation = goal;
            while (cameFrom.ContainsKey(currentLocation))
            {
                path.Insert(0, currentLocation);
                currentLocation = cameFrom[currentLocation];
            }
            path.Insert(0, currentLocation);
            return path.ToArray();
        }

        void GetNeighbors(Vector2Int tilePosition)
        {
            neighbors[0] = tilePosition + Vector2Int.up;
            neighbors[1] = tilePosition + Vector2Int.down;
            neighbors[2] = tilePosition + Vector2Int.left;
            neighbors[3] = tilePosition + Vector2Int.right;
        }

        public void PrintPath(Vector2Int[] path) => Debug.Log(string.Join(" -> ", path));
    }
}