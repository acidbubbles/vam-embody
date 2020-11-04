using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Handlers
{
    public class HairHandler : IHandler
    {
        public class MaterialReference
        {
            public Material material;
            public float originalAlphaAdjust;
        }

        private Material _hairMaterial;
        private string _hairShaderProperty;
        private float _hairShaderHiddenValue;
        private float _hairShaderOriginalValue;
        private List<MaterialReference> _materialRefs;

        public int Configure(DAZHairGroup hair)
        {
            if (hair == null || hair.name == "NoHair")
                return HandlerConfigurationResult.CannotApply;

            if (hair.name == "Sim2Hair" || hair.name == "Sim2HairMale" || hair.name == "CustomHairItem")
                return ConfigureSimV2Hair(hair);
            else if (hair.name == "SimHairGroup" || hair.name == "SimHairGroup2")
                return ConfigureSimHair(hair);
            else
                return ConfigureSimpleHair(hair);
        }

        private int ConfigureSimV2Hair(DAZHairGroup hair)
        {
            var materialRefs = new List<MaterialReference>(GetScalpMaterialReferences(hair));
            if (materialRefs.Count != 0) _materialRefs = materialRefs;

            var hairMaterial = hair.GetComponentInChildren<MeshRenderer>()?.material;
            if (hairMaterial == null)
                return HandlerConfigurationResult.TryAgainLater;

            _hairMaterial = hairMaterial;
            _hairShaderProperty = "_StandWidth";
            _hairShaderHiddenValue = 0f;
            _hairShaderOriginalValue = _hairMaterial.GetFloat(_hairShaderProperty);
            return HandlerConfigurationResult.Success;
        }

        private int ConfigureSimHair(DAZHairGroup hair)
        {
            SuperController.LogError("Hair " + hair.name + " is not supported!");
            return HandlerConfigurationResult.CannotApply;
        }

        private int ConfigureSimpleHair(DAZHairGroup hair)
        {
            var materialRefs = hair.GetComponentsInChildren<DAZMesh>()
                .SelectMany(m => m.materials)
                .Distinct()
                .Select(m => new MaterialReference
                {
                    material = m,
                    originalAlphaAdjust = m.GetFloat("_AlphaAdjust")
                })
                .ToList();

            if (materialRefs.Count == 0)
                return HandlerConfigurationResult.TryAgainLater;

            materialRefs.AddRange(GetScalpMaterialReferences(hair));

            _materialRefs = materialRefs;

            return HandlerConfigurationResult.Success;
        }

        private IEnumerable<MaterialReference> GetScalpMaterialReferences(DAZHairGroup hair)
        {
            return hair.GetComponentsInChildren<DAZSkinWrap>()
                .SelectMany(m => m.GPUmaterials)
                .Distinct()
                .Select(m => new MaterialReference
                {
                    material = m,
                    originalAlphaAdjust = m.GetFloat("_AlphaAdjust")
                });
        }

        public void Restore()
        {
            _hairMaterial = null;
            _materialRefs = null;
        }

        public void BeforeRender()
        {
            if (_hairMaterial != null)
                _hairMaterial.SetFloat(_hairShaderProperty, _hairShaderHiddenValue);
            if (_materialRefs != null)
                foreach (var materialRef in _materialRefs)
                    materialRef.material.SetFloat("_AlphaAdjust", -1f);
        }

        public void AfterRender()
        {
            if (_hairMaterial != null)
                _hairMaterial.SetFloat(_hairShaderProperty, _hairShaderOriginalValue);
            if (_materialRefs != null)
                foreach (var materialRef in _materialRefs)
                    materialRef.material.SetFloat("_AlphaAdjust", materialRef.originalAlphaAdjust);
        }
    }
}
