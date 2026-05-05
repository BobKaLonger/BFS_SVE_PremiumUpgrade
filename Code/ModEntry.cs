using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
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
            try
            {
                GameLocation interior = __instance.GetIndoors();
                if (interior == null)
                    return;
                
                switch (__instance.buildingType.Value)
                {
                    case PremiumBarn:
                        HandlePremiumBarn(interior);
                        break;
                    case PremiumCoop:
                        HandlePremiumCoop(interior);
                        break;
                }
            }
            catch (Exception ex)
            {
                Monitor?.Log($"Error in FinishConstruction postfix: {ex}", LogLevel.Error);
            }
        }

        private static void HandlePremiumBarn(GameLocation interior)
        {
            MoveObject(interior, 6, 3, 4, 4);

            MoveBlock(interior, 8, 3, 6, 4, 12, 1);
        }

        private static void HandlePremiumCoop(GameLocation interior)
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
            
            interior.objects.Remove(src);
            interior.objects.Remove(dst);
            interior.objects[dst] = obj;
            obj.TileLocation = dst;
        }

        private static void MoveBlock(GameLocation interior, int srcX, int srcY, int dstX, int dstY, int width, int height)
        {
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                    MoveObject(interior, srcX + dx, srcY + dy, dstX + dx, dstY + dy);
        }
    }
}