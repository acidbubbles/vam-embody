#define POV_DIAGNOSTICS
using System;
using System.Collections.Generic;
using System.Linq;

namespace Acidbubbles.ImprovedPoV
{
    public class MemoizedPerson
    {
        public DAZSkinV2 skin;
        public DAZHairGroup hair;
        public List<MemoizedMaterial> materials;
        private bool _failedOnce;

        public MemoizedPerson()
        {
        }

        public MemoizedPerson(DAZSkinV2 skin, DAZHairGroup hair)
        : this()
        {
            this.skin = skin;
            this.hair = hair;
        }

        public static MemoizedPerson FromBroadcastable(Dictionary<string, object> value)
        {
            if (value == null) return null;
            if (!value.ContainsKey("skin")) return null;
            var deserialized = new MemoizedPerson();
            deserialized.skin = (DAZSkinV2)value["skin"];
            deserialized.hair = (DAZHairGroup)value["hair"];
            deserialized.materials = ((List<List<object>>)value["materials"])?.Select(m => MemoizedMaterial.FromBroadcastable(m)).ToList();
            return deserialized;
        }

        public Dictionary<string, object> ToBroadcastable()
        {
            var serialized = new Dictionary<string, object>();
            serialized["skin"] = skin;
            serialized["hair"] = hair;
            serialized["materials"] = materials?.Select(m => m.ToBroadcastable()).ToList();
            return serialized;
        }

        internal static Dictionary<string, object> EmptyBroadcastable()
        {
            // NOTE: Unity does not allow broadcasting null
            return new Dictionary<string, object>();
        }

        internal void BeforeMirrorRender()
        {
            try
            {
                if (materials != null)
                {
                    foreach (var material in materials)
                    {
                        material.MakeVisible();
                    }
                }
            }
            catch (Exception e)
            {
                if (_failedOnce) return;
                _failedOnce = true;
                SuperController.LogError("Failed to show PoV materials: " + e);
            }
        }

        internal void AfterMirrorRender()
        {
            try
            {
                if (materials != null)
                {
                    foreach (var material in materials)
                    {
                        material.MakeInvisible();
                    }
                }
            }
            catch (Exception e)
            {
                if (_failedOnce) return;
                _failedOnce = true;
                SuperController.LogError("Failed to hide PoV materials: " + e);
            }
        }
    }
}