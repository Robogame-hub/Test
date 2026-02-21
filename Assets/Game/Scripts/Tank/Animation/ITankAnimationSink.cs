namespace TankGame.Tank.Animation
{
    /// <summary>
    /// Animation sink contract to apply a prepared motion state.
    /// </summary>
    public interface ITankAnimationSink
    {
        void Apply(in TankMotionState state);
    }
}
