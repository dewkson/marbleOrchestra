using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    public enum CellConnectivity
    {
        Disconnected,
        Connected,
        PathComplete
    }

    public readonly struct PathValidationResult
    {
        public readonly HashSet<Vector2Int> ConnectedCells;
        public readonly bool GoalReached;
        public readonly IReadOnlyList<Vector2Int> OrderedPath;

        public PathValidationResult(HashSet<Vector2Int> connectedCells, bool goalReached, IReadOnlyList<Vector2Int> orderedPath)
        {
            ConnectedCells = connectedCells;
            GoalReached = goalReached;
            OrderedPath = orderedPath;
        }
    }

    /// <summary>
    /// Pure logic: for every Start pipe, walks reciprocal pipe connections
    /// outward and checks whether any Goal pipe is reachable - one result
    /// per Start pipe, so a level can have several independent tracks.
    /// No rendering here.
    /// </summary>
    public static class PathValidator
    {
        public static IReadOnlyList<PathValidationResult> EvaluateAll(PathGrid grid)
        {
            List<PathValidationResult> results = new List<PathValidationResult>();
            foreach (PathPipe start in grid.FindPipesByRole(PipeRole.Start))
            {
                results.Add(EvaluateFrom(grid, start));
            }
            return results;
        }

        private static PathValidationResult EvaluateFrom(PathGrid grid, PathPipe start)
        {
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            List<Vector2Int> orderedPath = new List<Vector2Int>();

            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(start.Coord);
            visited.Add(start.Coord);

            while (frontier.Count > 0)
            {
                Vector2Int coord = frontier.Dequeue();
                PathPipe pipe = grid.GetPipe(coord);
                if (pipe == null || pipe.Definition == null) continue;

                Direction connections = pipe.Definition.Connections;
                foreach (Direction dir in DirectionExtensions.All)
                {
                    if ((connections & dir) == 0) continue;

                    Vector2Int neighborCoord = coord + dir.ToGridOffset();
                    if (!grid.IsInBounds(neighborCoord) || visited.Contains(neighborCoord)) continue;

                    PathPipe neighbor = grid.GetPipe(neighborCoord);
                    if (neighbor == null || neighbor.Definition == null) continue;

                    Direction neighborConnections = neighbor.Definition.Connections;
                    if ((neighborConnections & dir.Opposite()) == 0) continue;

                    visited.Add(neighborCoord);
                    cameFrom[neighborCoord] = coord;
                    frontier.Enqueue(neighborCoord);
                }
            }

            PathPipe goal = FindReachedGoal(grid, visited);
            bool goalReached = goal != null;

            if (goalReached)
            {
                Vector2Int step = goal.Coord;
                orderedPath.Add(step);
                while (cameFrom.TryGetValue(step, out Vector2Int previous))
                {
                    orderedPath.Add(previous);
                    step = previous;
                }
                orderedPath.Reverse();
            }

            return new PathValidationResult(visited, goalReached, orderedPath);
        }

        private static PathPipe FindReachedGoal(PathGrid grid, HashSet<Vector2Int> visited)
        {
            foreach (PathPipe goal in grid.FindPipesByRole(PipeRole.Goal))
            {
                if (visited.Contains(goal.Coord)) return goal;
            }
            return null;
        }
    }
}
