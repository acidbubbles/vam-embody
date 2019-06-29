#define POV_DIAGNOSTICS
using UnityEngine;

namespace Acidbubbles.ImprovedPoV.Hair
{
    public class HairMaterialStrategy : IStrategy
    {
        public const string Name = "Hair Material (allows mirrors)";

        string IStrategy.Name
        {
            get { return Name; }
        }

        private Material _material;
        private float _standWidth;

        public void Apply(PersonReference person)
        {
            if(_material != null) return;

            // NOTE: Only applies to SimV2 hair
            // TODO: Test without hair
            var hairRender = person.hair.GetComponentInChildren<MeshRenderer>();
            _material = hairRender.material;
            _standWidth = _material.GetFloat("_StandWidth");
            _material.SetFloat("_StandWidth", 0f);
            _material.SetInt("_ImprovedPoVEnabled", 1);

            person.hairStrategy = new HairMaterialMirrorStrategy(Name, _material, _standWidth);
        }

        public void Restore()
        {
            _material.SetFloat("_StandWidth", _standWidth);
            _material.SetInt("_ImprovedPoVEnabled", 0);
            _material = null;
        }

        public IMirrorStrategy GetMirrorStrategy(object data)
        {
            return HairMaterialMirrorStrategy.FromBroadcastable(Name, data);
        }
    }
}