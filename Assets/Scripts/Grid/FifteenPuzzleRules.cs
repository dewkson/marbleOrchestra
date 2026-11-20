using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// What a single cell means to the 15-Puzzle slide rules (see 0066).
    /// </summary>
    public enum PuzzleCell
    {
        Blocked,
        Free,
        Card,
        LockedCard
    }

    /// <summary>
    /// The slide rule of GameMode.FifteenPuzzle (see 0066), kept free of
    /// any runtime/editor representation so PathGrid (live PathPipes) and
    /// the Level Grid Editor's slide-based Randomize (raw LevelData lists)
    /// share one definition: a card slides one cell in a direction if the
    /// first non-card cell that way is free - every card in between moves
    /// along with it, like pushing a row of the classic 15-Puzzle. A
    /// locked card or a blocked cell in the way stops the whole slide.
    /// </summary>
    public static class FifteenPuzzleRules
    {
        /// Fills `line` with the cards that move (starting at `from`, in
        /// walking order towards the free cell) and returns the free cell
        /// they move into. False if `from` isn't a movable card or nothing
        /// that way can move.
        public static bool TryGetSlideLine(Vector2Int from, Direction direction, RectInt bounds,
            Func<Vector2Int, PuzzleCell> cellAt, List<Vector2Int> line, out Vector2Int freeCell)
        {
            line.Clear();
            freeCell = default;
            if (!bounds.Contains(from) || cellAt(from) != PuzzleCell.Card) return false;

            Vector2Int offset = direction.ToGridOffset();
            Vector2Int coord = from;

            while (bounds.Contains(coord))
            {
                PuzzleCell cell = cellAt(coord);
                if (cell == PuzzleCell.Free)
                {
                    freeCell = coord;
                    return line.Count > 0;
                }
                if (cell != PuzzleCell.Card) break;

                line.Add(coord);
                coord += offset;
            }

            line.Clear();
            return false;
        }

        /// The one and only direction `from` can slide in - what a plain
        /// click does. False if it can't move at all OR if several
        /// directions are open: a click carries no direction, so it must
        /// never guess (a drag picks one explicitly instead).
        public static bool TryGetOnlySlide(Vector2Int from, RectInt bounds,
            Func<Vector2Int, PuzzleCell> cellAt, List<Vector2Int> line, out Direction direction)
        {
            direction = Direction.None;
            line.Clear();
            List<Vector2Int> candidate = new List<Vector2Int>();

            foreach (Direction dir in DirectionExtensions.All)
            {
                if (!TryGetSlideLine(from, dir, bounds, cellAt, candidate, out _)) continue;

                if (direction != Direction.None)
                {
                    direction = Direction.None;
                    line.Clear();
                    return false;
                }

                direction = dir;
                line.AddRange(candidate);
            }

            return direction != Direction.None;
        }

        /// True if `from` can slide in at least one direction - whether
        /// pressing it can lead anywhere (by click or drag).
        public static bool CanSlideAny(Vector2Int from, RectInt bounds, Func<Vector2Int, PuzzleCell> cellAt)
        {
            List<Vector2Int> candidate = new List<Vector2Int>();
            foreach (Direction dir in DirectionExtensions.All)
            {
                if (TryGetSlideLine(from, dir, bounds, cellAt, candidate, out _)) return true;
            }
            return false;
        }
    }
}
