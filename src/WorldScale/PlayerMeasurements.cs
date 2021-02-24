using System.Linq;
using UnityEngine;

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
        if (_context.leftHand != null && _context.rightHand != null)
        {
            var lowestMotionControl = Mathf.Min(_context.leftHand.position.y, _context.rightHand.position.y);

            if (lowestMotionControl < headMotionControl.currentMotionControl.position.y / 10f)
                return headMotionControl.currentMotionControl.position.y - lowestMotionControl;
        }

        return SuperController.singleton.heightAdjustTransform.InverseTransformPoint(headMotionControl.currentMotionControl.position).y;
    }
}
