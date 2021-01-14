using System.Linq;
using UnityEngine;

public class MeasureHandsPaddingStep : IWizardStep, IWizardUpdate
{
    public string helpText => "Now put your hands together like you're praying, and press select when ready.";
    private readonly WizardContext _context;
    private FreeControllerV3 _leftHandControl;
    private FreeControllerV3 _rightHandControl;
    private Rigidbody _head;

    public MeasureHandsPaddingStep(WizardContext context)
    {
        _context = context;
        _head = context.containingAtom.rigidbodies.First(fc => fc.name == "head");
        _leftHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "lHandControl");
        _rightHandControl = context.containingAtom.freeControllers.First(fc => fc.name == "rHandControl");
    }

    public void Update()
    {
        // TODO: Fingers
        _leftHandControl.control.position = _head.position + _head.transform.forward * 0.3f + Vector3.down * 0.2f + _head.transform.right * -0.01f;
        _leftHandControl.control.eulerAngles = _head.rotation.eulerAngles;
        _leftHandControl.control.Rotate(new Vector3(180, 0, 90 + 10));

        _rightHandControl.control.position = _head.position + _head.transform.forward * 0.3f + Vector3.down * 0.2f + _head.transform.right * 0.01f;
        _rightHandControl.control.eulerAngles = _head.rotation.eulerAngles;
        _rightHandControl.control.Rotate(new Vector3(180, 0, 270 - 10));
    }

    public void Run()
    {
        _context.handsDistance = Vector3.Distance(_context.context.leftHand.position, _context.context.rightHand.position);
        // TODO: Measure hand center?
        #warning Debug
        _context.handsDistance = 0.03f;
    }
}
