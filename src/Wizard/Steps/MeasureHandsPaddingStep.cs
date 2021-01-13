using UnityEngine;

public class MeasureHandsPaddingStep : IWizardStep
{
    public string helpText => "Now put your hands together like you're praying, and press select when ready.";
    public void Run(WizardContext context)
    {
        context.handsDistance = Vector3.Distance(context.realLeftHand.position, context.realRightHand.position);
    }
}
