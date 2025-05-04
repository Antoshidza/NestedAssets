using System;
using UnityEngine;

namespace NestedAssets
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NestedAssetsAttribute : PropertyAttribute
    {
        public readonly Type Type;

        public NestedAssetsAttribute(Type type = null) : base(applyToCollection: true) => Type = type;
    }
}