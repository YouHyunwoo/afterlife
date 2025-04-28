using System;
using System.Collections.Generic;
using UnityEngine;
using DataStructure.Heap;

namespace Algorithm.PathFinding.AStar
{
    public class Node
    {
        public Vector2Int Location;
        public float F; // f -> g + h, h -> heuristic distance
        public float G; // g -> step
    }

    public class HeuristicCalculator
    {
        public static float ManhattanDistance(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        public static float EuclideanDistance(Vector2Int a, Vector2Int b) => Vector2Int.Distance(a, b);
    }

    public class PathFinder
    {
        readonly Func<Vector2Int, Vector2Int, float> heuristicCalculator;

        public PathFinder(Func<Vector2Int, Vector2Int, float> heuristicCalculator = null)
        {
            this.heuristicCalculator = heuristicCalculator ?? HeuristicCalculator.ManhattanDistance;
        }

        public bool FindPath(Vector2Int start, Vector2Int goal, IGrid grid, out Vector2Int[] path)
        {
            path = FindPath(start, goal, grid);
            return path[^1] == goal;
        }

        public Vector2Int[] FindPath(Vector2Int start, Vector2Int goal, IGrid grid)
        {
            Dictionary<Vector2Int, float> open = new();
            HashSet<Vector2Int> closed = new();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new();

            PriorityQueue<Node> queue = new();
            Node startNode = new()
            {
                Location = start,
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

                if (node.Location == goal)
                {
                    return GeneratePath(cameFrom, goal);
                }

                closed.Add(node.Location);

                Vector2Int[] neighbors = GetNeighbors(node.Location);
                foreach (var neighbor in neighbors)
                {
                    if (!grid.IsPassable(neighbor)) { continue; }
                    if (closed.Contains(neighbor)) { continue; }

                    float neighborG = node.G + 1;
                    float neighborH = heuristicCalculator.Invoke(neighbor, goal);
                    float neighborF = neighborG + neighborH;

                    if (open.ContainsKey(neighbor) && open[neighbor] <= neighborF) { continue; }

                    open[neighbor] = neighborF;
                    cameFrom[neighbor] = node.Location;

                    Node neighborNode = new()
                    {
                        Location = neighbor,
                        F = neighborF,
                        G = neighborG,
                    };

                    queue.Enqueue(neighborNode, neighborF);
                }
            }

            var path = GeneratePath(cameFrom, closestNode.Location);

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

        Vector2Int[] GetNeighbors(Vector2Int location)
        {
            return new Vector2Int[4]
            {
                location + Vector2Int.up,
                location + Vector2Int.down,
                location + Vector2Int.left,
                location + Vector2Int.right,
            };
        }

        public void PrintPath(Vector2Int[] path)
        {
            Debug.Log(string.Join(" -> ", path));
        }
    }
}