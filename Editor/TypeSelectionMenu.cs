﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace NestedAssets.Editor
{
    public class TypeSelectionMenu : AdvancedDropdown
    {
        private readonly IEnumerable<Type> _types;
        private readonly Dictionary<int, Type> _typeMap = new();

        public int ItemsCount { get; private set; }
        public event Action<Type> TypeSelected;

        public Vector2 MinimumSize
        {
            get => minimumSize;
            set => minimumSize = value;
        }

        public TypeSelectionMenu(AdvancedDropdownState state, IEnumerable<Type> types) : base(state) => 
            _types = types;

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select Type");
            foreach (var type in _types.OrderBy(type => type.Name[0]))
            {
                var item = new AdvancedDropdownItem(type.Name);
                _typeMap[item.id] = type;
                root.AddChild(item);
                ItemsCount++;
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            TypeSelected?.Invoke(_typeMap[item.id]);
            base.ItemSelected(item);
        }

        public static TypeSelectionMenu Create(Type baseType, Action<Type> onTypeSelected)
        {
            var types = TypeCache.GetTypesDerivedFrom(baseType).Where(type => type.IsSubclassOf(typeof(ScriptableObject)));
            if (baseType!.IsClass && !baseType!.IsAbstract)
                types = types.Append(baseType);
            var menu = new TypeSelectionMenu(new AdvancedDropdownState(), types) { MinimumSize = new Vector2(250f, 0f) };
            menu.TypeSelected += onTypeSelected;
            return menu;
        }
    }
}