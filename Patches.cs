using Harmony;
using System;
using System.Linq;
using UnityEngine;

namespace WildlifeBegone
{
    internal static class Patches
    {
        [HarmonyPatch(typeof(SpawnRegion), "Start", new Type[0])]
        private static class SpawnRegionPatch
        {
            private static void Prefix(SpawnRegion __instance)
            {
                if (__instance.m_StartHasBeenCalled || (GameManager.IsStoryMode() && !Settings.options.enableInStoryMode))
                    return;

                GameObject spawnedObject = __instance.m_SpawnablePrefab;
                BaseAi ai = spawnedObject.GetComponent<BaseAi>();
                if (ai == null)
                    return;

                SpawnRateSetting spawnRates = WildlifeBegone.Config.spawnRates[(int)ai.m_AiSubType];
                AdjustRegion(__instance, spawnRates);
            }

            private static void AdjustRegion(SpawnRegion region, SpawnRateSetting spawnRates)
            {
                float activeMultiplier = spawnRates.SpawnRegionActiveTimeMultiplier;
                float respawnMultiplier = spawnRates.MaximumRespawnsPerDayMultiplier;
                float maximumCountMultiplier = spawnRates.MaximumSpawnedAnimalsMultiplier;

                float oldChanceActive = region.m_ChanceActive;
                region.m_ChanceActive *= activeMultiplier;

                float oldRespawnTime = region.m_MaxRespawnsPerDayStalker;
                region.m_MaxRespawnsPerDayPilgrim *= respawnMultiplier;
                region.m_MaxRespawnsPerDayVoyageur *= respawnMultiplier;
                region.m_MaxRespawnsPerDayStalker *= respawnMultiplier;
                region.m_MaxRespawnsPerDayInterloper *= respawnMultiplier;

                int oldMaximumCountDay = region.m_MaxSimultaneousSpawnsDayStalker;
                int oldMaximumCountNight = region.m_MaxSimultaneousSpawnsNightStalker;
                region.m_MaxSimultaneousSpawnsDayPilgrim = RoundingMultiply(region.m_MaxSimultaneousSpawnsDayPilgrim, maximumCountMultiplier);
                region.m_MaxSimultaneousSpawnsDayVoyageur = RoundingMultiply(region.m_MaxSimultaneousSpawnsDayVoyageur, maximumCountMultiplier);
                region.m_MaxSimultaneousSpawnsDayStalker = RoundingMultiply(region.m_MaxSimultaneousSpawnsDayStalker, maximumCountMultiplier);
                region.m_MaxSimultaneousSpawnsDayInterloper = RoundingMultiply(region.m_MaxSimultaneousSpawnsDayInterloper, maximumCountMultiplier);
                region.m_MaxSimultaneousSpawnsNightPilgrim = RoundingMultiply(region.m_MaxSimultaneousSpawnsNightPilgrim, maximumCountMultiplier);
                region.m_MaxSimultaneousSpawnsNightVoyageur = RoundingMultiply(region.m_MaxSimultaneousSpawnsNightVoyageur, maximumCountMultiplier);
                region.m_MaxSimultaneousSpawnsNightStalker = RoundingMultiply(region.m_MaxSimultaneousSpawnsNightStalker, maximumCountMultiplier);
                region.m_MaxSimultaneousSpawnsNightInterloper = RoundingMultiply(region.m_MaxSimultaneousSpawnsNightInterloper, maximumCountMultiplier);

                if (Settings.options.enableLogging)
                {
                    Debug.Log(string.Format("Adjusted spawner {0}: Active chance {1:F1} -> {2:F1}, respawns / day {3:F2} -> {4:F2}, maximum spawns ({5:D}, {6:D}) -> ({7:D}, {8:D})",
                        region.name,
                        oldChanceActive, region.m_ChanceActive,
                        oldRespawnTime, region.m_MaxRespawnsPerDayStalker,
                        oldMaximumCountDay, oldMaximumCountNight, region.m_MaxSimultaneousSpawnsDayStalker, region.m_MaxSimultaneousSpawnsNightStalker));
                }
            }
        }

