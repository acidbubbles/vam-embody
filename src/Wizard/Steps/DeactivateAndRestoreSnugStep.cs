using System.Linq;

public class DeactivateAndRestoreSnugStep : WizardStepBase, IWizardStep
{
    public string helpText => "Snug setup is complete, continue to exit possession.";

    public DeactivateAndRestoreSnugStep(EmbodyContext context)
        : base(context)
    {
    }

    public void Apply()
    {
        context.embody.activeJSON.val = false;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = true;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = true;
    }
}
