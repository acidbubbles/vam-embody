using System.Linq;

public class FinishSnugSetupStep : WizardStepBase, IWizardStep
{
    public string helpText => "<b>Snug setup is complete<b>! Select Next to exit possession.";

    public FinishSnugSetupStep(EmbodyContext context)
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

        context.snug.selectedJSON.val = true;
        context.snug.previewSnugOffsetJSON.val = false;
        context.embody.activeJSON.val = false;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.LeftHand).enabled = true;
        context.trackers.motionControls.First(mc => mc.name == MotionControlNames.RightHand).enabled = true;
    }
}
