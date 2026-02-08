namespace Gameplay.Items
{
    public enum ContainerStepState : byte
    {
        None = 0,
        Empty,
        InProgress,
        Completed,
        FinalizedProduct,
        Invalid
    }
}
