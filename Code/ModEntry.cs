using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace PremiumUpgradeSVE
{
    public class ModEntry : Mod
    {
        internal static ModEntry? modInstance;
        
        private const string PremiumBarn = "FlashShifter.StardewValleyExpandedCP_PremiumBarn";
        private const string PremiumCoop = "FlashShifter.StardewValleyExpandedCP_PremiumCoop";

        public override void Entry(IModHelper helper)
        {
            modInstance = this;
            BuildingPatches.Initialize(Monitor);
            new Harmony(ModManifest.UniqueID).PatchAll();

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        internal void MakeIncubatorsMoveable(AnimalHouse indoors)
        {
            foreach (var pair in indoors.Objects.Pairs)
            {
                var obj = pair.Value;
                if (obj.QualifiedItemId == "(BC)101" && obj.questItem.Value)
                    obj.questItem.Value = false;
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            Utility.ForEachBuilding(building =>
            {
                if (building.indoors.Value is AnimalHouse indoors)
                    MakeIncubatorsMoveable(indoors);
                return true;
            });
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            Utility.ForEachBuilding(building =>
            {
                if (building.buildingType.Value is not (PremiumBarn or PremiumCoop))
                    return true;
                
                if (building.daysUntilUpgrade.Value > 0)
                    return true;
                
                string upgradeKey = $"{ModManifest.UniqueID}/buildingKey";
                string currentLevel = building.buildingType.Value;
                building.modData.TryGetValue(upgradeKey, out string? lastMovedLevel);

                if (lastMovedLevel == currentLevel)
                    return true;
                
                var interior = building.GetIndoors();
                if (interior == null)
                    return true;
                
                interior.reloadMap();
                building.updateInteriorWarps(interior);

                switch (currentLevel)
                {
                    case PremiumBarn:
                        BuildingPatches.HandlePremiumBarn(interior);
                        break;
                    case PremiumCoop:
                        BuildingPatches.HandlePremiumCoop(interior);
                        break;
                }

                building.modData[upgradeKey] = currentLevel;
                return true;
            });
        }
    }
}