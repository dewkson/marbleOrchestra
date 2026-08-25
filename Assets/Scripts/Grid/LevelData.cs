using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Describes a level's grid size and its two independent layers:
    /// pipes (swappable) and contents (fixed to the cell, e.g. sound triggers).
    /// Both are stored row-major (index = y * width + x), y = 0 at the bottom row.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "MarbleOrchestra/Level Data")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private int width = 4;
        [SerializeField] private int height = 3;
        [FormerlySerializedAs("cards")]
        [SerializeField] private List<PipeDefinition> pipes = new List<PipeDefinition>();
        [SerializeField] private List<CellContentDefinition> contents = new List<CellContentDefinition>();

        public int Width => width;
        public int Height => height;
        public IReadOnlyList<PipeDefinition> Pipes => pipes;
        public IReadOnlyList<CellContentDefinition> Contents => contents;

        public void SetPipeAt(int index, PipeDefinition pipe)
        {
            if (index < 0 || index >= pipes.Count) return;
            pipes[index] = pipe;
        }

        public void SetContentAt(int index, CellContentDefinition content)
        {
            if (index < 0 || index >= contents.Count) return;
            contents[index] = content;
        }

        public void EnsureListSizes()
        {
            int required = width * height;
            ResizeList(pipes, required);
            ResizeList(contents, required);
        }

        public void ResizeGrid(int newWidth, int newHeight)
        {
            newWidth = Mathf.Max(1, newWidth);
            newHeight = Mathf.Max(1, newHeight);

            List<PipeDefinition> newPipes = RemapGrid(pipes, width, height, newWidth, newHeight);
            List<CellContentDefinition> newContents = RemapGrid(contents, width, height, newWidth, newHeight);

            width = newWidth;
            height = newHeight;
            pipes = newPipes;
            contents = newContents;
        }

        private static List<T> RemapGrid<T>(List<T> source, int oldWidth, int oldHeight, int newWidth, int newHeight)
        {
            List<T> result = new List<T>(new T[newWidth * newHeight]);

            int copyWidth = Mathf.Min(oldWidth, newWidth);
            int copyHeight = Mathf.Min(oldHeight, newHeight);

            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    int oldIndex = y * oldWidth + x;
                    int newIndex = y * newWidth + x;
                    if (oldIndex < source.Count)
                    {
                        result[newIndex] = source[oldIndex];
                    }
                }
            }

            return result;
        }

        private static void ResizeList<T>(List<T> list, int required)
        {
            if (list.Count > required)
            {
                list.RemoveRange(required, list.Count - required);
            }
            else
            {
                while (list.Count < required)
                {
                    list.Add(default);
                }
            }
        }

        private void OnValidate()
        {
            int required = width * height;
            if (pipes.Count != required)
            {
                Debug.LogWarning($"{name}: expected {required} pipes for a {width}x{height} grid, but has {pipes.Count}.", this);
            }

            if (contents.Count != required)
            {
                Debug.LogWarning($"{name}: expected {required} content slots for a {width}x{height} grid, but has {contents.Count}.", this);
            }

            int startCount = 0;
            int goalCount = 0;
            foreach (PipeDefinition pipe in pipes)
            {
                if (pipe == null) continue;
                if (pipe.Role == PipeRole.Start) startCount++;
                if (pipe.Role == PipeRole.Goal) goalCount++;
            }

            if (startCount != 1)
            {
                Debug.LogWarning($"{name}: expected exactly 1 Start pipe, found {startCount}.", this);
            }

            if (goalCount != 1)
            {
                Debug.LogWarning($"{name}: expected exactly 1 Goal pipe, found {goalCount}.", this);
            }
        }
    }
}
