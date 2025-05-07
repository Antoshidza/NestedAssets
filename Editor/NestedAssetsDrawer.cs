using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
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
            var attr = attribute as NestedAssetsAttribute;
            var fieldType = fieldInfo.FieldType;

            #region validate field

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

            #endregion

            #region create type selection menu

            var types = TypeCache.GetTypesDerivedFrom(elementType).Where(type => type.IsSubclassOf(typeof(ScriptableObject)));
            if (elementType!.IsClass && !elementType!.IsAbstract)
                types = types.Append(elementType);
            var menuAdv = new TypeSelectionMenu(new AdvancedDropdownState(), types) { MinimumSize = new Vector2(250f, 0f) };
            menuAdv.TypeSelected += type => AddAsset(property, type);

            #endregion

            #region create list view

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
                overridingAddButtonBehavior = (_, button) => menuAdv.Show(button.worldBound),
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
            _assetsList.RegisterCallback<ContextualMenuPopulateEvent>(evt => 
                evt.menu.AppendAction("Synchronize", _ => 
                {
                    if(!EditorUtility.DisplayDialog("Synchronize assets with lists?","Are you sure you want synchronize list with assets?", "Synchronize!", 
                           "Cancel")) return;
                    
                    property.ClearArray();
                    foreach (var asset in AssetDatabase
                                 .LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(property.serializedObject.targetObject))
                                 .Where(asset => elementType!.IsAssignableFrom(asset.GetType()))) 
                        AddAssetToArray(property, asset);
                }));

            #endregion
            
            root.Add(_assetsList);
            
            return root;
        }

        #region add / remove asset methods

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
            var objectRef = property.GetArrayElementAtIndex(index).objectReferenceValue;
            if(!objectRef
                || !AssetDatabase.IsSubAsset(objectRef) 
                || !AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(property.serializedObject.targetObject)).Contains(objectRef))
                return;
            Undo.DestroyObjectImmediate(objectRef);
            AssetDatabase.SaveAssets();
        }
        
        #endregion
    }
}
