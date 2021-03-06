using System;
using System.Collections.Generic;

namespace ImJustMatt.Common.Integrations.JsonAssets
{
    public interface IJsonAssetsAPI
    {
        void LoadAssets(string path);

        int GetBigCraftableId(string name);

        //IDictionary<string, int> GetAllObjectIds();
        //IDictionary<string, int> GetAllCropIds();
        //IDictionary<string, int> GetAllFruitTreeIds();
        IDictionary<string, int> GetAllBigCraftableIds();
        //IDictionary<string, int> GetAllHatIds();
        //IDictionary<string, int> GetAllWeaponIds();
        //IDictionary<string, int> GetAllClothingIds();

        //List<string> GetAllObjectsFromContentPack(string cp);
        //List<string> GetAllCropsFromContentPack(string cp);
        //List<string> GetAllFruitTreesFromContentPack(string cp);
        //List<string> GetAllBigCraftablesFromContentPack(string cp);
        //List<string> GetAllHatsFromContentPack(string cp);
        //List<string> GetAllWeaponsFromContentPack(string cp);
        //List<string> GetAllClothingFromContentPack(string cp);
        //List<string> GetAllBootsFromContentPack(string cp);

        //event EventHandler ItemsRegistered;
        event EventHandler IdsAssigned;
        //event EventHandler AddedItemsToShop;
        //event EventHandler IdsFixed;

        //bool FixIdsInItem(Item item);
        //void FixIdsInItemList(List<Item> items);
        //void FixIdsInLocation(GameLocation location);

        //bool TryGetCustomSprite(object entity, out Texture2D texture, out Rectangle sourceRect);
        //bool TryGetCustomSpriteSheet(object entity, out Texture2D texture, out Rectangle sourceRect);
    }
}