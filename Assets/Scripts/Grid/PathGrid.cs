using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Parametric grid of PathPipes built from a LevelData asset.
    /// Owns the swap logic so it works independent of any input method.
    /// Also owns the content layer (e.g. sound triggers), which is bound to
    /// the cell coordinate and stays fixed regardless of pipe swaps.
    /// </summary>
    public class PathGrid : MonoBehaviour
    {
        [SerializeField] private LevelData level;
        [SerializeField] private float cellSize = 1.2f;
        [SerializeField] private float cardScale = 1f;

        private PathPipe[,] pipes;
        private CellContentDefinition[,] contents;

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
            pipes = new PathPipe[Width, Height];
            contents = new CellContentDefinition[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    PipeDefinition definition = index < levelData.Pipes.Count ? levelData.Pipes[index] : null;
                    pipes[x, y] = CreatePipe(new Vector2Int(x, y), definition);
                    contents[x, y] = index < levelData.Contents.Count ? levelData.Contents[index] : null;
                }
            }

            Revalidate();
        }

        public PathPipe GetPipe(Vector2Int coord)
        {
            return IsInBounds(coord) ? pipes[coord.x, coord.y] : null;
        }

        public CellContentDefinition GetContent(Vector2Int coord)
        {
            return IsInBounds(coord) ? contents[coord.x, coord.y] : null;
        }

        public bool IsInBounds(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < Width && coord.y >= 0 && coord.y < Height;
        }

        public PathPipe FindPipeByRole(PipeRole role)
        {
            if (pipes == null) return null;

            foreach (PathPipe pipe in pipes)
            {
                if (pipe != null && pipe.Role == role) return pipe;
            }

            return null;
        }

        public void SwapCards(Vector2Int a, Vector2Int b)
        {
            // Content layer is intentionally untouched here — it is bound to
            // the cell coordinate, not to the pipe occupying it.
            if (!IsInBounds(a) || !IsInBounds(b) || a == b) return;

            PathPipe pipeA = pipes[a.x, a.y];
            PathPipe pipeB = pipes[b.x, b.y];

            pipes[a.x, a.y] = pipeB;
            pipes[b.x, b.y] = pipeA;

            if (pipeA != null)
            {
                pipeA.SetCoord(b);
                pipeA.transform.localPosition = CellToLocalPosition(b);
            }

            if (pipeB != null)
            {
                pipeB.SetCoord(a);
                pipeB.transform.localPosition = CellToLocalPosition(a);
            }

            Revalidate();
        }

        public PathValidationResult Revalidate()
        {
            PathValidationResult result = PathValidator.Evaluate(this);
            LastValidation = result;

            foreach (PathPipe pipe in pipes)
            {
                if (pipe == null) continue;

                CellConnectivity connectivity = CellConnectivity.Disconnected;
                if (result.ConnectedCells.Contains(pipe.Coord))
                {
                    connectivity = result.GoalReached ? CellConnectivity.PathComplete : CellConnectivity.Connected;
                }

                pipe.SetConnectivity(connectivity);
            }

            return result;
        }

        public Vector3 CellToLocalPosition(Vector2Int coord)
        {
            return new Vector3(coord.x * cellSize, coord.y * cellSize, 0f);
        }

        private PathPipe CreatePipe(Vector2Int coord, PipeDefinition definition)
        {
            GameObject go = new GameObject($"Pipe_{coord.x}_{coord.y}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = CellToLocalPosition(coord);
            go.transform.localScale = Vector3.one * cardScale;
            go.AddComponent<BoxCollider2D>();

            PathPipe pipe = go.AddComponent<PathPipe>();
            pipe.Initialize(definition, coord);
            return pipe;
        }
    }
}
