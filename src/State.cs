using System;
using System.Collections.Generic;
using UnityEngine;

namespace Acidbubbles.ImprovedPoV
{
    public class MemoizedMaterial
    {
        private Material material;
        private Shader originalShader;
        private float originalAlphaAdjust;
        private Color originalColor;
        public Color originalSpecColor;
        private Shader replacementShader;
        private float replacementAlphaAdjust;
        private Color replacementColor;
        private Color replacementSpecColor;

        public static MemoizedMaterial FromMaterial(Material material)
        {
            var memoized = new MemoizedMaterial();
            memoized.material = material;
            memoized.originalShader = material.shader;
            memoized.originalAlphaAdjust = material.GetFloat("_AlphaAdjust");
            memoized.originalColor = material.GetColor("_Color");
            memoized.originalSpecColor = material.GetColor("_SpecColor");
            return memoized;
        }

        internal void ApplyReplacementShader(Shader shader, float alphaAdjust, Color color, Color specColor)
        {
            replacementShader = shader;
            replacementAlphaAdjust = alphaAdjust;
            replacementColor = color;
            replacementSpecColor = specColor;

            if (replacementShader != null)
                material.shader = replacementShader;

            MakeInvisible();
        }

        internal void RestoreOriginalShader()
        {
            MakeVisible();
            if (replacementShader != null)
                material.shader = originalShader;
        }

        internal void MakeVisible()
        {
            material.SetFloat("_AlphaAdjust", originalAlphaAdjust);
            material.SetColor("_Color", originalColor);
            material.SetColor("_SpecColor", originalSpecColor);
        }

        internal void MakeInvisible()
        {
            material.SetFloat("_AlphaAdjust", replacementAlphaAdjust);
            material.SetColor("_Color", replacementColor);
            material.SetColor("_SpecColor", replacementSpecColor);
        }
    }

    public class MemoizedPerson
    {
        public List<MemoizedMaterial> materials = new List<MemoizedMaterial>();
    }

    public class State
    {
        public static readonly State current = new State();

        public List<MemoizedPerson> persons;

        public State()
        {
            persons = new List<MemoizedPerson>();
        }

        public void Register(MemoizedPerson person)
        {
            persons.Add(person);
        }

        internal void Unregister(MemoizedPerson person)
        {
            persons.Remove(person);
        }

        internal IEnumerable<MemoizedMaterial> GetAllMaterials()
        {
            foreach (var person in persons)
                foreach (var material in person.materials)
                    yield return material;
        }
    }
}