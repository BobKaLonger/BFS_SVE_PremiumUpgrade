using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;

namespace PremiumUpgradeSVE
{
    internal class BuildingPatches
    {
        private const string PremiumBarn = "FlashShifter.StardewValleyExpandedCP_PremiumBarn";
        private const string PremiumCoop = "FlashShifter.StardewValleyExpandedCP_PremiumCoop";

        private static IMonitor? Monitor;

        internal static void Initialize(IMonitor monitor)
        {
            Monitor = monitor;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Building), nameof(Building.FinishConstruction))]
        public static void FinishConstruction_Postfix(Building __instance)
        {
            if (__instance.buildingType.Value is not (PremiumBarn or PremiumCoop))
                return;
            
            string upgradeKey = $"{ModEntry.modInstance!.ModManifest.UniqueID}/buildingKey";
            string currentLevel = __instance.buildingType.Value;

            __instance.modData.TryGetValue(upgradeKey, out string? lastMovedLevel);
            if (lastMovedLevel == currentLevel)
                return;

            ModEntry.modInstance!.Helper.Events.GameLoop.UpdateTicked += DoItemMoves;

            void DoItemMoves(object? sender, UpdateTickedEventArgs e)
            {
                ModEntry.modInstance!.Helper.Events.GameLoop.UpdateTicked -= DoItemMoves;

                GameLocation? interior = __instance.GetIndoors();
                if (interior == null || interior.map == null)
                    return;
                
                switch (currentLevel)
                {
                    case PremiumBarn:
                        HandlePremiumBarn(interior);
                        break;
                    case PremiumCoop:
                        HandlePremiumCoop(interior);
                        break;
                }

                __instance.modData[upgradeKey] = currentLevel;
            }
        }

        internal static void HandlePremiumBarn(GameLocation interior)
        {
            MoveObject(interior, 6, 3, 4, 4);

            MoveBlock(interior, 8, 3, 6, 4, 12, 1);
        }

        internal static void HandlePremiumCoop(GameLocation interior)
        {
            MoveObject(interior, 3, 3, 22, 4);

            MoveObject(interior, 2, 3, 3, 4);
            var incubatorTile = new Vector2(3, 4);
            if (interior.objects.TryGetValue(incubatorTile, out StardewValley.Object incubator)
                && incubator.QualifiedItemId == "(BC)101"
                && incubator.questItem.Value)
            {
                incubator.questItem.Value = false;
            }

            MoveBlock(interior, 6, 3, 5, 4, 12, 1);
        }

        private static void MoveObject(GameLocation interior, int srcX, int srcY, int dstX, int dstY)
        {
            var src = new Vector2(srcX, srcY);
            var dst = new Vector2(dstX, dstY);

            if (!interior.objects.TryGetValue(src, out StardewValley.Object obj))
                return;
            
            if (interior.objects.ContainsKey(dst))
            {
                Game1.player.team.returnedDonations.Add(interior.objects[dst]);
                Game1.player.team.newLostAndFoundItems.Value = true;
                interior.objects.Remove(dst);
            }

            interior.removeObject(src, false);
            obj.TileLocation = dst;
            interior.objects[dst] = obj;
        }

        private static void MoveBlock(GameLocation interior, int srcX, int srcY, int dstX, int dstY, int width, int height)
        {
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                    MoveObject(interior, srcX + dx, srcY + dy, dstX + dx, dstY + dy);
        }
    }
}