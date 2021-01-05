﻿using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

public interface IEyeTarget : IEmbodyModule
{
}

public class EyeTargetModule : EmbodyModuleBase, IEyeTarget
{
    private Transform _lEye;
    private Transform _rEye;
    private FreeControllerV3 _eyeTarget;
    private List<Atom> _mirrors;
    private Vector3 _eyeTargetRestorePosition;
    public override string storeId => "EyeTarget";

    public override void Init()
    {
        base.Init();

        var eyes = containingAtom.GetComponentsInChildren<LookAtWithLimits>();
        _lEye = eyes.First(eye => eye.name == "lEye").transform;
        _rEye = eyes.First(eye => eye.name == "rEye").transform;
        _eyeTarget = containingAtom.freeControllers.First(fc => fc.name == "eyeTargetControl");

        // TODO: Get the eye bones center
    }

    public override void OnEnable()
    {
        base.OnEnable();

        _eyeTargetRestorePosition = _eyeTarget.control.position;
        // TODO: Find mirrors of all types
        _mirrors = SuperController.singleton.GetAtoms().Where(a => a.type == "Glass").ToList();

        if (_mirrors.Count == 0) enabled = false;
    }

    public override void OnDisable()
    {
        base.OnDisable();

         _eyeTarget.control.position = _eyeTargetRestorePosition;
    }

    public void Update()
    {
        // TODO: Update the eye target position so it's behind the mirror in a straight line, at the same distance behind it. Find the first mirror the model is looking at (closest to lookat ray)
        var eyesCenter = (_lEye.position + _rEye.position) / 2f;
        var mirror = _mirrors[0];
        var mirrorPosition = mirror.mainController.transform.position;
        var reflectPosition = mirrorPosition + (eyesCenter - mirrorPosition); // TODO: Reflect with rotated mirrors: Vector3.Reflect()
        _eyeTarget.control.position = reflectPosition;
    }

    public override void StoreJSON(JSONClass jc)
    {
        base.StoreJSON(jc);
    }

    public override void RestoreFromJSON(JSONClass jc)
    {
        base.RestoreFromJSON(jc);
    }
}