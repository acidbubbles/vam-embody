using UnityEngine;

public class WizardContext
{
    public Atom containingAtom { get; set; }
    public IEmbody embody { get; set; }
    public ITrackersModule trackers;

    public Transform realLeftHand;
    public Transform realRightHand;

    public float handsDistance;
}
