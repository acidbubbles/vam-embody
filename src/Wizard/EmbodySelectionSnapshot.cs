public class EmbodySelectionSnapshot
{
    public EmbodyContext context;
    public bool passenger;
    public bool snug;
    public bool trackers;
    public bool eyeTarget;
    public bool hideGeometry;
    public bool offsetCamera;
    public bool worldScale;

    public static EmbodySelectionSnapshot Snap(EmbodyContext context)
    {
        return new EmbodySelectionSnapshot()
        {
            context = context,
            passenger = context.passenger.selectedJSON.val,
            snug = context.snug.selectedJSON.val,
            trackers = context.trackers.selectedJSON.val,
            eyeTarget = context.eyeTarget.selectedJSON.val,
            hideGeometry = context.hideGeometry.selectedJSON.val,
            offsetCamera = context.offsetCamera.selectedJSON.val,
            worldScale = context.worldScale.selectedJSON.val
        };
    }

    public void DisableAll()
    {
        context.passenger.selectedJSON.val = false;
        context.snug.selectedJSON.val = false;
        context.trackers.selectedJSON.val = false;
        context.eyeTarget.selectedJSON.val = false;
        context.hideGeometry.selectedJSON.val = false;
        context.offsetCamera.selectedJSON.val = false;
        context.worldScale.selectedJSON.val = false;
    }

    public void Restore()
    {
        context.passenger.selectedJSON.val = passenger;
        context.snug.selectedJSON.val = snug;
        context.trackers.selectedJSON.val = trackers;
        context.eyeTarget.selectedJSON.val = eyeTarget;
        context.hideGeometry.selectedJSON.val = hideGeometry;
        context.offsetCamera.selectedJSON.val = offsetCamera;
        context.worldScale.selectedJSON.val = worldScale;
    }
}
