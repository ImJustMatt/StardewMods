## Content Patcher Support

### SpriteSheets

Target Path:

`Mods/furyx639.ExpandedStorage/SpriteSheets/{StorageName}`

Example:

```json
{
  "Format": "1.19.0",
  "Changes": [
    {
      "Action": "EditImage",
      "Target": "Mods/furyx639.ExpandedStorage/SpriteSheets/Large Chest",
      "FromFile": "assets/large-chest.png"
    }
  ]
}
```


### Tab Images

Target Path:

`Mods/furyx639.ExpandedStorage/Tabs/{Mod.UniqueID}/{TabName}`

Use `furyx639.ExpandedStorage` as the Mod.UniqueID to patch the default tab
images available to all content packs

Example:

```json
{
  "Format": "1.19.0",
  "Changes": [
    {
      "Action": "EditImage",
      "Target": "Mods/furyx639.ExpandedStorage/Tabs/furyx639.ExpandedStorage/Crops",
      "FromFile": "assets/crops-tab.png"
    }
  ]
}
```