using System.Globalization;
using SimpleJSON;
using UnityEngine;

public static class Vector3Extensions
{
    public static JSONClass ToJSON(this Vector3 v)
    {
        return new JSONClass
        {
            {"x", v.x.ToString(CultureInfo.InvariantCulture)},
            {"y", v.y.ToString(CultureInfo.InvariantCulture)},
            {"z", v.z.ToString(CultureInfo.InvariantCulture)},
        };
    }

    public static Vector3 ToVector3(this JSONNode json, Vector3 defaultValue)
    {
        var jc = json.AsObject;
        if (!jc.HasKey("x")) return defaultValue;
        return new Vector3(
            float.Parse(jc["x"], CultureInfo.InvariantCulture),
            float.Parse(jc["y"], CultureInfo.InvariantCulture),
            float.Parse(jc["z"], CultureInfo.InvariantCulture)
        );
    }
}
