using UnityEngine;

public class MeasureAnchorDepthAndOffsetStep : IWizardStep
{
    public string helpText => $"Now put your right hand at the same level as your {_part} but on the front, squeezed on you.";

    private readonly string _part;
    private readonly ControllerAnchorPoint _anchor;

    public MeasureAnchorDepthAndOffsetStep(string part, ControllerAnchorPoint anchor)
    {
        _part = part;
        _anchor = anchor;
    }

    public void Run(EmbodyWizardContext context)
    {
        var adjustedHipsCenter = _anchor.GetAdjustedWorldPosition();
        var realHipsFront = Vector3.MoveTowards(context.realRightHand.position, adjustedHipsCenter, context.handsDistance / 2f);
        _anchor.RealLifeSize = new Vector3(_anchor.RealLifeSize.x, 0f, Vector3.Distance(realHipsFront, adjustedHipsCenter) * 2f);

        _anchor.Update();
    }
}
