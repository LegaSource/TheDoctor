using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TheDoctor.Behaviours.Items;
using TheDoctor.Managers;
using UnityEngine;

namespace TheDoctor;

[BepInPlugin(modGUID, modName, modVersion)]
public class TheDoctor : BaseUnityPlugin
{
    private const string modGUID = "Lega.TheDoctor";
    private const string modName = "The Doctor";
    private const string modVersion = "1.0.0";

    private readonly Harmony harmony = new Harmony(modGUID);
    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "thedoctor"));
    internal static ManualLogSource mls;
    public static ConfigFile configFile;

    // Enemies
    public static EnemyType doctorCorpseEnemy;

    // Items
    public static Item doctorHeart;

    // Materials
    public static Material inertScreen;
    public static Material scanningScreen;
    public static Material foundScreen;

    public void Awake()
    {
        mls = BepInEx.Logging.Logger.CreateLogSource("TheDoctor");
        configFile = Config;
        ConfigManager.Load();

        NetcodePatcher();
        LoadEnemies();
        LoadItems();
        LoadMaterials();
    }

    private static void NetcodePatcher()
    {
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (Type type in types)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                object[] attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                if (attributes.Length == 0) continue;

                _ = method.Invoke(null, null);
            }
        }
    }

    public static void LoadEnemies()
    {
        EnemyType doctorBrainEnemy = bundle.LoadAsset<EnemyType>("Assets/Brain/DoctorBrainEnemy.asset");
        NetworkPrefabs.RegisterNetworkPrefab(doctorBrainEnemy.enemyPrefab);

        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigManager.GetEnemiesSpawns();
        Enemies.RegisterEnemy(doctorBrainEnemy,
            spawnRateByLevelType,
            spawnRateByCustomLevelType,
            null,//bundle.LoadAsset<TerminalNode>("Assets/Brain/DoctorBrainTN.asset"),
            null);//bundle.LoadAsset<TerminalKeyword>("Assets/Brain/DoctorBrainTK.asset"));

        doctorCorpseEnemy = bundle.LoadAsset<EnemyType>("Assets/Corpse/DoctorCorpseEnemy.asset");
        NetworkPrefabs.RegisterNetworkPrefab(doctorCorpseEnemy.enemyPrefab);
    }

    public void LoadItems() =>
        //tt
        doctorHeart = RegisterItem(typeof(DoctorHeart), bundle.LoadAsset<Item>("Assets/Heart/DoctorHeartItem.asset"));

    public Item RegisterItem(Type type, Item item)
    {
        if (item.spawnPrefab.GetComponent(type) == null)
        {
            PhysicsProp script = item.spawnPrefab.AddComponent(type) as PhysicsProp;
            script.grabbable = true;
            script.grabbableToEnemies = true;
            script.itemProperties = item;
        }

        NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
        Utilities.FixMixerGroups(item.spawnPrefab);
        Items.RegisterItem(item);

        return item;
    }

    public static void LoadMaterials()
    {
        inertScreen = bundle.LoadAsset<Material>("Assets/Corpse/Materials/MI_Doctor_Screen_Inert.mat");
        scanningScreen = bundle.LoadAsset<Material>("Assets/Corpse/Materials/MI_Doctor_Screen_Scanning.mat");
        foundScreen = bundle.LoadAsset<Material>("Assets/Corpse/Materials/MI_Doctor_Screen_Found.mat");
    }
}
