using System;
/// <summary>
/// Wrist
/// By Acidbubbles
/// Better controller alignement when possessing
/// Source: https://github.com/acidbubbles/vam-wrist
/// </summary>
public class Wrist : MVRScript
{
    public override void Init()
    {
        try
        {
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.Init: " + exc);
        }
    }
    public void Update()
    {
        try
        {
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.Update: " + exc);
        }
    }

    public void OnEnable()
    {
        try
        {
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.OnEnable: " + exc);
        }
    }

    public void OnDisable()
    {
        try
        {
        }
        catch (Exception exc)
        {
            SuperController.LogError("Wrist.OnDisable: " + exc);
        }
    }

    public void OnDestroy()
    {
        OnDisable();
    }
}
