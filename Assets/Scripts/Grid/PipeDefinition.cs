using UnityEngine;
using UnityEngine.Serialization;

namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Gameplay data for a pipe type. Deliberately holds no visual/prefab
    /// reference so the same definition can drive different renderers later.
    /// </summary>
    public enum PipeRole
    {
        Normal,
        Start,
        Goal
    }

    [CreateAssetMenu(fileName = "Pipe_", menuName = "MarbleOrchestra/Pipe Definition")]
    public class PipeDefinition : ScriptableObject
    {
        [FormerlySerializedAs("cardId")]
        [SerializeField] private string pipeId = "Straight";
        [SerializeField] private Direction connections = Direction.Left | Direction.Right;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private PipeRole role = PipeRole.Normal;
        [SerializeField] private bool locked;
        [Tooltip("Optional 2D picture shown on the card in the planning view (see 0030). It travels with the pipe when cards are swapped. Null = plain colored card.")]
        [SerializeField] private Sprite cardImage;

        public string PipeId => pipeId;
        public Sprite CardImage => cardImage;
        public Direction Connections => connections;
        public Color Color => color;
        public Color BackgroundColor => backgroundColor;
        public PipeRole Role => role;
        public bool Locked => locked;
    }
}
