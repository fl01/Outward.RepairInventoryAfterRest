using System.Collections.Generic;
using BepInEx;
using HarmonyLib;

namespace Outward.RepairInventoryAfterRest
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class RepairInventoryAfterRestMod : BaseUnityPlugin
    {
        public const string GUID = "fl01.repair-inventory-after-rest";
        public const string NAME = "Repair inventory after rest";
        public const string VERSION = "1.0.0";

        internal void Awake()
        {
            new Harmony(GUID).PatchAll();
        }

        [HarmonyPatch(typeof(CharacterManager), "UpdateRest")]
        public class CharacterManager_UpdateRest
        {
            [HarmonyPrefix]
            public static void Prefix(CharacterManager __instance)
            {
                if (__instance.m_currentRestingStatus == CharacterManager.RestingStatus.InProgress
                    && NetworkLevelLoader.Instance.IsOverallLoadingDone && NetworkLevelLoader.Instance.ContinueAfterLoading)
                {
                    var character = __instance.GetFirstLocalCharacter();
                    float postRestRealRepairLength = character.CharacterResting.GetPostRestRealRepairLength();
                    var characterRepairEfficiency = (float)character.PlayerStats.RestRepairEfficiency;
                    float repairEfficiency = characterRepairEfficiency * postRestRealRepairLength * 0.005f;
                    var bagItems = character.Inventory?.EquippedBag?.Container?.GetContainedItems() ?? new List<Item>();

                    RepairItems(repairEfficiency, bagItems);
                    var pouchItems = character.Inventory?.m_inventoryPouch?.GetContainedItems() ?? new List<Item>();

                    RepairItems(repairEfficiency, pouchItems);
                }
            }

            private static void RepairItems(float repairEfficiency, List<Item> items)
            {
                foreach (var item in items)
                {
                    if (!item.IsEquippable || !item.RepairedInRest || item.DurabilityRatio <= 0f)
                    {
                        continue;
                    }

                    float ratio = item.DurabilityRatio + repairEfficiency;

                    if (ratio > 1f)
                    {
                        ratio = 1f;
                    }

                    item.SetDurabilityRatio(ratio);
                }
            }
        }
    }
}
