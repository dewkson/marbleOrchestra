using UnityEngine;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Gameplay data for a card type. Deliberately holds no visual/prefab
    /// reference so the same definition can drive different renderers later.
    /// </summary>
    public enum CardRole
    {
        Normal,
        Start,
        Goal
    }

    [CreateAssetMenu(fileName = "Card_", menuName = "MarbleOrchestra/Card Definition")]
    public class CardDefinition : ScriptableObject
    {
        [SerializeField] private string cardId = "Straight";
        [SerializeField] private Direction connections = Direction.Left | Direction.Right;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private CardRole role = CardRole.Normal;
        [SerializeField] private bool locked;

        public string CardId => cardId;
        public Direction Connections => connections;
        public Color Color => color;
        public Color BackgroundColor => backgroundColor;
        public CardRole Role => role;
        public bool Locked => locked;
    }
}
