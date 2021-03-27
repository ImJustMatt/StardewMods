## Integration using Content Patcher

You must edit or add a map to add Garbage Cans to them. The mod automatically
removes garbage cans on the Town TileSheet from the Buildings and Front layers.

Where a storage Garbage Can is placed will be based on the Tile Property of
`"Action": "Garbage {UniqueID}"` from the `"Buildings"` layer, where
`{UniqueID}` is a name that uniquely identifies the garbage can. This is used
later to customize the garbage can's loot table.

### Maps

By default, Garbage Day will only edit the `Maps\Town`. In order for it to scan
other maps you must add a `GarbageDay` Custom Property to your map.

### Loot

See Content Patcher's [Author Guide](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide.md#data-edit-data-model-assets)
for documentation on editing GarbageDay's data model to add/remove/edit Loot.

Target Path:

`Mods/GarbageDay/Loot`

Example:

```json
{
  "Format": "1.20.0",
  "Changes": [
    {
      "Action": "EditData",
      "Target": "Mods/GarbageDay/Loot/Global",
      "Entries": {
        "item_joja_cola": null,
        "item_broken_cd": null,
        "item_broken_glasses": null
      }
    },
    {
      "Action": "EditData",
      "Target": "Mods/GarbageDay/Loot/UniqueGarbageCanID",
      "Entries": {
        "category_artifact": 1
      }
    }
  ]
}
```

This example removes three items from the Global Loot Table, and adds an
item to a Garbage Can whose ID is `"UniqueGarbageCanID"`.

The loot file specifies items by their [Context Tag](https://github.com/ImJustMatt/StardewMods/blob/master/ExpandedStorage/docs/content-format.md#context-tags)
and their weighted probability that they get added to the Trash every day.

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