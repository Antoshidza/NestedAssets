using System;
using UnityEngine;

namespace NestedAssets
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NestedAssetsListAttribute : PropertyAttribute
    {
        public readonly Type Type;
        public readonly bool RemoveCascade;

        public NestedAssetsListAttribute(Type type = null, bool removeCascade = true) : base(applyToCollection: true)
        {
            Type = type;
            RemoveCascade = removeCascade;
        }
    }
}