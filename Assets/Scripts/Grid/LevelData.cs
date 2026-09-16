using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Names and bounds one SubLevel (see 0046): a rectangular area of the
    /// parent LevelData's big grid, e.g. "Xylophon" or "Percussion". Order
    /// in LevelData.SubLevels IS the progression order - there's no
    /// separate index field. A plain serializable class rather than its
    /// own ScriptableObject, since a SubLevel has no identity outside the
    /// LevelData it's carved out of.
    /// </summary>
    [System.Serializable]
    public class SubLevelDefinition
    {
        [SerializeField] private string subLevelName = "SubLevel";
        [SerializeField] private RectInt area = new RectInt(0, 0, 1, 1);

        public string Name => subLevelName;
        public RectInt Area => area;

        public SubLevelDefinition(string subLevelName, RectInt area)
        {
            this.subLevelName = subLevelName;
            this.area = area;
        }

        public void SetName(string newName) => subLevelName = newName;
        public void SetArea(RectInt newArea) => area = newArea;
    }

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
        [Tooltip("Loop length in steps (one beat each). Only the blocks between Start and Goal count - Start/Goal are silent and overlap the neighbouring laps, so a 16-step loop is an 18-block track. Every track's lap is rounded up to whole loops, so all tracks restart on a shared downbeat. 0 = each track loops with its own step count.")]
        [SerializeField] private int loopLengthSteps = 16;
        [FormerlySerializedAs("cards")]
        [SerializeField] private List<PipeDefinition> pipes = new List<PipeDefinition>();
        [SerializeField] private List<CellContentDefinition> contents = new List<CellContentDefinition>();
        [Tooltip("Cells marked here can't hold a pipe or content and are impassable - lets a level use only part of a larger rectangular grid.")]
        [SerializeField] private List<bool> blocked = new List<bool>();
        [Tooltip("SubLevels (see 0046) carve this grid into named, ordered puzzle areas the player progresses through one at a time. Empty = the whole grid is a single implicit SubLevel.")]
        [SerializeField] private List<SubLevelDefinition> subLevels = new List<SubLevelDefinition>();

        public int Width => width;
        public int Height => height;
        public int LoopLengthSteps => Mathf.Max(0, loopLengthSteps);
        public IReadOnlyList<PipeDefinition> Pipes => pipes;
        public IReadOnlyList<CellContentDefinition> Contents => contents;
        public IReadOnlyList<bool> Blocked => blocked;
        public IReadOnlyList<SubLevelDefinition> SubLevels => subLevels;

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

        public bool IsBlockedAt(int index)
        {
            return index >= 0 && index < blocked.Count && blocked[index];
        }

        /// Blocking a cell also clears any pipe/content it held - a
        /// non-buildable cell can't carry either layer.
        public void SetBlockedAt(int index, bool isBlocked)
        {
            if (index < 0 || index >= blocked.Count) return;
            blocked[index] = isBlocked;
            if (isBlocked)
            {
                SetPipeAt(index, null);
                SetContentAt(index, null);
            }
        }

        public void AddSubLevel(string subLevelName, RectInt area)
        {
            subLevels.Add(new SubLevelDefinition(subLevelName, area));
        }

        public void RemoveSubLevelAt(int index)
        {
            if (index < 0 || index >= subLevels.Count) return;
            subLevels.RemoveAt(index);
        }

        public void SetSubLevelName(int index, string subLevelName)
        {
            if (index < 0 || index >= subLevels.Count) return;
            subLevels[index].SetName(subLevelName);
        }

        public void SetSubLevelArea(int index, RectInt area)
        {
            if (index < 0 || index >= subLevels.Count) return;
            subLevels[index].SetArea(area);
        }

        /// Reorders SubLevels - their list position IS their progression
        /// order, so this is the only way to change it.
        public void MoveSubLevel(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= subLevels.Count) return;
            if (toIndex < 0 || toIndex >= subLevels.Count) return;
            if (fromIndex == toIndex) return;

            SubLevelDefinition item = subLevels[fromIndex];
            subLevels.RemoveAt(fromIndex);
            subLevels.Insert(toIndex, item);
        }

        public void EnsureListSizes()
        {
            int required = width * height;
            ResizeList(pipes, required);
            ResizeList(contents, required);
            ResizeList(blocked, required);
        }

        public void ResizeGrid(int newWidth, int newHeight)
        {
            newWidth = Mathf.Max(1, newWidth);
            newHeight = Mathf.Max(1, newHeight);

            List<PipeDefinition> newPipes = RemapGrid(pipes, width, height, newWidth, newHeight);
            List<CellContentDefinition> newContents = RemapGrid(contents, width, height, newWidth, newHeight);
            List<bool> newBlocked = RemapGrid(blocked, width, height, newWidth, newHeight);

            width = newWidth;
            height = newHeight;
            pipes = newPipes;
            contents = newContents;
            blocked = newBlocked;
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

            if (blocked.Count != required)
            {
                Debug.LogWarning($"{name}: expected {required} blocked flags for a {width}x{height} grid, but has {blocked.Count}.", this);
            }

            int startCount = 0;
            int goalCount = 0;
            for (int i = 0; i < pipes.Count; i++)
            {
                PipeDefinition pipe = pipes[i];
                if (pipe == null) continue;
                if (pipe.Role == PipeRole.Start) startCount++;
                if (pipe.Role == PipeRole.Goal) goalCount++;

                bool isStartOrGoal = pipe.Role == PipeRole.Start || pipe.Role == PipeRole.Goal;
                if (isStartOrGoal && i < contents.Count && contents[i] is ITriggerCellContent)
                {
                    Debug.LogWarning($"{name}: cell {i % width},{i / width} holds a {pipe.Role} pipe and trigger content - Start/Goal are always silent, the content is ignored there.", this);
                }

                if (i < blocked.Count && blocked[i])
                {
                    Debug.LogWarning($"{name}: cell {i % width},{i / width} is marked blocked but still holds a pipe.", this);
                }
            }

            if (startCount < 1)
            {
                Debug.LogWarning($"{name}: expected at least 1 Start pipe, found {startCount}.", this);
            }

            if (startCount != goalCount)
            {
                Debug.LogWarning($"{name}: expected the same number of Start and Goal pipes, found {startCount} Start and {goalCount} Goal.", this);
            }

            for (int i = 0; i < subLevels.Count; i++)
            {
                RectInt area = subLevels[i].Area;
                if (area.width <= 0 || area.height <= 0 || area.xMin < 0 || area.yMin < 0 || area.xMax > width || area.yMax > height)
                {
                    Debug.LogWarning($"{name}: SubLevel '{subLevels[i].Name}' has an area outside the {width}x{height} grid.", this);
                }
            }
        }
    }
}
