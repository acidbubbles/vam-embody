#define POV_DIAGNOSTICS
using UnityEngine;

namespace Acidbubbles.ImprovedPoV
{
    public class HairStrategy : MonoBehaviour
    {
        private Material _material;
        private float _standWidth;

        public void Configure(DAZHairGroup hair)
        {
            // NOTE: Only applies to SimV2 hair
            // TODO: Test without hair
            var hairRender = hair.GetComponentInChildren<MeshRenderer>();
            _material = hairRender.material;
            _standWidth = _material.GetFloat("_StandWidth");
        }

        public void OnDestroy()
        {
            _material = null;
        }

        public void OnWillRenderObject()
        {
            if (Camera.current.name == "MonitorRig")
                _material.SetFloat("_StandWidth", 0f);
        }

        public void OnRenderObject()
        {
            if (Camera.current.name == "MonitorRig")
                _material.SetFloat("_StandWidth", _standWidth);
        }
    }
}