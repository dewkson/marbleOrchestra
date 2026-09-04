namespace MarbleOrchestra.Grid
{
    /// <summary>
    /// Example IBlockMotionTrace that arcs the marble up and back down
    /// across the block via a parabola - offset = 4 * apexHeight * f *
    /// (1-f), zero at both ends (per the interface contract) and peaking at
    /// f = 0.5 - simulating a jump/hop through this one block without any
    /// physics engine.
    /// </summary>
    public class JumpMotionTrace : IBlockMotionTrace
    {
        private readonly float apexHeight;

        public JumpMotionTrace(float apexHeight)
        {
            this.apexHeight = apexHeight;
        }

        public float SampleOffset(float f) => 4f * apexHeight * f * (1f - f);
    }
}
