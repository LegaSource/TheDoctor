using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LegaFusionCore.Managers;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TheDoctor.Behaviours.Items;
using TheDoctor.Managers;
using TheDoctor.Patches;
using UnityEngine;

namespace TheDoctor;

[BepInPlugin(modGUID, modName, modVersion)]
public class TheDoctor : BaseUnityPlugin
{
    internal const string modGUID = "Lega.TheDoctor";
    internal const string modName = "The Doctor";
    internal const string modVersion = "1.0.6";

    private readonly Harmony harmony = new Harmony(modGUID);
    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "thedoctor"));
    internal static ManualLogSource mls;
    public static ConfigFile configFile;

    public static GameObject managerPrefab = NetworkPrefabs.CreateNetworkPrefab("TheDoctorNetworkManager");

    // Enemies
    public static EnemyType doctorCorpseEnemy;

    // Items
    public static Item doctorHeart;
    public static Item doctorEye;
    public static Item doctorBrain;

    // Hazards
    public static GameObject doctorClone;

    // Particles
    public static GameObject electroExplosionParticle;

    // Audios
    public static GameObject doctorCloneAudio;

    // Materials
    public static Material inertScreen;
    public static Material scanningScreen;
    public static Material foundScreen;

    public void Awake()
    {
        mls = BepInEx.Logging.Logger.CreateLogSource("TheDoctor");
        configFile = Config;
        ConfigManager.Load();

        LoadManager();
        NetcodePatcher();
        LoadEnemies();
        LoadItems();
        LoadMaterials();
        LoadNetworkPrefabs();

        harmony.PatchAll(typeof(StartOfRoundPatch));
    }

    public static void LoadManager()
    {
        Utilities.FixMixerGroups(managerPrefab);
        _ = managerPrefab.AddComponent<TheDoctorNetworkManager>();
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
        EnemyType doctorBrainEnemy = bundle.LoadAsset<EnemyType>("Assets/DoctorBrainAI/DoctorBrainEnemy.asset");
        doctorBrainEnemy.probabilityCurve = ConfigManager.ParseCurveFromString();
        NetworkPrefabs.RegisterNetworkPrefab(doctorBrainEnemy.enemyPrefab);
        (Dictionary<Levels.LevelTypes, int> spawnRateByLevelType, Dictionary<string, int> spawnRateByCustomLevelType) = ConfigManager.GetEnemiesSpawns();
        Enemies.RegisterEnemy(doctorBrainEnemy,
            spawnRateByLevelType,
            spawnRateByCustomLevelType,
            bundle.LoadAsset<TerminalNode>("Assets/DoctorBrainAI/DoctorBrainTN.asset"),
            bundle.LoadAsset<TerminalKeyword>("Assets/DoctorBrainAI/DoctorBrainTK.asset"));

        doctorCorpseEnemy = bundle.LoadAsset<EnemyType>("Assets/DoctorCorpseAI/DoctorCorpseEnemy.asset");
        NetworkPrefabs.RegisterNetworkPrefab(doctorCorpseEnemy.enemyPrefab);
        Enemies.RegisterEnemy(doctorCorpseEnemy,
            0,
            Levels.LevelTypes.None,
            bundle.LoadAsset<TerminalNode>("Assets/DoctorCorpseAI/DoctorCorpseTN.asset"),
            bundle.LoadAsset<TerminalKeyword>("Assets/DoctorCorpseAI/DoctorCorpseTK.asset"));
    }

    public void LoadItems()
    {
        doctorHeart = LFCObjectsManager.RegisterObject(typeof(DoctorHeart), bundle.LoadAsset<Item>("Assets/DoctorHeart/DoctorHeartItem.asset"));
        doctorEye = LFCObjectsManager.RegisterObject(typeof(DoctorEye), bundle.LoadAsset<Item>("Assets/DoctorEye/DoctorEyeItem.asset"));
        doctorBrain = LFCObjectsManager.RegisterObject(typeof(DoctorBrain), bundle.LoadAsset<Item>("Assets/DoctorBrain/DoctorBrainItem.asset"));
    }

    public static void LoadMaterials()
    {
        inertScreen = bundle.LoadAsset<Material>("Assets/DoctorCorpseAI/Materials/MI_Doctor_Screen_Inert.mat");
        scanningScreen = bundle.LoadAsset<Material>("Assets/DoctorCorpseAI/Materials/MI_Doctor_Screen_Scanning.mat");
        foundScreen = bundle.LoadAsset<Material>("Assets/DoctorCorpseAI/Materials/MI_Doctor_Screen_Found.mat");
    }

    public static void LoadNetworkPrefabs()
    {
        HashSet<GameObject> gameObjects =
        [
            (electroExplosionParticle = bundle.LoadAsset<GameObject>("Assets/Particles/ElectroExplosionParticle.prefab")),
            (doctorClone = bundle.LoadAsset<GameObject>("Assets/DoctorClone/DoctorClone.prefab")),
            (doctorCloneAudio = bundle.LoadAsset<GameObject>("Assets/Audios/Assets/DoctorCloneAudio.prefab"))
        ];

        foreach (GameObject gameObject in gameObjects)
        {
            NetworkPrefabs.RegisterNetworkPrefab(gameObject);
            Utilities.FixMixerGroups(gameObject);
        }
    }
}
