using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace NestedAssets.Editor
{
    [CustomPropertyDrawer(typeof(NestedAssetAttribute), false)]
    public class NestedAssetDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, bool> ShowInspectorMap = new();
        
        private Button _addRemoveButton;
        private Button _toggleInspectorButton;
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new PropertyField(property);
            var propId = property.GetStableId();

            var attr = attribute as NestedAssetAttribute;
            var baseType = attr!.Type != null ? attr.Type : fieldInfo.FieldType;
            
            if (!typeof(Object).IsAssignableFrom(baseType))
                return root;

            var typeSelectionMenu = TypeSelectionMenu.Create(baseType, selectedType =>
            {
                property.objectReferenceValue = selectedType.CreateAsset(property.serializedObject.targetObject);
                property.serializedObject.ApplyModifiedProperties();
            });
            
            root.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
            {
                if (root.childCount == 0 || root[0].childCount < 2)
                {
                    Debug.LogWarning($"NestedAssets: seems that {nameof(PropertyField)} have unexpected hierarchy, so can't setup properly");
                    return;
                }
                
                _addRemoveButton = new Button
                {
                    text = GetButtonLabel(property),
                    style =
                    {
                        width = 18f,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        marginBottom = 0f,
                        marginTop = 0f,
                        paddingBottom = 0f,
                        paddingTop = 0f
                    }
                };
                _addRemoveButton.clicked += () => 
                {
                    if (property.objectReferenceValue)
                    {
                        property.RemoveNestedAsset(property.objectReferenceValue);
                        property.objectReferenceValue = null;
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    else
                        typeSelectionMenu.Show(_addRemoveButton.worldBound);
                };
                root.RegisterValueChangeCallback(_ =>
                {
                    _addRemoveButton.text = GetButtonLabel(property);
                    RefreshInspector(property, root.Q<InspectorElement>(), propId);
                    if (_toggleInspectorButton != null)
                        _toggleInspectorButton.style.display = property.objectReferenceValue ? DisplayStyle.Flex : DisplayStyle.None;
                });

                root[0][1].Add(_addRemoveButton);

                if (attr.ShowInspector)
                {
                    ShowInspectorMap.TryAdd(propId, true);
                    ColorUtility.TryParseHtmlString("#8C8C8C", out var borderColor);
                    const float borderWidth = 3f;
                    var inspector = new InspectorElement
                    {
                        name = "inspector",
                        style =
                        {
                            borderLeftWidth = borderWidth,
                            borderLeftColor = borderColor,
                            paddingBottom = 5f,
                            paddingRight = 0f,
                            marginBottom = 10f
                        }
                    };
                    RefreshInspector(property, inspector, propId);
                    root.Add(inspector);
                    
                    _toggleInspectorButton = new Button
                    {
                        style =
                        {
                            backgroundImage = EditorGUIUtility.IconContent("scenevis_visible_hover").image as Texture2D,
                            backgroundSize = new StyleBackgroundSize(new BackgroundSize(12.5f, 12.5f)),
                            width = 18f,
                            marginBottom = 0f,
                            marginTop = 0f,
                            paddingBottom = 0f,
                            paddingTop = 0f,
                            marginLeft = 0,
                            display = property.objectReferenceValue ? DisplayStyle.Flex : DisplayStyle.None
                        },
                        tooltip = "toggle inspector visibility"
                    };
                    _toggleInspectorButton.clicked += () =>
                    {
                        ShowInspectorMap[propId] = !ShowInspectorMap[propId];
                        RefreshInspector(property, inspector, propId);
                    };
                    root[0][1].Add(_toggleInspectorButton);
                }
            });
            
            return root;
        }

        private static string GetButtonLabel(SerializedProperty property) => 
            property.objectReferenceValue ? "x" : "+";

        private static void RefreshInspector(SerializedProperty property, InspectorElement inspector, string propId)
        {
            if (ShowInspectorMap[propId] && property.objectReferenceValue)
            {
                inspector.style.display = DisplayStyle.Flex;
                inspector.Bind(new SerializedObject(property.objectReferenceValue));
            }
            else
            {
                if(!property.objectReferenceValue)
                    inspector.ClearBindings();
                inspector.style.display = DisplayStyle.None;
            }
        }
    }
}