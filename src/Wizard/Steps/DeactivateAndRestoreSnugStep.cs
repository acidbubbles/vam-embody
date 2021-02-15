using System.Linq;

public class DeactivateAndRestoreSnugStep : WizardStepBase, IWizardStep
{
    public string helpText => "Snug setup is complete, select Next to exit possession.";

    public DeactivateAndRestoreSnugStep(EmbodyContext context)
        : base(context)
    {
    }

    public override void Enter()
    {
        base.Enter();

        context.snug.previewSnugOffsetJSON.val = true;
    }

    public void Apply()
    {
    }

    public override void Leave()
    {
        base.Leave();

        context.embody.activeJSON.val = false;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = true;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = true;
    }
}
