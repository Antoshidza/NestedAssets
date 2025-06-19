#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NestedAssets
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

        public static void RemoveNestedAsset(this Object parent, Object asset)
        {
            if(!asset
               || !AssetDatabase.IsSubAsset(asset) 
               || !AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(parent)).Contains(asset))
                return;
            RemoveCascade(asset);
            Undo.DestroyObjectImmediate(asset);
            AssetDatabase.SaveAssets();
        }

        public static void RemoveNestedAsset(this SerializedProperty property, Object asset) => 
            property.serializedObject.targetObject.RemoveNestedAsset(asset);

        public static string GetStableId(this SerializedProperty property)
        {
            var assetGuid = string.Empty;
            var assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
            if (!string.IsNullOrEmpty(assetPath)) 
                assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            
            return $"{assetGuid}:{property.propertyPath}";
        }
        
        private static void RemoveCascade(Object deletingObject)
        {
            if(!deletingObject) return;

            var fields = deletingObject.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => 
                    (Attribute.IsDefined(field, typeof(NestedAssetAttribute)) && field.GetCustomAttribute<NestedAssetAttribute>().RemoveCascade)
                    || (Attribute.IsDefined(field, typeof(NestedAssetsListAttribute)) && field.GetCustomAttribute<NestedAssetsListAttribute>().RemoveCascade));
            foreach (var fieldInfo in fields)
            {
                var refValue = fieldInfo.GetValue(deletingObject);
                if(refValue == null) continue;
                var type = refValue.GetType(); 
                if (type.IsArray)
                {
                    if(!typeof(Object).IsAssignableFrom(type.GetElementType()))
                    {
                        Debug.LogWarning($"Deleting object {deletingObject.name} contains array field {fieldInfo.FieldType.Name} {fieldInfo.Name} with one of the NestedAssets attribute but element type {type.GetElementType()} isn't derived from {nameof(Object)} ");
                        continue;
                    }
                    foreach (var element in (refValue as IEnumerable)!.Cast<Object>().Where(element => element)) 
                        deletingObject.RemoveNestedAsset(element);
                }
                else
                {
                    if (!typeof(Object).IsAssignableFrom(type))
                    {
                        Debug.LogWarning($"Deleting object {deletingObject.name} contains field {fieldInfo.Name} with one of the NestedAssets attribute but it's type isn't derived from {nameof(Object)} ({type.GetElementType()})");
                        continue;
                    }
                    deletingObject.RemoveNestedAsset(refValue as Object);
                }
            }
        }
    }
}
#endif
