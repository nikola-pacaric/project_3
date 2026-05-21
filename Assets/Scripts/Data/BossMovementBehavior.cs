namespace Warblade.Data
{
    public enum BossMovementBehavior
    {
        [UnityEngine.InspectorName("Legacy: Hold Position")]
        HoldPosition,

        HorizontalPatrol,

        [UnityEngine.InspectorName("Legacy: Sine Drift")]
        SineDrift,

        FigureEight,
        DashAndPause,
        DiveSweep,
        LaneSwitch,
        PlayerShadow,
        BoxPatrol
    }

    public enum BossPhaseTransitionTarget
    {
        ArenaCenter,
        ClosestPointOnNextMovement
    }
}
