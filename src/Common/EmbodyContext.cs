using System.Collections;
using System.Linq;
using UnityEngine;

public class EmbodyContext
{
    public readonly MVRScript plugin;
    public Atom containingAtom => plugin.containingAtom;

    public EmbodyContext(MVRScript plugin)
    {
        this.plugin = plugin;
    }

    public void Initialize()
    {
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        return plugin.StartCoroutine(routine);
    }

    public void StopCoroutine(Coroutine routine)
    {
        plugin.StopCoroutine(routine);
    }
}
