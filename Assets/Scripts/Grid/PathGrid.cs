using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Parametric grid of PathCards built from a LevelData asset.
    /// Owns the swap logic so it works independent of any input method.
    /// </summary>
    public class PathGrid : MonoBehaviour
    {
        [SerializeField] private LevelData level;
        [SerializeField] private float cellSize = 1.2f;
        [SerializeField] private float cardScale = 1f;

        private PathCard[,] cells;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public PathValidationResult LastValidation { get; private set; }

        private void Awake()
        {
            if (level != null)
            {
                Build(level);
            }
        }

        public void Build(LevelData levelData)
        {
            level = levelData;
            Width = levelData.Width;
            Height = levelData.Height;
            cells = new PathCard[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    CardDefinition definition = index < levelData.Cards.Count ? levelData.Cards[index] : null;
                    cells[x, y] = CreateCard(new Vector2Int(x, y), definition);
                }
            }

            Revalidate();
        }

        public PathCard GetCard(Vector2Int coord)
        {
            return IsInBounds(coord) ? cells[coord.x, coord.y] : null;
        }

        public bool IsInBounds(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < Width && coord.y >= 0 && coord.y < Height;
        }

        public PathCard FindCardByRole(CardRole role)
        {
            if (cells == null) return null;

            foreach (PathCard card in cells)
            {
                if (card != null && card.Role == role) return card;
            }

            return null;
        }

        public void SwapCards(Vector2Int a, Vector2Int b)
        {
            if (!IsInBounds(a) || !IsInBounds(b) || a == b) return;

            PathCard cardA = cells[a.x, a.y];
            PathCard cardB = cells[b.x, b.y];

            cells[a.x, a.y] = cardB;
            cells[b.x, b.y] = cardA;

            if (cardA != null)
            {
                cardA.SetCoord(b);
                cardA.transform.localPosition = CellToLocalPosition(b);
            }

            if (cardB != null)
            {
                cardB.SetCoord(a);
                cardB.transform.localPosition = CellToLocalPosition(a);
            }

            Revalidate();
        }

        public PathValidationResult Revalidate()
        {
            PathValidationResult result = PathValidator.Evaluate(this);
            LastValidation = result;

            foreach (PathCard card in cells)
            {
                if (card == null) continue;

                CellConnectivity connectivity = CellConnectivity.Disconnected;
                if (result.ConnectedCells.Contains(card.Coord))
                {
                    connectivity = result.GoalReached ? CellConnectivity.PathComplete : CellConnectivity.Connected;
                }

                card.SetConnectivity(connectivity);
            }

            return result;
        }

        public Vector3 CellToLocalPosition(Vector2Int coord)
        {
            return new Vector3(coord.x * cellSize, coord.y * cellSize, 0f);
        }

        private PathCard CreateCard(Vector2Int coord, CardDefinition definition)
        {
            GameObject go = new GameObject($"Card_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = CellToLocalPosition(coord);
            go.transform.localScale = Vector3.one * cardScale;
            go.AddComponent<BoxCollider2D>();

            PathCard card = go.AddComponent<PathCard>();
            card.Initialize(definition, coord);
            return card;
        }
    }
}
