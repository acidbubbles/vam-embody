using System.Linq;

public class RecordViveTrackersFeetStep : WizardStepBase, IWizardStep
{
    public string helpText => @"
Align your feet to match the model's feet position and angle as closely as possible, and select Next.\n\nSkip if you don't plan on using feet trackers.".TrimStart();

    public RecordViveTrackersFeetStep(EmbodyContext context)
        : base(context)
    {

    }

    public bool Apply()
    {
        var autoSetup = new TrackerAutoSetup(context.containingAtom);
        var feet = context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("FootControl")).ToList();
        var trackersNearFloor = context.trackers.viveTrackers
            .Where(t => t.SyncMotionControl())
            .Where(t => t.currentMotionControl.position.y < 0.25f)
            .ToList();

        if (trackersNearFloor.Count != 2)
        {
            lastError = $"Expected to find 2 trackers near floor level, but {trackersNearFloor.Count} were found.\n\nFoot trackers were not assigned.\n\nTry again, or skip this step.";
            return false;
        }

        foreach (var mc in trackersNearFloor)
        {
            if (!mc.SyncMotionControl()) continue;
            autoSetup.AttachToClosestNode(mc, feet);
        }

        if (trackersNearFloor[0].mappedControllerName == trackersNearFloor[1].mappedControllerName)
        {
            lastError = $"Embody: Both vive trackers were mapped to the same foot.\n\nMake sure your feet are each placed close to the model's feet.\n\nTry again, or skip this step.";
            trackersNearFloor[0].mappedControllerName = null;
            trackersNearFloor[1].mappedControllerName = null;
            return false;
        }

        context.Refresh();
        return true;
    }

    public override void Update()
    {
        // TODO: Enable and disable the preview
        // TODO: Draw lines connecting trackers with their closest target
    }
}
