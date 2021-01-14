using System.Linq;
using UnityEngine;

public class MeasureAnchorWidthStep : IWizardStep, IWizardUpdate
{
    public string helpText => $"Possession activated. Now put your hands on your real {_part}, and press select when ready.";

    private readonly WizardContext _context;
    private readonly string _part;
    private readonly ControllerAnchorPoint _anchor;
    private FreeControllerV3 _leftHandControl;
    private FreeControllerV3 _rightHandControl;

    public MeasureAnchorWidthStep(WizardContext context, string part, ControllerAnchorPoint anchor)
    {
        _context = context;
        _part = part;
        _anchor = anchor;
        _leftHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "lHandControl");
        _rightHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
    }

    public void Update()
    {
        // VisualCuesHelper.Cross(Color.green).transform.position = _anchor.GetInGameWorldPosition() + (-_anchor.RigidBody.transform.right * (_anchor.InGameSize.x / 2f + _context.handsDistance / 2f));
        // TODO: Cancel out the hand size, whatever that is. Figure out if we should compute it or just hardcode it.
        _leftHandControl.control.position = _anchor.GetInGameWorldPosition() + (-_anchor.RigidBody.transform.right * (_anchor.InGameSize.x / 2f + _context.handsDistance / 2f));
        _leftHandControl.control.eulerAngles = _anchor.RigidBody.rotation.eulerAngles;
        _leftHandControl.control.Rotate(new Vector3(0, 0, 90));

        _rightHandControl.control.position = _anchor.GetInGameWorldPosition() + (_anchor.RigidBody.transform.right * (_anchor.InGameSize.x / 2f + _context.handsDistance / 2f));
        _rightHandControl.control.eulerAngles = _anchor.RigidBody.rotation.eulerAngles;
        _rightHandControl.control.Rotate(new Vector3(0, 0, -90));
    }

    public void Run()
    {
        // TODO: Highlight the ring where we want the hands to be.
        // TODO: Make the model move their hand in the right position.
        var gameHipsCenter = _anchor.GetInGameWorldPosition();
        // TODO: Check the forward size too, and the offset.
        // TODO: Don't check the _hand control_ distance, instead check the relevant distance (from inside the hands)
        var realHipsWidth = Vector3.Distance(_context.realLeftHand.position, _context.realRightHand.position) - _context.handsDistance;
        var realHipsXCenter = (_context.realLeftHand.position + _context.realRightHand.position) / 2f;
        _anchor.RealLifeSize = new Vector3(realHipsWidth, 0f, _anchor.InGameSize.z);
        _anchor.RealLifeOffset = realHipsXCenter - gameHipsCenter;
        SuperController.LogMessage($"Real Hips height: {realHipsXCenter.y}, Game Hips height: {gameHipsCenter}");
        SuperController.LogMessage($"Real Hips width: {realHipsWidth}, Game Hips width: {_anchor.RealLifeSize.x}");
        SuperController.LogMessage($"Real Hips center: {realHipsXCenter}, Game Hips center: {gameHipsCenter}");
    }
}
