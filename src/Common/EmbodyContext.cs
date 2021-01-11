using System.Collections;
using System.Linq;
using UnityEngine;

public class EmbodyContext
{
    public readonly MVRScript plugin;
    public Atom containingAtom => plugin.containingAtom;
    public FreeControllerV3 headControl;

    public EmbodyContext(MVRScript plugin)
    {
        this.plugin = plugin;
    }

    public void Initialize()
    {
        headControl = containingAtom.freeControllers.FirstOrDefault(fc => fc.name == "headControl");
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        return plugin.StartCoroutine(routine);
    }
}
