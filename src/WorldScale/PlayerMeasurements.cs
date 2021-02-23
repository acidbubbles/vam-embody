using System.Linq;

public class PlayerMeasurements
{
    private readonly EmbodyContext _context;

    public PlayerMeasurements(EmbodyContext context)
    {
        _context = context;
    }

    public float MeasureHeight()
    {
        var headMotionControl = _context.trackers.motionControls.First(mc => mc.name == MotionControlNames.Head);
        var lowestMotionControl = _context.trackers.motionControls
            .Where(mc => mc.name == MotionControlNames.LeftHand || mc.name == MotionControlNames.RightHand)
            .Where(t => t.enabled && t.SyncMotionControl())
            .Min(vt => vt.currentMotionControl.position.y);

        if (lowestMotionControl < headMotionControl.currentMotionControl.position.y / 10f)
            return headMotionControl.currentMotionControl.position.y - lowestMotionControl;

        return SuperController.singleton.heightAdjustTransform.InverseTransformPoint(headMotionControl.currentMotionControl.position).y;
    }
}
