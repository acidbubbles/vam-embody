#define POV_DIAGNOSTICS
using System.Collections.Generic;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV.Hair
{
    public class HairMaterialMirrorStrategy : IMirrorStrategy
    {
        private string _ownerStrategyName;
        private readonly Material _material;
        private readonly float _standWidth;

        string IMirrorStrategy.OwnerStrategyName => _ownerStrategyName;

        public HairMaterialMirrorStrategy(string ownerStrategyName, Material material, float standWidth)
        {
            this._ownerStrategyName = ownerStrategyName;
            _material = material;
            _standWidth = standWidth;
        }

        public static HairMaterialMirrorStrategy FromBroadcastable(string ownerStrategyName, object data)
        {
            var dict = (Dictionary<string, object>)data;
            return new HairMaterialMirrorStrategy(
                ownerStrategyName,
                (Material)dict["material"],
                (float)dict["standWidth"]
                );
        }

        public object ToBroadcastable()
        {
            var dict = new Dictionary<string, object>();
            dict["material"] = _material;
            dict["standWidth"] = _standWidth;
            return dict;
        }

        public bool BeforeMirrorRender()
        {
            if (_material.GetInt("_ImprovedPoVEnabled") != 1) return false;
            _material.SetFloat("_StandWidth", _standWidth);
            return true;
        }

        public void AfterMirrorRender()
        {
            _material.SetFloat("_StandWidth", 0f);
        }
    }
}