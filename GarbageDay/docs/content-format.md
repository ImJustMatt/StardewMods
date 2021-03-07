## Garbage Day Content Format

### Contents

* [Overview](#overview)
* [Maps](#maps)
* [Global Loot](#global-loot)
* [Local Loot](#local-loot)

### Overview

A Garbage Day content pack must include the following files:

- `manifest.json`
- `garbage-day.json`

#### Manifest.json

`manifest.json` must specify this is a content pack for Garbage Day:

```json
"ContentPackFor": {
  "UniqueID": "furyx639.GarbageDay"
}
```

For full details of `manifest.json` refer to
[Modding:Modder Guide/APIs/Manifest](https://stardewcommunitywiki.com/Modding:Modder_Guide/APIs/Manifest).

#### Garbage-Day.json

`garbage-day.json` is used to whitelist maps and add loot:

```json
{
  "Maps": [
    "Maps\\ModdedMap",
    "Maps\\OtherMap"
  ],
  "GlobalLoot": {
    "item_trash": 1,
    "item_joja_cola": 1
  },
  "LocalLoot": {
    "MuseumCan": {
      "category_artifact": 0.005
    }
  }
}
```

#### Maps

Maps are a list of paths to the modded map from the Game Content folder.  
This path should match the "Target" path for maps added by [Content Patcher](https://github.com/Pathoschild/StardewMods/blob/stable/ContentPatcher/docs/author-guide.md#readme).

#### Global Loot

Global Loot are items that can have a chance to be added to every Garbage Can
at the start of each morning.

Example:

```json
{
  "color_red": 1,
  "color_blue": 2
}
```

Each loot item is defined by a [Context Tag](https://github.com/ImJustMatt/StardewMods/blob/master/ExpandedStorage/docs/content-format.md#context-tags)
and their chance is weighed against other loot in the same loot table.

From the example above, there is double the chance that a blue item is selected over a red item.

#### Local Loot

Local loot follows the same format as [Global Loot](#global-loot) with the one
difference being they will only be added to the loot table for a specific
Garbage Can based on the Unique ID from the Map Tile Property of
`"Action": "Garbage {Unique ID}"` on the `"Buildings"` layer.

Example:

```json
{
  "SmoothieShop": {
    "item_pineapple": 1
  },
  "LumberShop": {
    "item_wood": 1,
    "item_hardwood": 0.1
  }
}
```

In this example, the modded map should have a tile where the Garbage is placed
with a matching Tile Property for this loot table:

`"Action": "Garbage SmoothieShop""`