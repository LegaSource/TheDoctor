using BepInEx.Configuration;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TheDoctor.Managers;

public class ConfigManager
{
    // GLOBAL
    public static ConfigEntry<string> spawnWeights;
    public static ConfigEntry<int> amountCorpses;
    public static ConfigEntry<float> scanMinCooldown;
    public static ConfigEntry<float> scanMaxCooldown;
    public static ConfigEntry<float> scanDuration;
    // DOCTOR'S HEART
    public static ConfigEntry<int> trackingDuration;

    public static void Load()
    {
        // GLOBAL
        spawnWeights = TheDoctor.configFile.Bind(Constants.GLOBAL, "Spawn weights", "Vanilla:20,Modded:20", $"{Constants.DOCTOR_BRAIN} spawn weights");
        amountCorpses = TheDoctor.configFile.Bind(Constants.GLOBAL, "Amount corpses", 8, $"Amount of {Constants.DOCTOR_CORPSE} that spawn at the same time as the {Constants.DOCTOR_BRAIN}");
        scanMinCooldown = TheDoctor.configFile.Bind(Constants.GLOBAL, "Scan min", 2f, $"Minimum cooldown time between {Constants.DOCTOR_CORPSE} scans");
        scanMaxCooldown = TheDoctor.configFile.Bind(Constants.GLOBAL, "Scan max", 8f, $"Maximum cooldown time between {Constants.DOCTOR_CORPSE} scans");
        scanDuration = TheDoctor.configFile.Bind(Constants.GLOBAL, "Scan duration", 7f, $"Scan duration for the {Constants.DOCTOR_CORPSE}");
        // DOCTOR'S HEART
        trackingDuration = TheDoctor.configFile.Bind(Constants.DOCTOR_HEART, "Tracking duration", 30, $"{Constants.DOCTOR_HEART} tracking duration");
    }

    public static (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) GetEnemiesSpawns()
    {
        Dictionary<Levels.LevelTypes, int> spawnRateByLevelType = [];
        Dictionary<string, int> spawnRateByCustomLevelType = [];
        foreach (string spawnWeight in spawnWeights.Value.Split(',').Select(s => s.Trim()))
        {
            string[] values = spawnWeight.Split(':');
            if (values.Length != 2) continue;

            string name = values[0];
            if (int.TryParse(values[1], out int spawnRate))
            {
                if (Enum.TryParse(name, ignoreCase: true, out Levels.LevelTypes levelType)) spawnRateByLevelType[levelType] = spawnRate;
                else spawnRateByCustomLevelType[name] = spawnRate;
            }
        }
        return (spawnRateByLevelType, spawnRateByCustomLevelType);
    }
}
