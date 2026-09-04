namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Default IBlockMotionTrace: no offset at all, i.e. the plain linear
    /// entry-to-exit height lerp TrackBlockSpawner already computed stands
    /// unchanged - today's existing Kinematic3D behavior.
    /// </summary>
    public class LinearMotionTrace : IBlockMotionTrace
    {
        public static readonly LinearMotionTrace Instance = new LinearMotionTrace();

        public float SampleOffset(float f) => 0f;
    }
}
