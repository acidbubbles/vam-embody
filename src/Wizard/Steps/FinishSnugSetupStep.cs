using System.Linq;

public class FinishSnugSetupStep : WizardStepBase, IWizardStep
{
    public string helpText => "<b>Snug setup is complete</b>!\n\nSelect Next to exit possession.";

    public FinishSnugSetupStep(EmbodyContext context)
        : base(context)
    {
    }

    public override void Enter()
    {
        base.Enter();

        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = true;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = true;
        context.snug.previewSnugOffsetJSON.val = true;
        context.snug.selectedJSON.val = true;
    }

    public void Apply()
    {
    }

    public override void Leave()
    {
        base.Leave();

        context.embody.activeJSON.val = false;
        context.snug.previewSnugOffsetJSON.val = false;
    }
}
