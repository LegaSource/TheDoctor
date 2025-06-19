using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheDoctor.Behaviours.Items;
using TheDoctor.Managers;
using Unity.Netcode;
using UnityEngine;

namespace TheDoctor.Behaviours.Enemies;

public class DoctorCorpseAI : EnemyAI
{
    public DoctorBrainAI doctorBrain;

    public Transform TurnCompass;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Collider corpseCollider;
    public Light scanLight;
    public Light chaseLight;

    public int oldBehaviourStateIndex = 0;
    public int amountHit = 0;
    public float moveTimer = 0f;
    public bool hasDroppedItem = false;

    public Coroutine attackCoroutine;
    public Coroutine hitEnemyCoroutine;

    public AudioClip chaseSound;
    public AudioClip[] moveSounds = Array.Empty<AudioClip>();
    public AudioClip[] attackSounds = Array.Empty<AudioClip>();

    public ParticleSystem explosionParticle;

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
        _ = StartCoroutine(InitializeAnimatorCoroutine());

        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null) TheDoctor.mls.LogError("SkinnedMeshRenderer not found for the DoctorCorpse");

        Transform headBone = skinnedMeshRenderer.bones.FirstOrDefault(b => b.name.Equals("PHYSICS_Head"));
        if (headBone != null)
        {
            scanLight.transform.SetParent(headBone, worldPositionStays: false);
            scanLight.transform.localPosition = Vector3.zero;
            scanLight.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
            scanLight.gameObject.SetActive(false);

            chaseLight.transform.SetParent(headBone, worldPositionStays: false);
            chaseLight.transform.localPosition = Vector3.zero;
            chaseLight.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
            chaseLight.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    public void InitializeCorpseClientRpc(NetworkObjectReference obj)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;
        doctorBrain = networkObject.gameObject.GetComponentInChildren<DoctorBrainAI>();
    }

    public IEnumerator InitializeAnimatorCoroutine()
    {
        creatureAnimator.SetTrigger("startRun");
        yield return null;
        creatureAnimator.Play("run", 0, UnityEngine.Random.value);
        creatureAnimator.speed = 0f;
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
        if (moveSounds.Length > 0 && moveTimer <= 0)
        {
            creatureSFX.PlayOneShot(moveSounds[UnityEngine.Random.Range(0, moveSounds.Length)]);
            moveTimer = 0.4f;
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
        UpdateStateClientRpc();
    }

    public void DoScanning()
    {
        agent.speed = 0f;
        UpdateStateClientRpc();
    }

    public void DoChasing()
    {
        agent.speed = attackCoroutine == null ? ConfigManager.corpseSpeed.Value : 0f;
        UpdateStateClientRpc();

        SetMovingTowardsTargetPlayer(targetPlayer);
        moveTowardsDestination = attackCoroutine == null;
    }

    [ClientRpc]
    public void UpdateStateClientRpc()
    {
        if (currentBehaviourStateIndex == oldBehaviourStateIndex) return;
        oldBehaviourStateIndex = currentBehaviourStateIndex;

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
        => ConfigureClientState(TheDoctor.inertScreen, 0f, false, false, false);

    public void DoScanningForClients()
        => ConfigureClientState(TheDoctor.scanningScreen, 0f, false, true, false);

    public void DoChasingForClients()
        => ConfigureClientState(TheDoctor.foundScreen, 1f, true, false, true);

    private void ConfigureClientState(Material screenMaterial, float animationSpeed, bool isTrigger, bool isScanLightActive, bool isChaseLightActive)
    {
        SetScreenMaterial(screenMaterial);

        creatureAnimator.speed = animationSpeed;
        corpseCollider.isTrigger = isTrigger;

        scanLight.gameObject.SetActive(isScanLightActive);
        chaseLight.gameObject.SetActive(isChaseLightActive);
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
        creatureSFX.PlayOneShot(attackSounds[UnityEngine.Random.Range(0, attackSounds.Length)]);
        agent.speed = 0f;
        moveTowardsDestination = false;

        yield return new WaitForSeconds(0.5f);

        player.DamagePlayer(20, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing);
        creatureAnimator.SetTrigger("startRun");
        attackCoroutine = null;
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (isEnemyDead) return;

        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

        if (hitEnemyCoroutine == null && currentBehaviourStateIndex == (int)State.CHASING)
        {
            amountHit++;
            if (amountHit >= 2) hitEnemyCoroutine = StartCoroutine(HitEnemyCoroutine());
        }
        // Si FREEZING ou SCANNING, réveiller le corps
        else if (hitEnemyCoroutine == null && (currentBehaviourStateIndex == (int)State.FREEZING || currentBehaviourStateIndex == (int)State.SCANNING))
        {
            if (doctorBrain.scanCoroutine != null)
            {
                doctorBrain.StopCoroutine(doctorBrain.scanCoroutine);
                doctorBrain.scanCoroutine = null;
            }

            foreach (DoctorCorpseAI corpse in doctorBrain.corpses.Where(c => c != null && c.currentBehaviourStateIndex == (int)State.SCANNING))
            {
                corpse.creatureSFX.Stop();
                corpse.SwitchToBehaviourStateOnLocalClient((int)State.FREEZING);
            }

            // Réveiller 2 autres corps si aucun n'est en chase
            if (!doctorBrain.corpses.Any(c => c != null && c.currentBehaviourStateIndex == (int)State.CHASING))
            {
                TDUtilities.Shuffle(doctorBrain.corpses);
                doctorBrain.corpses.Where(c => c != null && c != this)
                    .Take(2)
                    .ToList()
                    .ForEach(c =>
                    {
                        c.targetPlayer = playerWhoHit;
                        c.SwitchToBehaviourStateOnLocalClient((int)State.CHASING);
                    });
            }

            targetPlayer = playerWhoHit;
            SwitchToBehaviourStateOnLocalClient((int)State.CHASING);
        }
    }

    public IEnumerator HitEnemyCoroutine()
    {
        amountHit = 0;
        explosionParticle.Play();
        SwitchToBehaviourStateOnLocalClient((int)State.FREEZING);
        if (IsHost || IsServer) SpawnDoctorItem();
        hasDroppedItem = true;

        yield return new WaitForSeconds(4f);

        explosionParticle.Stop();
        SwitchToBehaviourStateOnLocalClient((int)State.CHASING);
        hitEnemyCoroutine = null;
    }

    public void SpawnDoctorItem()
    {
        if (hasDroppedItem) return;

        List<GameObject> doctorItems = [TheDoctor.doctorHeart.spawnPrefab, TheDoctor.doctorEye.spawnPrefab];
        GameObject gameObject = Instantiate(doctorItems[new System.Random().Next(doctorItems.Count)], transform.position + (Vector3.up * 0.5f), Quaternion.identity, StartOfRound.Instance.propsContainer);

        DoctorItem doctorItem = gameObject.GetComponent<DoctorItem>();
        doctorItem.fallTime = 0f;
        doctorItem.isInFactory = !isOutside;
        gameObject.GetComponent<NetworkObject>().Spawn();

        int value = doctorItem is DoctorEye ? UnityEngine.Random.Range(ConfigManager.eyeMinValue.Value, ConfigManager.eyeMaxValue.Value) : UnityEngine.Random.Range(ConfigManager.heartMinValue.Value, ConfigManager.heartMaxValue.Value);
        doctorItem.InitializeDoctorItemClientRpc(doctorBrain.GetComponent<NetworkObject>(), value);
    }

    public override void KillEnemy(bool destroy = false)
    {
        if (IsHost || IsServer) KillExplosionServerRpc(destroy);
    }

    [ServerRpc(RequireOwnership = false)]
    public void KillExplosionServerRpc(bool destroy)
        => KillExplosionClientRpc(destroy);

    [ClientRpc]
    public void KillExplosionClientRpc(bool destroy)
    {
        Landmine.SpawnExplosion(transform.position + Vector3.up, spawnExplosionEffect: true, 0f, 4f, 20);
        base.KillEnemy(destroy);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DoAnimationServerRpc(string animationState)
        => DoAnimationClientRpc(animationState);

    [ClientRpc]
    public void DoAnimationClientRpc(string animationState)
        => creatureAnimator.SetTrigger(animationState);
}
