using System.Collections.Generic;
using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Describes a card set and grid size, not concrete GameObjects.
    /// Cards are stored row-major (index = y * width + x), y = 0 at the bottom row.
    /// </summary>
    [CreateAssetMenu(fileName = "Level_", menuName = "MarbleOrchestra/Level Data")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private int width = 4;
        [SerializeField] private int height = 3;
        [SerializeField] private List<CardDefinition> cards = new List<CardDefinition>();

        public int Width => width;
        public int Height => height;
        public IReadOnlyList<CardDefinition> Cards => cards;

        private void OnValidate()
        {
            int required = width * height;
            if (cards.Count != required)
            {
                Debug.LogWarning($"{name}: expected {required} cards for a {width}x{height} grid, but has {cards.Count}.", this);
            }
        }
    }
}
