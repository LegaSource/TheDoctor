using AddonFusion.Registries;
using BepInEx;
using HarmonyLib;
using LegaFusionCore.Managers;
using System;
using System.IO;
using System.Reflection;
using TheDoctor;
using TheDoctorAddon.Behaviours.AddonComponents;
using TheDoctorAddon.Behaviours.AddonProps;
using TheDoctorAddon.Patches;
using UnityEngine;

namespace TheDoctorAddon;

[BepInPlugin(modGUID, modName, modVersion)]
[BepInDependency("Lega.TheDoctor", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("Lega.AddonFusion", BepInDependency.DependencyFlags.HardDependency)]
public class SnowPlaygroundsAddon : BaseUnityPlugin
{
    public const string modGUID = "Lega.TheDoctorAddon";
    public const string modName = "The Doctor Addon";
    public const string modVersion = "1.0.0";

    private readonly Harmony harmony = new Harmony(modGUID);
    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "thedoctoraddon"));

    public void Awake()
    {
        LoadItems();
        harmony.PatchAll(typeof(DoctorBrainPatch));
    }

    public void LoadItems() => RegisterAddon(typeof(SpectralDecoy), Constants.SPECTRAL_DECOY, typeof(SpectralDecoyItem), bundle.LoadAsset<Item>("Assets/AddonProps/SpectralDecoyItem.asset"));

    public void RegisterAddon(Type addonType, string addonName, Type itemType, Item item)
    {
        item = LFCObjectsManager.RegisterObject(itemType, item);
        AddonObjectRegistry.Add(addonType, addonName, item.spawnPrefab);
    }
}

