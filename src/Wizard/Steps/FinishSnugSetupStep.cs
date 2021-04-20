using System.Linq;

public class FinishSnugSetupStep : WizardStepBase, IWizardStep
{
    public string helpText => @"
<b>Snug setup is complete</b>!

You can see in white the model proportions, and in green, yours. There is also a line that shows how your hands are offset to compensate.

Select Next to exit possession.".TrimStart();

    public FinishSnugSetupStep(EmbodyContext context)
        : base(context)
    {
    }

    public override void Enter()
    {
        base.Enter();

        context.embody.RefreshDelayed();
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = true;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = true;
        var lElbowMotionControl = context.trackers.motionControls.FirstOrDefault(mc => mc.mappedControllerName == "lElbowControl");
        if (lElbowMotionControl != null) lElbowMotionControl.enabled = true;
        var rElbowMotionControl = context.trackers.motionControls.FirstOrDefault(mc => mc.mappedControllerName == "rElbowControl");
        if (rElbowMotionControl != null) rElbowMotionControl.enabled = true;
        context.snug.previewSnugOffsetJSON.val = true;
        context.snug.selectedJSON.val = true;
    }

    public bool Apply()
    {
        context.diagnostics.TakeSnapshot($"{nameof(FinishSnugSetupStep)}.{nameof(Apply)}");

        return true;
    }

    public override void Leave(bool final)
    {
        base.Leave(final);

        context.embody.activeJSON.val = false;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = true;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = true;
        var lElbowControl = context.trackers.motionControls.FirstOrDefault(mc => mc.mappedControllerName == "lElbowControl");
        if (lElbowControl != null) lElbowControl.enabled = true;
        var rElbowControl = context.trackers.motionControls.FirstOrDefault(mc => mc.mappedControllerName == "rElbowControl");
        if (rElbowControl != null) rElbowControl.enabled = true;
    }
}