        [HarmonyPatch(typeof(RandomSpawnObject), "Start", new Type[0])]
        private static class RandomSpawnObjectPatch
        {
            private static void Prefix(RandomSpawnObject __instance)
            {
                if (!IsSpawnerRSO(__instance))
                    return;
                if (GameManager.IsStoryMode() && !Settings.options.enableInStoryMode)
                    return;

                float oldRerollTime = __instance.m_RerollAfterGameHours;
                int oldMaxObjects = __instance.m_NumObjectsToEnableStalker;

                float maximumCountMultiplier = Settings.options.activeSpawnerCountMultiplier;

                __instance.m_RerollAfterGameHours *= rsoSettings.RerollActiveSpawnersTimeMultiplier;
                __instance.m_NumObjectsToEnablePilgrim = RoundingMultiply(__instance.m_NumObjectsToEnablePilgrim, maximumCountMultiplier);
                __instance.m_NumObjectsToEnableVoyageur = RoundingMultiply(__instance.m_NumObjectsToEnableVoyageur, maximumCountMultiplier);
                __instance.m_NumObjectsToEnableStalker = RoundingMultiply(__instance.m_NumObjectsToEnableStalker, maximumCountMultiplier);
                __instance.m_NumObjectsToEnableInterloper = RoundingMultiply(__instance.m_NumObjectsToEnableInterloper, maximumCountMultiplier);

                if (Settings.options.enableLogging)
                {
                    Debug.Log(string.Format("Adjusted RSO {0}: Reroll time {1:F1} -> {2:F1}, maximum active {3:D} -> {4:D}",
                            __instance.name,
                            oldRerollTime, __instance.m_RerollAfterGameHours,
                            oldMaxObjects, __instance.m_NumObjectsToEnableStalker));
                }
            }

            private static bool IsSpawnerRSO(RandomSpawnObject rso)
            {
                foreach (GameObject go in rso.m_ObjectList)
                {
                    if (go && !go.GetComponent<SpawnRegion>())
                        return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(ConsoleManager), "RegisterCommands", new Type[0])]
        private static class AddConsoleCommands
        {
            private static void Postfix()
            {
                uConsole.RegisterCommand("animals_count", new Action(CountAnimals));
                uConsole.RegisterCommand("animals_kill_all", new Action(KillAllAnimals));
            }

            private const int numAnimalTypes = 6;

            private static void CountAnimals()
            {
                int[] counts = new int[numAnimalTypes];
                BaseAi[] animals = UnityEngine.Object.FindObjectsOfType<BaseAi>();
                foreach (BaseAi animal in animals)
                {
                    if (animal.GetAiMode() != AiMode.Dead)
                    {
                        int ordinal = (int)animal.m_AiSubType;
                        if (ordinal >= numAnimalTypes)
                            ordinal = 0;
                        ++counts[ordinal];
                    }
                }

                object[] args = counts.Cast<object>().ToArray();
                Debug.Log(string.Format("{2} bears, {4} rabbits, {3} deer, {1} wolves, {5} moose, {0} unknown", args));
            }

            private static void KillAllAnimals()
            {
                int[] counts = new int[numAnimalTypes];
                BaseAi[] animals = UnityEngine.Object.FindObjectsOfType<BaseAi>();
                foreach (BaseAi animal in animals)
                {
                    if (animal.GetAiMode() != AiMode.Dead)
                    {
                        animal.SetAiMode(AiMode.Dead);
                        animal.Despawn();

                        int ordinal = (int)animal.m_AiSubType;
                        if (ordinal >= numAnimalTypes)
                            ordinal = 0;
                        ++counts[ordinal];
                    }
                }

                object[] args = counts.Cast<object>().ToArray();
                Debug.Log(string.Format("Killed {2} bears, {4} rabbits, {3} deer, {1} wolves, {5} moose, {0} unknown", args));
            }
        }

        private static int RoundingMultiply(int fieldValue, float multiplier)
        {
            return Math.Max(1, Mathf.RoundToInt(fieldValue * multiplier));
        }
    }
}
