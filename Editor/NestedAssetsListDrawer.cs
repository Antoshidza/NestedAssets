using System;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// ReSharper disable PossibleMultipleEnumeration

namespace NestedAssets.Editor
{
    [CustomPropertyDrawer(typeof(NestedAssetsListAttribute), false)]
    public class NestedAssetsListDrawer : PropertyDrawer
    {
        private ListView _assetsList;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var attr = attribute as NestedAssetsListAttribute;
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

            var typeSelectionMenu = TypeSelectionMenu.Create(elementType, type => AddAsset(property, type));

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
                overridingAddButtonBehavior = (_, button) => typeSelectionMenu.Show(button.worldBound),
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
                    RemoveAssetAt(property, index);
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

        private static void AddAsset(SerializedProperty property, Type assetType) => 
            AddAssetToArray(property, assetType.CreateAsset(property.serializedObject.targetObject));

        private static void AddAssetToArray(SerializedProperty property, Object asset)
        {
            property.InsertArrayElementAtIndex(property.arraySize);
            var insertedElement = property.GetArrayElementAtIndex(property.arraySize - 1);
            insertedElement.objectReferenceValue = asset;
            insertedElement.serializedObject.ApplyModifiedProperties();
        }

        private static void RemoveAssetAt(SerializedProperty property, int index) => 
            property.RemoveNestedAsset(property.GetArrayElementAtIndex(index).objectReferenceValue);

        #endregion
    }
}
