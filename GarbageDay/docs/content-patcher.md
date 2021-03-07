## Integration using Content Patcher

You must edit or add a map to add Garbage Cans to them. The mod automatically
removes garbage cans on the Town TileSheet from the Buildings and Front layers.

Where a storage Garbage Can is placed will be based on the Tile Property of
`"Action": "Garbage {UniqueID}"` from the `"Buildings"` layer, where
`{UniqueID}` is a name that uniquely identifies the object. This is used later
to customize the garbage can's loot table.

### Maps

By default, Garbage Day will only edit the `Maps\Town`. In order for it to scan
other maps you must add a `yourmod.json` file into Garbage Day's `maps` folder.

Example:

```json
[
  "Maps\\ModdedMap",
  "Maps\\AnotherMap",
]
```

### SpriteSheets

The SpriteSheet can be patched in the [Expanded Storage](https://github.com/ImJustMatt/StardewMods/blob/master/ExpandedStorage/docs/content-patcher.md)
supported format.

Target Path:

`Mods/furyx639.ExpandedStorage/SpriteSheets/Garbage Can`

Example:

```json
{
  "Format": "1.20.0",
  "Changes": [
    {
      "Action": "EditImage",
      "Target": "Mods/furyx639.ExpandedStorage/SpriteSheets/Garbage Can",
      "FromFile": "assets/garbage-can.png"
    }
  ]
}
```

### Loot Tables

Target Path:

`Mods/furyx639.GarbageDay/GlobalLoot`  
`Mods/furyx639.GarbageDay/Loot/{UniqueID}`

Example:

```json
{
  "Format": "1.20.0",
  "Changes": [
    {
      "Action": "Include",
      "FromFile": "assets/custom-loot.json"
    }
  ]
}
```

Sample Loot File:

```json
{
  "item_trash": 1,
  "category_artifact": 0.005
}
```

The loot file specifies items by their [Context Tag](https://github.com/ImJustMatt/StardewMods/blob/master/ExpandedStorage/docs/content-format.md#context-tags)
and their weighted probability that they get added to the Trash every day.