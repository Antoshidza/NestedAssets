using System;
using UnityEngine;

namespace NestedAssets
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NestedAssetsListAttribute : PropertyAttribute
    {
        public readonly Type Type;

        public NestedAssetsListAttribute(Type type = null) : base(applyToCollection: true) => Type = type;
    }
}