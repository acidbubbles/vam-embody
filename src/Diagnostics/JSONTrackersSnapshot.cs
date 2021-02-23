﻿using System.Globalization;
using Battlehub.UIControls;
using SimpleJSON;
using UnityEngine;

public class JSONTrackersSnapshot
{
    public string name { get; set; }
    public JSONTrackerSnapshot head { get; set; }
    public JSONTrackerSnapshot leftHand { get; set; }
    public JSONTrackerSnapshot rightHand { get; set; }
    public JSONTrackerSnapshot viveTracker1 { get; set; }
    public JSONTrackerSnapshot viveTracker2 { get; set; }
    public JSONTrackerSnapshot viveTracker3 { get; set; }
    public JSONTrackerSnapshot viveTracker4 { get; set; }
    public JSONTrackerSnapshot viveTracker5 { get; set; }
    public JSONTrackerSnapshot viveTracker6 { get; set; }
    public JSONTrackerSnapshot viveTracker7 { get; set; }
    public JSONTrackerSnapshot viveTracker8 { get; set; }
    public JSONClass pluginJSON { get; set; }
    public float worldScale { get; set; }
    public float playerHeightAdjust { get; set; }
    public JSONTrackerSnapshot navigationRig { get; set; }
    public JSONArray poseJSON { get; set; }

    public JSONClass ToJSON()
    {
        return new JSONClass
        {
            {"Name", name},
            {"WorldScale", worldScale.ToString(NumberFormatInfo.InvariantInfo)},
            {"PlayerHeightAdjust", playerHeightAdjust.ToString(NumberFormatInfo.InvariantInfo)},
            {"NavigationRig", navigationRig.ToJSON()},
            {"Head", head.ToJSON()},
            {"LeftHand", leftHand.ToJSON()},
            {"RightHand", rightHand.ToJSON()},
            {"ViveTracker1", viveTracker1.ToJSON()},
            {"ViveTracker2", viveTracker2.ToJSON()},
            {"ViveTracker3", viveTracker3.ToJSON()},
            {"ViveTracker4", viveTracker4.ToJSON()},
            {"ViveTracker5", viveTracker5.ToJSON()},
            {"ViveTracker6", viveTracker6.ToJSON()},
            {"ViveTracker7", viveTracker7.ToJSON()},
            {"ViveTracker8", viveTracker8.ToJSON()},
            {"Plugin", pluginJSON}
        };
    }

    public static JSONTrackersSnapshot FromJSON(JSONClass jc)
    {
        return new JSONTrackersSnapshot
        {
            name = jc["Name"].Value,
            worldScale = jc["WorldScale"].AsFloat,
            playerHeightAdjust = jc["PlayerHeightAdjust"].AsFloat,
            navigationRig = JSONTrackerSnapshot.FromJSON(jc["NavigationRig"]),
            head = JSONTrackerSnapshot.FromJSON(jc["Head"]),
            leftHand = JSONTrackerSnapshot.FromJSON(jc["LeftHand"]),
            rightHand = JSONTrackerSnapshot.FromJSON(jc["RightHand"]),
            viveTracker1 = JSONTrackerSnapshot.FromJSON(jc["ViveTracker1"]),
            viveTracker2 = JSONTrackerSnapshot.FromJSON(jc["ViveTracker2"]),
            viveTracker3 = JSONTrackerSnapshot.FromJSON(jc["ViveTracker3"]),
            viveTracker4 = JSONTrackerSnapshot.FromJSON(jc["ViveTracker4"]),
            viveTracker5 = JSONTrackerSnapshot.FromJSON(jc["ViveTracker5"]),
            viveTracker6 = JSONTrackerSnapshot.FromJSON(jc["ViveTracker6"]),
            viveTracker7 = JSONTrackerSnapshot.FromJSON(jc["ViveTracker7"]),
            viveTracker8 = JSONTrackerSnapshot.FromJSON(jc["ViveTracker8"]),
            pluginJSON = jc["Plugin"].AsObject
        };
    }
}

public class JSONTrackerSnapshot
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

    public static JSONTrackerSnapshot FromJSON(JSONNode jsonNode)
    {
        return new JSONTrackerSnapshot
        {
            position = jsonNode["Position"].ToVector3(Vector3.zero),
            rotation = jsonNode["Rotation"].ToVector3(Vector3.zero)
        };
    }

    public static JSONTrackerSnapshot From(Transform transform)
    {
        return new JSONTrackerSnapshot
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