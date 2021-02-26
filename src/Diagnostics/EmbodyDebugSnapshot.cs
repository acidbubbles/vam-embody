using System.Globalization;
using SimpleJSON;
using UnityEngine;

public class EmbodyDebugSnapshot
{
    public string name;
    public JSONClass pluginJSON;
    public JSONArray poseJSON;
    public float worldScale;
    public float playerHeightAdjust;
    public string vrMode;
    public EmbodyTransformDebugSnapshot navigationRig;
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

    public JSONClass ToJSON()
    {
        var jc = new JSONClass
        {
            {"Name", name ?? "Unnamed"},
            {"VRMode", vrMode ?? "Unspecified"},
            {"WorldScale", worldScale.ToString(NumberFormatInfo.InvariantInfo)},
            {"PlayerHeightAdjust", playerHeightAdjust.ToString(NumberFormatInfo.InvariantInfo)},
        };
        if (navigationRig != null) jc["NavigationRig"] = navigationRig.ToJSON();
        if (head != null) jc["Head"] = head.ToJSON();
        if (leftHand != null) jc["LeftHand"] = leftHand.ToJSON();
        if (rightHand != null) jc["RightHand"] = rightHand.ToJSON();
        if (viveTracker1 != null) jc["ViveTracker1"] = viveTracker1.ToJSON();
        if (viveTracker2 != null) jc["ViveTracker2"] = viveTracker2.ToJSON();
        if (viveTracker3 != null) jc["ViveTracker3"] = viveTracker3.ToJSON();
        if (viveTracker4 != null) jc["ViveTracker4"] = viveTracker4.ToJSON();
        if (viveTracker5 != null) jc["ViveTracker5"] = viveTracker5.ToJSON();
        if (viveTracker6 != null) jc["ViveTracker6"] = viveTracker6.ToJSON();
        if (viveTracker7 != null) jc["ViveTracker7"] = viveTracker7.ToJSON();
        if (viveTracker8 != null) jc["ViveTracker8"] = viveTracker8.ToJSON();
        if (pluginJSON != null) jc["Plugin"] = pluginJSON;
        if (poseJSON != null) jc["Pose"] = poseJSON;
        return jc;
    }

    public static EmbodyDebugSnapshot FromJSON(JSONClass jc)
    {
        return new EmbodyDebugSnapshot
        {
            name = jc.HasKey("Name") ? jc["Name"].Value : null,
            vrMode = jc.HasKey("VRMode") ? jc["VRMode"].Value : null,
            worldScale = jc.HasKey("WorldScale") ? jc["WorldScale"].AsFloat : 0f,
            playerHeightAdjust = jc.HasKey("PlayerHeightAdjust") ? jc["PlayerHeightAdjust"].AsFloat : 0f,
            navigationRig = jc.HasKey("NavigationRig") ? EmbodyTransformDebugSnapshot.FromJSON(jc["NavigationRig"]) : null,
            head = jc.HasKey("Head") ? EmbodyTransformDebugSnapshot.FromJSON(jc["Head"]) : null,
            leftHand = jc.HasKey("LeftHand") ? EmbodyTransformDebugSnapshot.FromJSON(jc["LeftHand"]) : null,
            rightHand = jc.HasKey("RightHand") ? EmbodyTransformDebugSnapshot.FromJSON(jc["RightHand"]) : null,
            viveTracker1 = jc.HasKey("ViveTracker1") ? EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker1"]) : null,
            viveTracker2 = jc.HasKey("ViveTracker2") ? EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker2"]) : null,
            viveTracker3 = jc.HasKey("ViveTracker3") ? EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker3"]) : null,
            viveTracker4 = jc.HasKey("ViveTracker4") ? EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker4"]) : null,
            viveTracker5 = jc.HasKey("ViveTracker5") ? EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker5"]) : null,
            viveTracker6 = jc.HasKey("ViveTracker6") ? EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker6"]) : null,
            viveTracker7 = jc.HasKey("ViveTracker7") ? EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker7"]) : null,
            viveTracker8 = jc.HasKey("ViveTracker8") ? EmbodyTransformDebugSnapshot.FromJSON(jc["ViveTracker8"]) : null,
            pluginJSON = jc.HasKey("Plugin") ? jc["Plugin"].AsObject : null,
            poseJSON = jc.HasKey("Pose") ? jc["Pose"].AsArray : null
        };
    }
}

public class EmbodyTransformDebugSnapshot
{
    public Vector3 position;
    public Vector3 rotation;

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
