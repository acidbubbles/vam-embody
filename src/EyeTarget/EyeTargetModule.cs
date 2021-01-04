using UnityEngine;

public interface IEyeTarget : IEmbodyModule
{
}

public class EyeTargetModule : EmbodyModuleBase, IEyeTarget
{
    public override void Init()
    {
        base.Init();

        // TODO: Get the eye bones center
    }

    public override void OnEnable()
    {
        base.OnEnable();

        // TODO: Find mirrors
    }

    public override void OnDisable()
    {
        base.OnDisable();
    }

    public void Update()
    {
        // TODO: Update the eye target position so it's behind the mirror in a straight line, at the same distance behind it. Find the first mirror the model is looking at (closest to lookat ray)
    }
}
