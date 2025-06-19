using System;
using UnityEngine;

namespace NestedAssets
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NestedAssetAttribute : PropertyAttribute
    {
        public readonly Type Type;
        public readonly bool ShowInspector;
        public readonly bool RemoveCascade;

        public NestedAssetAttribute(Type type = null, bool showInspector = true, bool removeCascade = true)
        {
            Type = type;
            ShowInspector = showInspector;
            RemoveCascade = removeCascade;
        }
    }
}