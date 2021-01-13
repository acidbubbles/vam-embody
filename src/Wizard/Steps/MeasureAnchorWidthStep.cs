using UnityEngine;

public class MeasureAnchorWidthStep : IWizardStep
{
    public string helpText => $"Possession activated. Now put your hands on your real {_part}, and press select when ready.";

    private readonly string _part;
    private readonly ControllerAnchorPoint _anchor;

    public MeasureAnchorWidthStep(string part, ControllerAnchorPoint anchor)
    {
        _part = part;
        _anchor = anchor;
    }

    public void Run(WizardContext context)
    {
        // TODO: Highlight the ring where we want the hands to be.
        // TODO: Make the model move their hand in the right position.
        var gameHipsCenter = _anchor.GetInGameWorldPosition();
        // TODO: Check the forward size too, and the offset.
        // TODO: Don't check the _hand control_ distance, instead check the relevant distance (from inside the hands)
        var realHipsWidth = Vector3.Distance(context.realLeftHand.position, context.realRightHand.position) - context.handsDistance;
        var realHipsXCenter = (context.realLeftHand.position + context.realRightHand.position) / 2f;
        _anchor.RealLifeSize = new Vector3(realHipsWidth, 0f, _anchor.InGameSize.z);
        _anchor.RealLifeOffset = realHipsXCenter - gameHipsCenter;
        SuperController.LogMessage($"Real Hips height: {realHipsXCenter.y}, Game Hips height: {gameHipsCenter}");
        SuperController.LogMessage($"Real Hips width: {realHipsWidth}, Game Hips width: {_anchor.RealLifeSize.x}");
        SuperController.LogMessage($"Real Hips center: {realHipsXCenter}, Game Hips center: {gameHipsCenter}");
    }
}
