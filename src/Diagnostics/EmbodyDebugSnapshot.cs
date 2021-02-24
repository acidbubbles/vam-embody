using System.Globalization;
using SimpleJSON;
using UnityEngine;

public class EmbodyDebugSnapshot
{
    public string name;
    public EmbodyTransformDebugSnapshot head;
    public EmbodyTransformDebugSnapshot leftHand;
    public EmbodyTransformDebugSnapshot rightHand;
    public EmbodyTransformDebugSnapshot viveTracker1;
    public EmbodyTransformDebugSnapshot viveTracker2;
    public EmbodyTransformDebugSnapshot viveTracker3;
    public EmbodyTransformDebugSnapshot viveTracker4;
    public EmbodyTransformDebugSnapshot viveTracker5;
    public EmbodyTransformDebugSnapshot viveTracker6;
    public EmbodyTransformDebugSnapshot viveTracker7;
    public EmbodyTransformDebugSnapshot viveTracker8;
    public JSONClass pluginJSON;
    public float worldScale;
    public float playerHeightAdjust;
    public EmbodyTransformDebugSnapshot navigationRig;
    public JSONArray poseJSON;

    public JSONClass ToJSON()
    {
        return new JSONClass
        {
            {"Name", name},
            {"WorldScale", worldScale.ToString(NumberFormatInfo.InvariantInfo)},
            {"PlayerHeightAdjust", playerHeightAdjust.ToString(NumberFormatInfo.InvariantInfo)},
            {"NavigationRig", navigationRig?.ToJSON()},
            {"Head", head?.ToJSON()},
            {"LeftHand", leftHand?.ToJSON()},
            {"RightHand", rightHand?.ToJSON()},
            {"ViveTracker1", viveTracker1?.ToJSON()},
            {"ViveTracker2", viveTracker2?.ToJSON()},
            {"ViveTracker3", viveTracker3?.ToJSON()},
            {"ViveTracker4", viveTracker4?.ToJSON()},
            {"ViveTracker5", viveTracker5?.ToJSON()},
            {"ViveTracker6", viveTracker6?.ToJSON()},
            {"ViveTracker7", viveTracker7?.ToJSON()},
            {"ViveTracker8", viveTracker8?.ToJSON()},
            {"Plugin", pluginJSON}
        };
    }

    public static EmbodyDebugSnapshot FromJSON(JSONClass jc)
    {
        return new EmbodyDebugSnapshot
        {
            name = jc["Name"].Value,
            worldScale = jc["WorldScale"].AsFloat,
            playerHeightAdjust = jc["PlayerHeightAdjust"].AsFloat,
            navigationRig = EmbodyTransformDebugSnapshot.FromJSON(jc["NavigationRig"]),
            head = EmbodyTransformDebugSnapshot.FromJSON(jc["Head"]),
            leftHand = EmbodyTransformDebugSnapshot.FromJSON(jc["LeftHand"]),
            rightHand = EmbodyTransformDebugSnapshot.FromJSON(jc["RightHand"]),
            viveTracker1 = EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker1"]),
            viveTracker2 = EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker2"]),
            viveTracker3 = EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker3"]),
            viveTracker4 = EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker4"]),
            viveTracker5 = EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker5"]),
            viveTracker6 = EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker6"]),
            viveTracker7 = EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker7"]),
            viveTracker8 = EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker8"]),
            pluginJSON = jc["Plugin"].AsObject
        };
    }
}

public class EmbodyTransformDebugSnapshot
{
    public Vector3 position { get; set; }
    public Vector3 rotation { get; set; }

    public JSONNode ToJSON()
    {
        return new JSONClass
        {
            {"Position", position.ToJSON()},
            {"Rotation", rotation.ToJSON()}
        };
    }

    public static EmbodyTransformDebugSnapshot FromJSON(JSONNode jsonNode)
    {
        return new EmbodyTransformDebugSnapshot
        {
            position = jsonNode["Position"].ToVector3(Vector3.zero),
            rotation = jsonNode["Rotation"].ToVector3(Vector3.zero)
        };
    }

    public static EmbodyTransformDebugSnapshot From(Transform transform)
    {
        if (transform == null) return null;
        if (!transform.gameObject.activeInHierarchy) return null;
        return new EmbodyTransformDebugSnapshot
        {
            position = transform.position,
            rotation = transform.eulerAngles
        };
    }

    public override string ToString()
    {
        return $"Position: {position.x:0.000}, {position.y:0.000}, {position.z:0.000}\nRotation: {rotation.x:0.000}, {rotation.y:0.000}, {rotation.z:0.000}";
    }
}
