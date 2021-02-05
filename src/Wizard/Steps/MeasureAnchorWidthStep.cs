using System.Linq;
using UnityEngine;

public class MeasureAnchorWidthStep : WizardStepBase, IWizardStep
{
    public string helpText => $"Possession activated. Now put your hands on your real {_anchor.label}, and Press next when ready.";

    private readonly ControllerAnchorPoint _anchor;
    private readonly FreeControllerV3 _leftHandControl;
    private readonly FreeControllerV3 _rightHandControl;
    private FreeControllerV3Snapshot _leftHandSnapshot;
    private FreeControllerV3Snapshot _rightHandSnapshot;

    public MeasureAnchorWidthStep(EmbodyContext context, ControllerAnchorPoint anchor)
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

    public override void Update()
    {
        // VisualCuesHelper.Cross(Color.green).transform.position = _anchor.GetInGameWorldPosition() + (-_anchor.RigidBody.transform.right * (_anchor.InGameSize.x / 2f + _context.handsDistance / 2f));
        // TODO: Cancel out the hand size, whatever that is. Figure out if we should compute it or just hardcode it.
        _leftHandControl.control.position = _anchor.GetInGameWorldPosition() + (-_anchor.bone.transform.right * (_anchor.inGameSize.x / 2f + TrackersConstants.handsDistance / 2f));
        _leftHandControl.control.eulerAngles = _anchor.bone.rotation.eulerAngles;
        _leftHandControl.control.Rotate(new Vector3(0, 0, 90));

        _rightHandControl.control.position = _anchor.GetInGameWorldPosition() + (_anchor.bone.transform.right * (_anchor.inGameSize.x / 2f + TrackersConstants.handsDistance / 2f));
        _rightHandControl.control.eulerAngles = _anchor.bone.rotation.eulerAngles;
        _rightHandControl.control.Rotate(new Vector3(0, 0, -90));
    }

    public void Apply()
    {
        // TODO: Highlight the ring where we want the hands to be.
        // TODO: Make the model move their hand in the right position.
        var gameHipsCenter = _anchor.GetInGameWorldPosition();
        // TODO: Check the forward size too, and the offset.
        // TODO: Don't check the _hand control_ distance, instead check the relevant distance (from inside the hands)
        var realHipsWidth = Vector3.Distance(context.leftHand.position, context.rightHand.position) - TrackersConstants.handsDistance;
        var realHipsXCenter = (context.leftHand.position + context.rightHand.position) / 2f;
        _anchor.realLifeSize = new Vector3(realHipsWidth, 0f, _anchor.inGameSize.z);
        _anchor.realLifeOffset = realHipsXCenter - gameHipsCenter;
        SuperController.LogMessage($"Real Hips height: {realHipsXCenter.y}, Game Hips height: {gameHipsCenter}");
        SuperController.LogMessage($"Real Hips width: {realHipsWidth}, Game Hips width: {_anchor.realLifeSize.x}");
        SuperController.LogMessage($"Real Hips center: {realHipsXCenter}, Game Hips center: {gameHipsCenter}");
    }

    public override void Leave()
    {
        _leftHandSnapshot.Restore(true);
        _rightHandSnapshot.Restore(true);
    }
}
