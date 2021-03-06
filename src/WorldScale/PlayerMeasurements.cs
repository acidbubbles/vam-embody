﻿using System.Linq;
using UnityEngine;

public class PlayerMeasurements
{
    public static bool lastMeasurementUsedControllerAsFloor { get; private set; }

    private readonly EmbodyContext _context;

    public PlayerMeasurements(EmbodyContext context)
    {
        _context = context;
    }

    public float MeasureHeight()
    {
        lastMeasurementUsedControllerAsFloor = true;
        var headMotionControl = _context.trackers.motionControls.First(mc => mc.name == MotionControlNames.Head);
        if (_context.LeftHand() != null && _context.RightHand() != null)
        {
            var lowestMotionControl = Mathf.Min(_context.LeftHand().position.y, _context.RightHand().position.y);

            if (lowestMotionControl < headMotionControl.currentMotionControl.position.y / 10f)
            {
                lastMeasurementUsedControllerAsFloor = true;
                return headMotionControl.currentMotionControl.position.y - lowestMotionControl;
            }
        }

        return SuperController.singleton.heightAdjustTransform.InverseTransformPoint(headMotionControl.currentMotionControl.position).y;
    }
}
