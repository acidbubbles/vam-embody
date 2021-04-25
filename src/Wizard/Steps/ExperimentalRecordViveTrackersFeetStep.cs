using System.Linq;

public class ExperimentalRecordViveTrackersFeetStep : WizardStepBase, IWizardStep
{
    private float _heightAdjust;

    public string helpText => @"
Align your feet to match the model's feet position and angle as closely as possible, and stand straight.

Press <b>Next</b> when ready.

Skip if you don't plan on using feet trackers.

You can also press Escape to align without possession.".TrimStart();

    public ExperimentalRecordViveTrackersFeetStep(EmbodyContext context)
        : base(context)
    {

    }

    public override void Enter()
    {
        base.Enter();

        context.trackers.previewTrackerOffsetJSON.val = true;
        context.embody.activeJSON.val = true;
        _heightAdjust = SuperController.singleton.playerHeightAdjust;
    }

    public override void Update()
    {
        SuperController.singleton.playerHeightAdjust = _heightAdjust;
    }

    public bool Apply()
    {
        context.diagnostics.TakeSnapshot($"{nameof(ExperimentalRecordViveTrackersFeetStep)}.{nameof(Apply)}.Before");
        var feet = context.containingAtom.freeControllers.Where(fc => fc.name.EndsWith("FootControl")).ToList();
        var eyes = context.head.position.y;
        var floor = context.worldScale.worldScaleMethodJSON.val == WorldScaleModule.PlayerHeightMethod ? eyes - (context.worldScale.playerHeightJSON.val * SuperController.singleton.worldScale) : 0f;
        var maxY = floor + (eyes - floor) * 0.25f;
        var trackersNearFloor = context.trackers.viveTrackers
            .Where(t => t.SyncMotionControl())
            .Where(t => t.currentMotionControl.position.y < maxY)
            .ToList();

        if (trackersNearFloor.Count != 2)
        {
            lastError = $"Expected to find 2 trackers near floor level, but {trackersNearFloor.Count} were found.\n\nFoot trackers were not assigned.\n\nTry again, or skip this step.";
            return false;
        }

        var autoSetup = new TrackerAutoSetup(context);
        foreach (var mc in trackersNearFloor)
        {
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
        context.diagnostics.TakeSnapshot($"{nameof(ExperimentalRecordViveTrackersFeetStep)}.{nameof(Apply)}.After");
        return true;
    }
}
