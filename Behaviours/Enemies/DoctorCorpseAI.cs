using GameNetcodeStuff;
using System;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace TheDoctor.Behaviours.Enemies;

public class DoctorCorpseAI : EnemyAI
{
    public AudioClip[] MoveSounds = Array.Empty<AudioClip>();
    public float moveTimer = 0f;

    public Transform TurnCompass;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Collider corpseCollider;
    public Light scanLight;

    public int amountHit = 0;

    public Coroutine attackCoroutine;
    public Coroutine hitEnemyCoroutine;

    public enum State
    {
        FREEZING,
        SCANNING,
        CHASING
    }

    public override void Start()
    {
        base.Start();

        currentBehaviourStateIndex = (int)State.FREEZING;
        creatureAnimator.SetTrigger("startRun");

        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null) TheDoctor.mls.LogError("SkinnedMeshRenderer not found for the DoctorCorpse");

        Transform headBone = skinnedMeshRenderer.bones.FirstOrDefault(b => b.name.Equals("PHYSICS_Head"));
        if (headBone != null)
        {
            scanLight.transform.SetParent(headBone, worldPositionStays: false);
            scanLight.transform.localPosition = Vector3.zero;
            scanLight.transform.rotation = headBone.rotation;
            scanLight.gameObject.SetActive(false);
        }
    }

    public override void Update()
    {
        base.Update();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        PlayMoveSound();
        if (targetPlayer != null && currentBehaviourStateIndex == (int)State.CHASING)
        {
            TurnCompass.LookAt(targetPlayer.gameplayCamera.transform.position);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(new Vector3(0f, TurnCompass.eulerAngles.y, 0f)), 4f * Time.deltaTime);
        }
    }

    public void PlayMoveSound()
    {
        if (currentBehaviourStateIndex != (int)State.CHASING) return;

        moveTimer -= Time.deltaTime;
        if (MoveSounds.Length > 0 && moveTimer <= 0)
        {
            creatureSFX.PlayOneShot(MoveSounds[UnityEngine.Random.Range(0, MoveSounds.Length)]);
            moveTimer = 0.5f;
        }
    }

    public override void DoAIInterval()
    {
        base.DoAIInterval();

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.FREEZING:
                DoFreezing();
                break;
            case (int)State.SCANNING:
                DoScanning();
                break;
            case (int)State.CHASING:
                DoChasing();
                break;
        }
    }

    public void DoFreezing()
    {
        agent.speed = 0f;
        creatureAnimator.speed = 0f;
        UpdateStateClientRpc();
    }

    public void DoScanning()
    {
        agent.speed = 0f;
        creatureAnimator.speed = 0f;
        UpdateStateClientRpc();

        ScanClientRpc();
    }

    [ClientRpc]
    public void ScanClientRpc()
    {

        scanLight.transform.rotation = transform.rotation;
        scanLight.gameObject.SetActive(true);
    }

    public void DoChasing()
    {
        agent.speed = attackCoroutine == null ? 5f : 0f;
        creatureAnimator.speed = 1f;
        UpdateStateClientRpc();

        SetMovingTowardsTargetPlayer(targetPlayer);
        moveTowardsDestination = attackCoroutine == null;
    }

    [ClientRpc]
    public void UpdateStateClientRpc()
    {
        switch (currentBehaviourStateIndex)
        {
            case (int)State.FREEZING:
                DoFreezingForClients();
                break;
            case (int)State.SCANNING:
                DoScanningForClients();
                break;
            case (int)State.CHASING:
                DoChasingForClients();
                break;
        }
    }

    public void DoFreezingForClients()
        => ConfigureClientState(TheDoctor.inertScreen, false, false);

    public void DoScanningForClients()
        => ConfigureClientState(TheDoctor.scanningScreen, false, true);

    public void DoChasingForClients()
        => ConfigureClientState(TheDoctor.foundScreen, true, false);

    private void ConfigureClientState(Material screenMaterial, bool isTrigger, bool isLightActive)
    {
        SetScreenMaterial(screenMaterial);
        corpseCollider.isTrigger = isTrigger;
        scanLight.gameObject.SetActive(isLightActive);
        //scanLight.transform.rotation = transform.rotation;
    }

    public void SetScreenMaterial(Material screenMaterial)
    {
        if (screenMaterial == null) return;

        Material[] corpseMaterials = skinnedMeshRenderer.materials;
        for (int i = 0; i < corpseMaterials.Length; i++)
        {
            if (!corpseMaterials[i].name.StartsWith("MI_Doctor_Screen")) continue;

            corpseMaterials[i] = screenMaterial;
            break;
        }
        skinnedMeshRenderer.materials = corpseMaterials;
    }

    public override void OnCollideWithPlayer(Collider other)
    {
        base.OnCollideWithPlayer(other);

        if (currentBehaviourStateIndex != (int)State.CHASING || attackCoroutine != null) return;

        PlayerControllerB player = MeetsStandardPlayerCollisionConditions(other);
        if (player == null || player != GameNetworkManager.Instance.localPlayerController) return;

        AttackServerRpc((int)player.playerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AttackServerRpc(int playerId)
        => AttackClientRpc(playerId);

    [ClientRpc]
    public void AttackClientRpc(int playerId)
        => attackCoroutine ??= StartCoroutine(AttackCoroutine(StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>()));

    public IEnumerator AttackCoroutine(PlayerControllerB player)
    {
        creatureAnimator.SetTrigger("startAttack");
        //creatureSFX.PlayOneShot(SwingSound);
        agent.speed = 0f;
        moveTowardsDestination = false;

        yield return new WaitForSeconds(0.8f);

        player.DamagePlayer(20, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing);
        creatureAnimator.SetTrigger("startRun");
        attackCoroutine = null;
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (isEnemyDead) return;

        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

        if (currentBehaviourStateIndex == (int)State.CHASING && hitEnemyCoroutine == null)
        {
            amountHit++;
            if (amountHit >= 2) hitEnemyCoroutine = StartCoroutine(HitEnemyCoroutine());
        }
    }

    public IEnumerator HitEnemyCoroutine()
    {
        SwitchToBehaviourStateOnLocalClient((int)State.FREEZING);
        if (IsHost || IsServer) SpawnItem();

        yield return new WaitForSeconds(2f);

        SwitchToBehaviourStateOnLocalClient((int)State.CHASING);
        hitEnemyCoroutine = null;
    }

    public void SpawnItem()
    {
        GameObject gameObject = Instantiate(TheDoctor.doctorHeart.spawnPrefab, transform.position + (Vector3.up * 0.5f), Quaternion.identity, StartOfRound.Instance.propsContainer);
        GrabbableObject grabbableObject = gameObject.GetComponent<GrabbableObject>();
        grabbableObject.fallTime = 0f;
        grabbableObject.isInFactory = !isOutside;
        gameObject.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoAnimationServerRpc(string animationState)
        => DoAnimationClientRpc(animationState);

    [ClientRpc]
    public void DoAnimationClientRpc(string animationState)
        => creatureAnimator.SetTrigger(animationState);
}
