using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheDoctor.Managers;
using Unity.Netcode;
using UnityEngine;

namespace TheDoctor.Behaviours.Enemies;

public class DoctorBrainAI : EnemyAI
{
    public List<EntranceTeleport> entrances;
    public List<DoctorCorpseAI> corpses = [];

    public float scanTimer;
    public float scanCooldown = 5f;
    public Coroutine scanCoroutine;

    public enum State
    {
        MANAGING
    }

    public override void Start()
    {
        base.Start();

        currentBehaviourStateIndex = (int)State.MANAGING;
        entrances = FindObjectsOfType<EntranceTeleport>().ToList();

        if (IsHost || IsServer) SpawnCorpses();
        // Hack du vaisseau
    }

    private void SpawnCorpses()
    {
        Vector3[] positions = GetCorpsesPositions();
        foreach (Vector3 position in positions)
        {
            GameObject gameObject = Instantiate(TheDoctor.doctorCorpseEnemy.enemyPrefab, position, Quaternion.identity);
            gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);
            corpses.Add(gameObject.GetComponent<DoctorCorpseAI>());
        }
    }

    public Vector3[] GetCorpsesPositions()
    {
        int amount = ConfigManager.amountCorpses.Value;

        List<Vector3> allPositions = allAINodes
            .Select(n => n.transform.position)
            .Where(p => Vector3.Distance(p, transform.position) > 30f)
            .ToList();

        if (allPositions.Count < amount)
        {
            allPositions = allAINodes
                .Select(n => n.transform.position)
                .Where(p => Vector3.Distance(p, transform.position) > 30f)
                .ToList();
        }

        TDUtilities.Shuffle(allPositions);
        return allPositions.Take(amount).ToArray();
    }

    public override void Update()
    {
        base.Update();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        if (scanCoroutine != null) scanTimer = 0f;
        scanTimer += Time.deltaTime;
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.MANAGING:
                DoManaging();
                break;
        }
    }

    public void DoManaging()
    {
        List<DoctorCorpseAI> awakedCorpses = corpses.Where(c => c != null && c.currentBehaviourStateIndex == (int)DoctorCorpseAI.State.CHASING).ToList();
        if (awakedCorpses.Any())
        {
            bool isPlayerEscaped = true;
            foreach (DoctorCorpseAI awakedCorpse in awakedCorpses)
            {
                if (Vector3.Distance(awakedCorpse.transform.position, awakedCorpse.targetPlayer.transform.position) <= 20f)
                {
                    isPlayerEscaped = false;
                    break;
                }
            }

            if (isPlayerEscaped) awakedCorpses.ForEach(c => c.SwitchToBehaviourClientRpc((int)DoctorCorpseAI.State.FREEZING));

            return;
        }

        if (scanTimer > scanCooldown)
        {
            scanTimer = 0f;
            scanCooldown = Random.Range(ConfigManager.scanMinCooldown.Value, ConfigManager.scanMaxCooldown.Value);

            scanCoroutine = StartCoroutine(ScanningCoroutine());
            return;
        }

        if (scanCoroutine == null)
        {
            corpses.Where(c => c != null && c.currentBehaviourStateIndex == (int)DoctorCorpseAI.State.SCANNING)
                .ToList()
                .ForEach(c => c.SwitchToBehaviourClientRpc((int)DoctorCorpseAI.State.FREEZING));
        }
    }

    public IEnumerator ScanningCoroutine()
    {
        TDUtilities.Shuffle(corpses);
        List<DoctorCorpseAI> scanningCorpses = corpses.Where(c => c != null).Take(8).ToList();
        scanningCorpses.ForEach(c => c.SwitchToBehaviourClientRpc((int)DoctorCorpseAI.State.SCANNING));

        float timePassed = 0f;
        PlayerControllerB foundedPlayer = null;
        while (timePassed < ConfigManager.scanDuration.Value)
        {
            foreach (DoctorCorpseAI corpse in scanningCorpses)
            {
                foundedPlayer = FoundClosestPlayerInRange(corpse);
                if (foundedPlayer != null) break;
            }

            yield return new WaitForSeconds(0.2f);
            timePassed += 0.2f;
        }

        foreach (DoctorCorpseAI corpse in scanningCorpses)
        {
            if (foundedPlayer != null)
            {
                corpse.targetPlayer = foundedPlayer;
                corpse.SwitchToBehaviourClientRpc((int)DoctorCorpseAI.State.CHASING);
                continue;
            }
            corpse.SwitchToBehaviourClientRpc((int)DoctorCorpseAI.State.FREEZING);
        }
        scanCoroutine = null;
    }

    public PlayerControllerB FoundClosestPlayerInRange(DoctorCorpseAI corpse)
    {
        PlayerControllerB player = corpse.CheckLineOfSightForPlayer(60f, 20, 20);
        return player != null && corpse.PlayerIsTargetable(player) ? player : null;
    }
}
