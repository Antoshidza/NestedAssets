using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NestedAssets.Editor
{
    public static class NestedAssets
    {
        public static Object CreateAsset(this Type type, Object parent)
        {
            var asset = ScriptableObject.CreateInstance(type);
            asset.name = type.Name;
            
            AssetDatabase.AddObjectToAsset(asset, parent);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(parent);

            return asset;
        }

        public static void RemoveNestedAsset(this SerializedProperty property, Object asset)
        {
            if(!asset
               || !AssetDatabase.IsSubAsset(asset) 
               || !AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(property.serializedObject.targetObject)).Contains(asset))
                return;
            Undo.DestroyObjectImmediate(asset);
            AssetDatabase.SaveAssets();
        }
        
        public static string GetStableId(this SerializedProperty property)
        {
            var assetGuid = string.Empty;
            var assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
            if (!string.IsNullOrEmpty(assetPath)) 
                assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            
            return $"{assetGuid}:{property.propertyPath}";
        }
    }
}