namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// How the player rearranges a level's cards in the 2D planning view
    /// (see 0063/0064) - set once per LevelData in the Level Grid Editor.
    /// CardSwap: any two unlocked cards trade places (see 0004).
    /// FifteenPuzzle: cells without a card are free slots; a card in the
    /// same row/column as a free slot slides towards it, pushing every
    /// card in between along (see 0066).
    /// </summary>
    public enum GameMode
    {
        CardSwap,
        FifteenPuzzle
    }
}
