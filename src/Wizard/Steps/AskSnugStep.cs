using System.Collections.Generic;
using System.Linq;

public class AskSnugStep : WizardStepBase, IWizardStep
{
    public string helpText => "Do you want to setup Snug?\n\nThis will dynamically adjust your hands so despite body proportion differences, your in-game hands position will match your own in relation to your body.\n\nSelect Next to configure Snug, or Skip to disable Snug.";

    private readonly List<IWizardStep> _steps;

    public AskSnugStep(EmbodyContext context, List<IWizardStep> steps)
        : base(context)
    {
        _steps = steps;
    }

    public void Apply()
    {
        _steps.Add(new ActivateWithoutSnugStep(context));
        _steps.Add(new MeasureHandsPaddingStep(context));
        foreach (var anchor in context.snug.anchorPoints.Where(a => !a.locked && a.active))
        {
            _steps.Add(new MeasureAnchorWidthStep(context, anchor));
            _steps.Add(new MeasureAnchorDepthAndOffsetStep(context, anchor));
        }
        _steps.Add(new DeactivateAndRestoreSnugStep(context));
    }
}
