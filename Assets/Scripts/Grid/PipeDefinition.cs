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

        public string PipeId => pipeId;
        public Direction Connections => connections;
        public Color Color => color;
        public Color BackgroundColor => backgroundColor;
        public PipeRole Role => role;
        public bool Locked => locked;
    }
}
