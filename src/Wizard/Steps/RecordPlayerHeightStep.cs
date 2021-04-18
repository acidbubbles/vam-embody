using System.Linq;

public class RecordPlayerHeightStep : WizardStepBase, IWizardStep
{
    private readonly PlayerMeasurements _playerMeasurements;

    public string helpText => @"
We will now <b>measure your height</b>.

- Place one <b>controller on the ground</b> (optional)
- Stand <b>straight</b>
- Look <b>forward</b>

Press <b>Next</b> when ready.".TrimStart();

    public RecordPlayerHeightStep(EmbodyContext context)
        : base(context)
    {
        _playerMeasurements = new PlayerMeasurements(context);
    }

    public bool Apply()
    {
        context.worldScale.playerHeightJSON.val = _playerMeasurements.MeasureHeight();
        context.worldScale.worldScaleMethodJSON.val = WorldScaleModule.PlayerHeightMethod;

        context.diagnostics.TakeSnapshot($"{nameof(RecordPlayerHeightStep)}.{nameof(Apply)}");

        return true;
    }

    public override void Leave(bool final)
    {
        base.Leave(final);

        context.worldScale.selectedJSON.val = false;
        if (context.worldScale.worldScaleMethodJSON.val == WorldScaleModule.PlayerHeightMethod)
            context.worldScale.ApplyWorldScale();
    }
}
