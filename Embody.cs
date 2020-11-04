using System;

public class Embody : MVRScript
{
    public override void Init()
    {
        try
        {
            SuperController.LogMessage($"{nameof(Embody)} initialized");
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(Embody)}.{nameof(Init)}: {e}");
        }
    }

    public void OnEnable()
    {
        try
        {
            SuperController.LogMessage($"{nameof(Embody)} enabled");
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(Embody)}.{nameof(OnEnable)}: {e}");
        }
    }

    public void OnDisable()
    {
        try
        {
            SuperController.LogMessage($"{nameof(Embody)} disabled");
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(Embody)}.{nameof(OnDisable)}: {e}");
        }
    }

    public void OnDestroy()
    {
        try
        {
            SuperController.LogMessage($"{nameof(Embody)} destroyed");
        }
        catch (Exception e)
        {
            SuperController.LogError($"{nameof(Embody)}.{nameof(OnDestroy)}: {e}");
        }
    }
}
