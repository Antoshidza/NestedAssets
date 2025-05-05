using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// ReSharper disable PossibleMultipleEnumeration

namespace NestedAssets.Editor
{
    [CustomPropertyDrawer(typeof(NestedAssetsAttribute), false)]
    public class NestedAssetsDrawer : PropertyDrawer
    {
        private ListView _assetsList;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var menu = new GenericMenu();
            var attr = attribute as NestedAssetsAttribute;
            var fieldType = fieldInfo.FieldType;

            if (!fieldType.IsArray && !(fieldType.IsGenericType && typeof(IList).IsAssignableFrom(fieldType)))
            {
                root.Add(new Label
                {
                    text = $"{fieldInfo.Name} [NestedAssets] of type {fieldInfo.FieldType.Name} isn't Array or List<T>.",
                    style = { whiteSpace = WhiteSpace.Normal, marginTop = 10f }
                });
                return root;
            }

            Type elementType = null;
            if (attr!.Type != null) elementType = attr.Type;
            else if (fieldInfo.FieldType.IsArray) elementType = fieldInfo.FieldType.GetElementType();
            else if (fieldInfo.FieldType.IsGenericType && typeof(IList).IsAssignableFrom(fieldInfo.FieldType)) elementType = fieldInfo.FieldType.GetGenericArguments()[0];
            
            if (!typeof(Object).IsAssignableFrom(elementType))
            {
                root.Add(new Label
                {
                    text = $"[NestedAssets] element type should derive from {nameof(Object)}, but type you're use as element type is {elementType!.Name}.",
                    style = { whiteSpace = WhiteSpace.Normal, marginTop = 10f }
                });
                return root;
            }

            foreach (var type in TypeCache.GetTypesDerivedFrom(elementType)
                         .Where(type => type.IsSubclassOf(typeof(ScriptableObject)))) 
                AddTypeToMenu(type);
            if(elementType!.IsClass && !elementType!.IsAbstract)
                AddTypeToMenu(elementType);
            
            _assetsList = new ListView
            {
                bindingPath = property.propertyPath,
                makeItem = () =>
                {
                    var foldout = new Foldout();
                    var inspector = new InspectorElement();
                    foldout.Add(inspector);
                    inspector.style.flexGrow = 1;
                    return foldout;
                },
                bindItem = (visualElement, index) =>
                {
                    var foldout = visualElement as Foldout;
                    var inspector = foldout.Q<InspectorElement>();
                    var elementProp = property.GetArrayElementAtIndex(index);
                    
                    if (elementProp.objectReferenceValue)
                    {
                        foldout!.text = ObjectNames.NicifyVariableName(elementProp.objectReferenceValue.GetType().Name);
                        inspector.Bind(new SerializedObject(elementProp.objectReferenceValue));
                    }
                    else
                        foldout!.text = $"{nameof(SerializedProperty.objectReferenceValue)} is null";
                },
                reorderable = true,
                allowAdd = true,
                allowRemove = true,
                reorderMode = ListViewReorderMode.Animated,
                selectionType = SelectionType.Multiple,
                showBoundCollectionSize = false,
                showBorder = true,
                showAddRemoveFooter = true,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                overridingAddButtonBehavior = (_, _) =>
                {
                    if(menu.GetItemCount() == 0)
                        Debug.LogWarning($"No types derived from {elementType!.Name} were found in project.", property.serializedObject.targetObject);
                    else
                        menu.ShowAsContext();
                },
                headerTitle = property.displayName,
                showFoldoutHeader = true
            };
            _assetsList.itemsChosen += objects =>
            {
                if (objects.Count() == 1)
                    Selection.activeObject = (objects.First() as SerializedProperty)!.boxedValue as Object;
            };
            _assetsList.itemsRemoved += indexes =>
            {
                foreach (var index in indexes) 
                    RemoveCommand(property, index);
            };
            root.Add(_assetsList);

            var syncButton = new Button
            {
                text = "sync",
                style = { alignSelf = Align.Center }
            };
            syncButton.clicked += () => 
            {
                property.ClearArray();
                var mainAsset = property.serializedObject.targetObject;
                var assetPath = AssetDatabase.GetAssetPath(mainAsset);
                foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath)
                             .Where(asset => elementType!.IsAssignableFrom(asset.GetType()))) 
                    AddAssetToArray(property, asset);
            };
            root.Add(syncButton);
            
            return root;
            
            void AddTypeToMenu(Type type) =>
                menu.AddItem(new GUIContent(type.Name), false, () => AddAsset(property, type));
        }
        
        private static void AddAsset(SerializedProperty property, Type assetType)
        {
            var asset = ScriptableObject.CreateInstance(assetType);
            asset.name = assetType.Name;
            
            AssetDatabase.AddObjectToAsset(asset, property.serializedObject.targetObject);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
            
            AddAssetToArray(property, asset);
        }

        private static void AddAssetToArray(SerializedProperty property, Object asset)
        {
            property.InsertArrayElementAtIndex(property.arraySize);
            var insertedElement = property.GetArrayElementAtIndex(property.arraySize - 1);
            insertedElement.objectReferenceValue = asset;
            insertedElement.serializedObject.ApplyModifiedProperties();
        }

        private static void RemoveCommand(SerializedProperty property, int index)
        {
            Undo.DestroyObjectImmediate(property.GetArrayElementAtIndex(index).objectReferenceValue);
            AssetDatabase.SaveAssets();
        }
    }
}