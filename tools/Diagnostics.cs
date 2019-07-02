#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Diagnostics
{
    public static void DumpSceneGameObjects()
    {
        foreach (var o in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            PrintTree(o.gameObject, "UI");
        }
    }

    public static void PrintTree(GameObject o, params string[] exclude)
    {
        PrintTree(0, o, exclude, new HashSet<GameObject>());
    }

    public static void PrintTree(int indent, GameObject o, string[] exclude, HashSet<GameObject> found)
    {
        if (found.Contains(o))
        {
            SuperController.LogMessage("|" + new String(' ', indent) + " [" + o.tag + "] " + o.name + " {RECURSIVE}");
            return;
        }
        if (o == null)
        {
            SuperController.LogMessage("|" + new String(' ', indent) + "{null}");
            return;
        }
        if (exclude.Any(x => o.gameObject.name.Contains(x)))
        {
            return;
        }
        found.Add(o);
        SuperController.LogMessage("|" + new String(' ', indent) + " [" + o.tag + "] " + o.name);
        for (int i = 0; i < o.transform.childCount; i++)
        {
            var under = o.transform.GetChild(i).gameObject;
            PrintTree(indent + 4, under, exclude, found);
        }
    }

    public static string GetHierarchy(GameObject o)
    {
        if (o == null)
            return "{null}";

        var items = new List<string>(new[] { o.name });
        GameObject parent = o;
        for (int i = 0; i < 100; i++)
        {
            parent = parent.transform.parent?.gameObject;
            if (parent == null || parent == o) break;
            items.Insert(0, parent.gameObject.name);
        }
        return string.Join(" -> ", items.ToArray());
    }


    public static void SimulateSave()
    {
        var j = new SimpleJSON.JSONArray();
        foreach (var atom in SuperController.singleton.GetAtoms())
        {
            if (!atom.name.Contains("Mirror") && !atom.name.Contains("Glass")) continue;

            try
            {
                // atom.GetJSON(true, true, true);
                foreach (var id in atom.GetStorableIDs())
                {
                    var stor = atom.GetStorableByID(id);
                    if (stor.gameObject == null) throw new NullReferenceException("123");
                    try
                    {
                        if (stor == null) throw new Exception("Case 1");
                        if (stor.enabled == false) throw new Exception("Case 2");
                        SuperController.LogMessage("Storage" + atom.name + "/" + stor.name + " (" + stor.storeId + ")");
                        string[] value = stor.GetAllFloatAndColorParamNames().ToArray();
                        SuperController.LogMessage(" -" + string.Join(", ", value));
                        // var x = stor.name;
                        // stor.GetJSON();
                    }
                    catch (Exception se)
                    {
                        SuperController.LogMessage("Error with " + atom.name + "/" + stor.name + ": " + se);
                    }
                }
                // atom.Store(j);
            }
            catch (Exception je)
            {
                SuperController.LogMessage("Error with " + GetHierarchy(atom.gameObject) + " " + atom + ": " + je);
            }
        }
    }

    internal static IEnumerable<GameObject> AllChildren(GameObject gameObject)
    {
        return gameObject.GetComponentsInChildren<MonoBehaviour>().GroupBy(b => b.gameObject).Select(x => x.Key).Where(o => o != gameObject);
    }
}