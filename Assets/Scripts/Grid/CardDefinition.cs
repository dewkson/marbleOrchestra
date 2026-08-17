using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Gameplay data for a card type. Deliberately holds no visual/prefab
    /// reference so the same definition can drive different renderers later.
    /// </summary>
    [CreateAssetMenu(fileName = "Card_", menuName = "MarbleOrchestra/Card Definition")]
    public class CardDefinition : ScriptableObject
    {
        [SerializeField] private string cardId = "Straight";
        [SerializeField] private Direction connections = Direction.Left | Direction.Right;
        [SerializeField] private Color color = Color.white;

        public string CardId => cardId;
        public Direction Connections => connections;
        public Color Color => color;
    }
}
