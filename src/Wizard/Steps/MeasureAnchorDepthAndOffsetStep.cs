using UnityEngine;

public class MeasureAnchorDepthAndOffsetStep : WizardStepBase, IWizardStep
{
    public string helpText => $"Now put your right hand at the same level as your {_part} but on the front, squeezed on you.";

    private readonly string _part;
    private readonly ControllerAnchorPoint _anchor;

    public MeasureAnchorDepthAndOffsetStep(EmbodyContext context, string part, ControllerAnchorPoint anchor)
        : base(context)
    {
        _part = part;
        _anchor = anchor;
    }

    public void Apply()
    {
        var adjustedHipsCenter = _anchor.GetAdjustedWorldPosition();
        var realHipsFront = Vector3.MoveTowards(context.rightHand.position, adjustedHipsCenter, TrackersConstants.handsDistance / 2f);
        _anchor.realLifeSize = new Vector3(_anchor.realLifeSize.x, 0f, Vector3.Distance(realHipsFront, adjustedHipsCenter) * 2f);

        _anchor.Update();
    }
}
