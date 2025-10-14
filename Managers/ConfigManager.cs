using BepInEx.Configuration;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheDoctor.Managers;

public class ConfigManager
{
    // GLOBAL
    public static ConfigEntry<string> spawnWeights;
    public static ConfigEntry<string> probabilityCurve;
    public static ConfigEntry<int> amountCorpses;
    public static ConfigEntry<float> scanMinCooldown;
    public static ConfigEntry<float> scanMaxCooldown;
    public static ConfigEntry<bool> isShipDialogue;
    public static ConfigEntry<bool> isInsideDialogue;
    // DOCTOR'S CORPSE
    public static ConfigEntry<float> scanDuration;
    public static ConfigEntry<float> corpseSpeed;
    // DOCTOR'S HEART
    public static ConfigEntry<int> heartTrackingDuration;
    public static ConfigEntry<int> heartMinValue;
    public static ConfigEntry<int> heartMaxValue;
    // DOCTOR'S EYE
    public static ConfigEntry<int> eyeTrackingDuration;
    public static ConfigEntry<int> eyeMinValue;
    public static ConfigEntry<int> eyeMaxValue;
    // DOCTOR'S BRAIN
    public static ConfigEntry<bool> brainSpecialAbility;
    public static ConfigEntry<int> brainMinValue;
    public static ConfigEntry<int> brainMaxValue;
    // SPECTRAL DECOY
    public static ConfigEntry<int> spectralDecoyCooldown;
    public static ConfigEntry<int> spectralDecoyDamage;

    public static void Load()
    {
        // GLOBAL
        spawnWeights = TheDoctor.configFile.Bind(Constants.GLOBAL, "Spawn weights", "Vanilla:20,Modded:20", $"{Constants.BRAIN} enemy spawn weights");
        probabilityCurve = TheDoctor.configFile.Bind(Constants.GLOBAL, "Probability curve", "0,1;0.25,1;0.3,0;1,0", $"{Constants.BRAIN} probability curve (format : time,value;time,value;...)");
        amountCorpses = TheDoctor.configFile.Bind(Constants.GLOBAL, "Amount corpses", 8, $"Amount of {Constants.CORPSE} that spawn at the same time as the {Constants.BRAIN}");
        scanMinCooldown = TheDoctor.configFile.Bind(Constants.GLOBAL, "Scan min", 2f, $"Minimum cooldown duration between {Constants.CORPSE} scans");
        scanMaxCooldown = TheDoctor.configFile.Bind(Constants.GLOBAL, "Scan max", 8f, $"Maximum cooldown duration between {Constants.CORPSE} scans");
        isShipDialogue = TheDoctor.configFile.Bind(Constants.GLOBAL, "Ship dialogue", true, $"Trigger the ship dialogue when the {Constants.BRAIN} spawns");
        isInsideDialogue = TheDoctor.configFile.Bind(Constants.GLOBAL, "Inside dialogue", true, $"Trigger the inside dialogue when the {Constants.BRAIN} spawns");
        // DOCTOR'S CORPSE
        scanDuration = TheDoctor.configFile.Bind(Constants.CORPSE, "Scan duration", 7f, $"Scan duration for the {Constants.CORPSE}");
        corpseSpeed = TheDoctor.configFile.Bind(Constants.CORPSE, "Corpse speed", 5f, $"{Constants.CORPSE} speed");
        // DOCTOR'S HEART
        heartTrackingDuration = TheDoctor.configFile.Bind(Constants.HEART, "Tracking duration", 45, $"{Constants.HEART} tracking duration");
        heartMinValue = TheDoctor.configFile.Bind(Constants.HEART, "Min value", 15, $"{Constants.HEART} min value");
        heartMaxValue = TheDoctor.configFile.Bind(Constants.HEART, "Max value", 30, $"{Constants.HEART} max value");
        // DOCTOR'S EYE
        eyeTrackingDuration = TheDoctor.configFile.Bind(Constants.EYE, "Tracking duration", 45, $"{Constants.EYE} tracking duration");
        eyeMinValue = TheDoctor.configFile.Bind(Constants.EYE, "Min value", 15, $"{Constants.EYE} min value");
        eyeMaxValue = TheDoctor.configFile.Bind(Constants.EYE, "Max value", 30, $"{Constants.EYE} max value");
        // DOCTOR'S BRAIN
        brainSpecialAbility = TheDoctor.configFile.Bind(Constants.BRAIN, "Enable special ability", true, $"Enable {Constants.BRAIN} item's special ability ({Constants.SPECTRAL_DECOY})?");
        brainMinValue = TheDoctor.configFile.Bind(Constants.BRAIN, "Min value", 50, $"{Constants.BRAIN} min value");
        brainMaxValue = TheDoctor.configFile.Bind(Constants.BRAIN, "Max value", 100, $"{Constants.BRAIN} max value");
        // SPECTRAL DECOY
        spectralDecoyCooldown = TheDoctor.configFile.Bind(Constants.SPECTRAL_DECOY, "Cooldown", 120, $"Cooldown duration of the {Constants.SPECTRAL_DECOY}");
        spectralDecoyDamage = TheDoctor.configFile.Bind(Constants.SPECTRAL_DECOY, "Damage", 2, $"Damages inflicted by the {Constants.SPECTRAL_DECOY}");
    }

    public static AnimationCurve ParseCurveFromString()
    {
        if (string.IsNullOrWhiteSpace(probabilityCurve.Value))
            return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

        string[] keyPairs = probabilityCurve.Value.Split(';');
        List<Keyframe> keys = [];

        foreach (string pair in keyPairs)
        {
            string[] parts = pair.Split(',');
            if (parts.Length != 2) continue;

            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float time)
                && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float value))
            {
                keys.Add(new Keyframe(time, value));
            }
        }

        return keys.Count == 0 ? new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f)) : new AnimationCurve(keys.ToArray());
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
