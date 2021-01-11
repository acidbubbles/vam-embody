using System.Collections;
using UnityEngine;

public class EmbodyContext
{
    public readonly MVRScript plugin;
    public Atom containingAtom => plugin.containingAtom;

    public EmbodyContext(MVRScript plugin)
    {
        this.plugin = plugin;
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        return plugin.StartCoroutine(routine);
    }
}
