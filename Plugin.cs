using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
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

    private static readonly AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "thedoctor"));
    internal static ManualLogSource mls;
    public static ConfigFile configFile;

    // Enemies
    public static EnemyType doctorCorpseEnemy;

    // Items
    public static Item doctorHeart;
    public static Item doctorEye;
    public static Item doctorBrain;

    // Hazards
    public static GameObject doctorClone;

    // Particles
    public static GameObject darkExplosionParticle;
    public static GameObject electroExplosionParticle;

    // Audios
    public static GameObject doctorCloneAudio;

    // Materials
    public static Material inertScreen;
    public static Material scanningScreen;
    public static Material foundScreen;

    // Shaders
    public static Material redShader;
    public static Material yellowShader;

    public void Awake()
    {
        mls = BepInEx.Logging.Logger.CreateLogSource("TheDoctor");
        configFile = Config;
        ConfigManager.Load();

        NetcodePatcher();
        LoadEnemies();
        LoadItems();
        LoadHazards();
        LoadParticles();
        LoadAudios();
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
        EnemyType doctorBrainEnemy = bundle.LoadAsset<EnemyType>("Assets/DoctorBrainAI/DoctorBrainEnemy.asset");
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
        doctorHeart = RegisterItem(typeof(DoctorHeart), bundle.LoadAsset<Item>("Assets/DoctorHeart/DoctorHeartItem.asset"));
        doctorEye = RegisterItem(typeof(DoctorEye), bundle.LoadAsset<Item>("Assets/DoctorEye/DoctorEyeItem.asset"));
        doctorBrain = RegisterItem(typeof(DoctorBrain), bundle.LoadAsset<Item>("Assets/DoctorBrain/DoctorBrainItem.asset"));
    }

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

    public void LoadHazards()
        => doctorClone = RegisterGameObject("Assets/DoctorClone/DoctorClone.prefab");

    public void LoadParticles()
    {
        darkExplosionParticle = RegisterGameObject("Assets/Particles/DarkExplosionParticle.prefab");
        electroExplosionParticle = RegisterGameObject("Assets/Particles/ElectroExplosionParticle.prefab");
    }

    public void LoadAudios()
        => doctorCloneAudio = RegisterGameObject("Assets/Audios/Assets/DoctorCloneAudio.prefab");

    public GameObject RegisterGameObject(string path)
    {
        GameObject gameObject = bundle.LoadAsset<GameObject>(path);
        NetworkPrefabs.RegisterNetworkPrefab(gameObject);
        Utilities.FixMixerGroups(gameObject);
        return gameObject;
    }

    public static void LoadMaterials()
    {
        inertScreen = bundle.LoadAsset<Material>("Assets/DoctorCorpseAI/Materials/MI_Doctor_Screen_Inert.mat");
        scanningScreen = bundle.LoadAsset<Material>("Assets/DoctorCorpseAI/Materials/MI_Doctor_Screen_Scanning.mat");
        foundScreen = bundle.LoadAsset<Material>("Assets/DoctorCorpseAI/Materials/MI_Doctor_Screen_Found.mat");
        redShader = bundle.LoadAsset<Material>("Assets/Shaders/Materials/RedMaterial.mat");
        yellowShader = bundle.LoadAsset<Material>("Assets/Shaders/Materials/YellowMaterial.mat");
    }
}
