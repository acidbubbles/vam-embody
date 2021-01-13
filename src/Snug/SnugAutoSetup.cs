using System.Collections.Generic;
using UnityEngine;

public class SnugAutoSetup
{
    private readonly Atom _containingAtom;
    private readonly ISnugModule _snug;

    public SnugAutoSetup(Atom containingAtom, ISnugModule snug)
    {
        _containingAtom = containingAtom;
        _snug = snug;
    }

    public void AutoSetup()
    {
        // TODO: Recalculate when the y offset is changed
        // TODO: Check when the person scale changes
        var colliders = ScanBodyColliders().ToList();
        foreach (var anchor in _snug.anchorPoints)
        {
            if (anchor.Locked) continue;
            // if (anchor.Label != "Abdomen") continue;
            AutoSetup(anchor.RigidBody, anchor, colliders);
        }
        // TODO: If the UI is open, the sliders will be wrong. Determine when is the right time to do the auto-setup.
    }

    private static void AutoSetup(Component rb, ControllerAnchorPoint anchor, IEnumerable<Collider> colliders)
    {
        const float raycastDistance = 100f;
        var rbTransform = rb.transform;
        var rbUp = rbTransform.up;
        var rbOffsetPosition = rbTransform.position + rbUp * anchor.InGameOffset.y;
        var rbRotation = rbTransform.rotation;
        var rbForward = rbTransform.forward;

        var rays = new List<Ray>();
        for (var i = 0; i < 360; i += 5)
        {
            var rotation = Quaternion.AngleAxis(i, rbUp);
            var origin = rbOffsetPosition + rotation * (rbForward * raycastDistance);
            rays.Add(new Ray(origin, rbOffsetPosition - origin));
        }

        var min = Vector3.positiveInfinity;
        var max = Vector3.negativeInfinity;
        var isHit = false;
        foreach (var collider in colliders)
        {
            foreach (var ray in rays)
            {
                RaycastHit hit;
                if (!collider.Raycast(ray, out hit, raycastDistance)) continue;
                isHit = true;
                min = Vector3.Min(min, hit.point);
                max = Vector3.Max(max, hit.point);

                // var hitCue = VisualCuesHelper.CreatePrimitive(null, PrimitiveType.Cube, new Color(0f, 1f, 0f, 0.2f));
                // _cues.Add(hitCue);
                // hitCue.transform.localScale = Vector3.one * 0.002f;
                // hitCue.transform.position = hit.point;
            }
        }

        if (!isHit) return;

        var size = Quaternion.Inverse(rbRotation) * (max - min);
        var center = min + (max - min) / 2f;
        // TODO: Why add virtual offset here?
        var offset = center - rbTransform.position; //* + rbUp * anchor.VirtualOffset.y;

        // var cue = VisualCuesHelper.Cross(Color.red);
        // _cues.Add(cue);
        // cue.transform.localScale = Vector3.one * 2f;
        // cue.transform.position = center;

        // TODO: Adjust padding for scale?
        var padding = new Vector3(0.02f, 0f, 0.02f);

        anchor.InGameSize = size + padding;
        anchor.InGameOffset = offset;
        anchor.RealLifeSize = anchor.InGameSize;
        anchor.RealLifeOffset = Vector3.zero;
        anchor.Update();
    }

    private IEnumerable<Collider> ScanBodyColliders()
    {
        var personRoot = _containingAtom.transform.Find("rescale2");
        // Those are the ones where colliders can actually be found for female:
        //.Find("geometry").Find("FemaleMorphers")
        //.Find("PhysicsModel").Find("Genesis2Female")
        return ScanBodyColliders(personRoot);
    }

    private IEnumerable<Collider> ScanBodyColliders(Transform root)
    {
        if(root.name == "lCollar" || root.name == "rCollar") yield break;
        if(root.name == "lShin" || root.name == "rShin") yield break;

        foreach (var collider in root.GetComponents<Collider>())
        {
            if (!collider.enabled) continue;
            yield return collider;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            foreach (var collider in ScanBodyColliders(child))
                yield return collider;
        }
    }
}
