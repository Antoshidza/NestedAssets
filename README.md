# Nested Assets

Allow you to decorate any array or list of `ScriptableObject` derived type field with `[NestedAssets]` attribute to let you add / remove / edit objects (which
are assignable from this type) directly from this field owner inspector. Added objects created nested to field owner object, removed objects automatically 
destroyed.

## :computer: Getting started
### Install package via git url
```
https://github.com/Antoshidza/NestedAssets.git
```

### Usage
Let's say you have your
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

> :bulb: **Field you use `[NestedAssets]` on should be an array or list of type derived from `ScriptableObject`.
> Field itself should be able to be serialized by unity**

> :bulb: Use `[NestedAssets]` for cases where you want to have your object instances per parent object instead of sharing. Like each `Effect` on `Skill` created again for each `Skill` asset instead of being created once and used on multiple skills. Though it is possible to implement for this package.

> :bulb: in `[NestedAsset]` you can specify type you want to use as base for nested assets like this `[NestedAssets(typeof(Effect))]`. It may help if you want to make type selection more concrete (though still can be made just with changing field type)

> :bulb: You can use "Synchronize" option in list context menu to synchronize all nested objects of target type with list view. Please note that in case where you have 
> multiple lists of the same type with [NestedAssets] "Synchronize" logic can't differ what asset belongs what list.

> :exclamation: If you have any deep hierarchy of same types, for example your `Effect` can have another effect with `[NestedAsset]` attribute then be careful due to
> unity can only have one level of nesting assets, so all your `Effect` assets will appear under main asset despite it's place in hierarchy, in that case using 
> "Synchronize" will invalidate your setup or even can produce infinite loops.

## :monocle_face: Why use this?
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

## Support :+1: Contribute :computer: Contact :speech_balloon:
I wish this project will be helpful for your project! So feel free to send bug reports / pull requests, start discussions / critique, those all are **highly** appreciated!
You can reach me [discord](https://www.discordapp.com/users/219868910223228929)!

If you want to support me, you can use this

![image](https://github.com/user-attachments/assets/b9fb3f56-8678-494e-980f-4d8d80c7d865)
