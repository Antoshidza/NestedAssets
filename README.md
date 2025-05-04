# Nested Assets

Allow you to decorate any array or list of `ScriptableObject` derived type field with `[NestedAssets]` attribute to let you add / remove / edit objects (which
are assignable from this type) directly from this field owner inspector. Added objects created nested to field owner object, removed objects automatically 
destroyed.

## Getting started
### Install this package via git url
```
https://github.com/Antoshidza/NestedAssets.git
```

### Usage
Let say you have your
```csharp
public class Skill : ScriptableObject
{
    [SerializedField, NestedAssets] private Effect[] _effects;
}

public abstract Effect : ScriptableObject { /* base impl */ }

public class Damage : Effect 
{
    [SerializedField] private float _value;
}

public class MoveSpeedMuliplier : Effect 
{
    [SerializedField] private float _value;
}
```
Having `[NestedAssets]` on `_effects` field makes it appear in inspector like this:

![image](https://github.com/user-attachments/assets/45f7c398-4a0f-4a3c-90a3-f1d5dcae2211)

![image](https://github.com/user-attachments/assets/ea4c0fc2-7a59-4d83-aaf1-165e2689926d)

> **Field you use `[NestedAssets]` on should be an array or list of type derived from `ScriptableObject`. 
> Field itself should be able to be serialized by unity**

> You can use "sync" button to synchronize all nested objects of target type with list view. Please note that in case where you have multiple lists of the same type with [NestedAssets] "sync" button logic can't differ what asset belongs what list.

## Why use this?
While unity pushes us to work with project configuration from editor rather than from code you have probably faced the situation when you want to have 
polymorphism in inspector. You want to assign objects derived from same type but have not the same type themselves. One of the possible solutions could be...

### `[SerializeReference]` attribute
From some point in unity history `[SerializeReference]` [attribute](https://docs.unity3d.com/6000.1/Documentation/ScriptReference/SerializeReference.html) 
appears. So we can store different object assignable to field type by reference. However, unity doesn't provide any auto add / remove / edit UI for fields 
with `[SeralizeReference]` attribute and just skip those fields from inspection.

There are multiple free solutions from devs which use `[SerializeReference]` and bring UI for that. I personally use 
[SerializeReferenceExtensions](https://github.com/mackysoft/Unity-SerializeReferenceExtensions) in my projects.

However, for me, `[SerializeReference]` based solutions have one major drawback: when use this attribute unity serialize your object with type full  name.
It looks like `type: {class: TypeName, ns: Namespace, asm: AssemblyName}`. As you probably already have experience with or can guess when you change the name
of type, namespace or assembly unity will lose it and won't be able to automatically patch. So every time you want to rename something you will manually patch 
your files.

### Having polymorphism as `ScriptableObjects`
You can have your `ScriptableObject` derived base `class`, have multiple types derived from this base class and create them separately and manually. 
The obvious drawback is that you do all manually: create then add asset to list, remove from list then not forget to destroy asset, to inspect or edit asset
you need to open its own inspector, and you can't see all picture at the same time.

### But wait, I've seen unity have polymorphism without any of that
If you work with unity enough time you've probably seen cases where unity gives you polymorphism. For example `RenderFeature`s in URP assets or modificators
in postprocessing volumes profiles. You also could notice that when you add (for example again) render feature it appears as separate `ScriptableObject` of
some concrete type but attached to parent object (like foldout list) and also being able to be edited from main object inspector.

**This is what Nested Assets exactly do.**
