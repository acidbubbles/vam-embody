using System.Linq;
using UnityEngine;

public class MeasureAnchorDepthAndOffsetStep : WizardStepBase, IWizardStep
{
    public string helpText => $"Now put your right hand at the same level as your {_anchor.label} but on the front, squeezed on you.";

    private readonly ControllerAnchorPoint _anchor;
    private readonly FreeControllerV3 _leftHandControl;
    private readonly FreeControllerV3 _rightHandControl;
    private FreeControllerV3Snapshot _leftHandSnapshot;
    private FreeControllerV3Snapshot _rightHandSnapshot;

    public MeasureAnchorDepthAndOffsetStep(EmbodyContext context, ControllerAnchorPoint anchor)
        : base(context)
    {
        _anchor = anchor;
        _leftHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "lHandControl");
        _rightHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
    }

    public override void Enter()
    {
        _leftHandSnapshot = FreeControllerV3Snapshot.Snap(_leftHandControl);
        _rightHandSnapshot = FreeControllerV3Snapshot.Snap(_rightHandControl);
    }

    public void Apply()
    {
        var adjustedHipsCenter = _anchor.GetAdjustedWorldPosition();
        var realHipsFront = Vector3.MoveTowards(context.rightHand.position, adjustedHipsCenter, TrackersConstants.handsDistance / 2f);
        _anchor.realLifeSize = new Vector3(_anchor.realLifeSize.x, 0f, Vector3.Distance(realHipsFront, adjustedHipsCenter) * 2f);

        _anchor.Update();
    }

    public override void Leave()
    {
        _leftHandSnapshot.Restore(true);
        _rightHandSnapshot.Restore(true);
    }
}
