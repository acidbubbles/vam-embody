using UnityEngine;

public class MeasureAnchorDepthAndOffsetStep : IWizardStep
{
    public string helpText => $"Now put your right hand at the same level as your {_part} but on the front, squeezed on you.";

    private readonly WizardContext _context;
    private readonly string _part;
    private readonly ControllerAnchorPoint _anchor;

    public MeasureAnchorDepthAndOffsetStep(WizardContext context, string part, ControllerAnchorPoint anchor)
    {
        _context = context;
        _part = part;
        _anchor = anchor;
    }

    public void Run()
    {
        var adjustedHipsCenter = _anchor.GetAdjustedWorldPosition();
        var realHipsFront = Vector3.MoveTowards(_context.context.rightHand.position, adjustedHipsCenter, _context.handsDistance / 2f);
        _anchor.realLifeSize = new Vector3(_anchor.realLifeSize.x, 0f, Vector3.Distance(realHipsFront, adjustedHipsCenter) * 2f);

        _anchor.Update();
    }
}
