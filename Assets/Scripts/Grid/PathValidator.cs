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

        public PathValidationResult(HashSet<Vector2Int> connectedCells, bool goalReached)
        {
            ConnectedCells = connectedCells;
            GoalReached = goalReached;
        }
    }

    /// <summary>
    /// Pure logic: walks reciprocal card connections outward from the Start
    /// card and checks whether the Goal card is reachable. No rendering here.
    /// </summary>
    public static class PathValidator
    {
        public static PathValidationResult Evaluate(PathGrid grid)
        {
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            PathCard start = grid.FindCardByRole(CardRole.Start);
            if (start == null)
            {
                return new PathValidationResult(visited, false);
            }

            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            frontier.Enqueue(start.Coord);
            visited.Add(start.Coord);

            while (frontier.Count > 0)
            {
                Vector2Int coord = frontier.Dequeue();
                PathCard card = grid.GetCard(coord);
                if (card == null || card.Definition == null) continue;

                Direction connections = card.Definition.Connections;
                foreach (Direction dir in DirectionExtensions.All)
                {
                    if ((connections & dir) == 0) continue;

                    Vector2Int neighborCoord = coord + dir.ToGridOffset();
                    if (!grid.IsInBounds(neighborCoord) || visited.Contains(neighborCoord)) continue;

                    PathCard neighbor = grid.GetCard(neighborCoord);
                    if (neighbor == null || neighbor.Definition == null) continue;

                    Direction neighborConnections = neighbor.Definition.Connections;
                    if ((neighborConnections & dir.Opposite()) == 0) continue;

                    visited.Add(neighborCoord);
                    frontier.Enqueue(neighborCoord);
                }
            }

            PathCard goal = grid.FindCardByRole(CardRole.Goal);
            bool goalReached = goal != null && visited.Contains(goal.Coord);

            return new PathValidationResult(visited, goalReached);
        }
    }
}
