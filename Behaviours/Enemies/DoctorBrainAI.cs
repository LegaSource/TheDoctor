using GameNetcodeStuff;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheDoctor.Behaviours.Items;
using TheDoctor.Managers;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

namespace TheDoctor.Behaviours.Enemies;

public class DoctorBrainAI : EnemyAI
{
    public List<EntranceTeleport> entrances;
    public List<DoctorCorpseAI> corpses = [];

    public Camera camera;
    public Transform cameraPivot;
    public Camera playerCamera;

    public float scanTimer;
    public float scanCooldown = 5f;
    public bool canLoseChase = true;

    public Coroutine scanCoroutine;
    public Coroutine killCoroutine;

    public AudioClip speakerHackedSound;
    public AudioClip deathSound;

    public ParticleSystem auraParticle;

    public enum State
    {
        MANAGING
    }

    public override void Start()
    {
        base.Start();

        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
        playerCamera = player.gameplayCamera;

        currentBehaviourStateIndex = (int)State.MANAGING;
        entrances = FindObjectsOfType<EntranceTeleport>().ToList();

        if (IsHost || IsServer) SpawnCorpses();
        if (player.isInsideFactory)
        {
            GameObject audioObj = new GameObject("HackedSpeakerAudio");
            audioObj.transform.parent = player.transform;
            audioObj.transform.localPosition = Vector3.forward * 50f;

            AudioSource audioSource = audioObj.AddComponent<AudioSource>();
            audioSource.clip = speakerHackedSound;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 10f;
            audioSource.maxDistance = 200f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.Play();

            Destroy(audioObj, speakerHackedSound.length);
        }
        else
        {
            StartOfRound.Instance.speakerAudioSource.PlayOneShot(speakerHackedSound);
        }
    }

    private void SpawnCorpses()
    {
        Vector3[] positions = GetCorpsesPositions();
        foreach (Vector3 position in positions)
        {
            GameObject gameObject = Instantiate(TheDoctor.doctorCorpseEnemy.enemyPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            gameObject.GetComponentInChildren<NetworkObject>().Spawn(true);

            DoctorCorpseAI doctorCorpse = gameObject.GetComponent<DoctorCorpseAI>();
            doctorCorpse.InitializeCorpseClientRpc(GetComponent<NetworkObject>());
            corpses.Add(doctorCorpse);
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

        if (isEnemyDead || StartOfRound.Instance.allPlayersDead || killCoroutine != null) return;

        switch (currentBehaviourStateIndex)
        {
            case (int)State.MANAGING:
                DoManaging();
                break;
        }
    }

    public void DoManaging()
    {
        if (killCoroutine != null) return;

        List<DoctorCorpseAI> awakedCorpses = corpses.Where(c => c != null && c.currentBehaviourStateIndex == (int)DoctorCorpseAI.State.CHASING).ToList();
        if (canLoseChase && awakedCorpses.Any())
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
        }

        if (corpses.Any(c => c != null && (c.currentBehaviourStateIndex == (int)DoctorCorpseAI.State.CHASING || c.hitEnemyCoroutine != null)))
            return;

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
        List<DoctorCorpseAI> scanningCorpses = corpses.Where(c => c != null).Take(3).ToList();
        scanningCorpses.ForEach(c => c.SwitchToBehaviourClientRpc((int)DoctorCorpseAI.State.SCANNING));

        // Délai de grâce
        yield return new WaitForSeconds(0.5f);

        float timePassed = 0f;
        PlayerControllerB foundedPlayer = null;
        while (timePassed < ConfigManager.scanDuration.Value)
        {
            if (scanningCorpses.Any(c => (foundedPlayer = FoundClosestPlayerInRange(c)) != null)) break;
            yield return new WaitForSeconds(0.2f);
            timePassed += 0.2f;
        }

        foreach (DoctorCorpseAI corpse in scanningCorpses)
        {
            corpse.creatureSFX.Stop();
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
        PlayerControllerB player = corpse.CheckLineOfSightForPlayer(60f, 20, 3);
        return player != null && corpse.PlayerIsTargetable(player) ? player : null;
    }

    public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (isEnemyDead) return;

        base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);

        enemyHP -= force;
        if (IsOwner)
        {
            corpses.ForEach(c =>
            {
                if (c != null && c.currentBehaviourStateIndex != (int)DoctorCorpseAI.State.CHASING)
                {
                    c.targetPlayer = playerWhoHit;
                    c.SwitchToBehaviourClientRpc((int)DoctorCorpseAI.State.CHASING);
                }
            });

            if (enemyHP <= 0) KillEnemyOnOwnerClient();
        }
    }

    public override void KillEnemy(bool destroy = false)
    {
        if (IsHost || IsServer)
        {
            if (scanCoroutine != null)
            {
                StopCoroutine(scanCoroutine);
                scanCoroutine = null;
            }

            KillAnimationServerRpc(destroy);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void KillAnimationServerRpc(bool destroy)
        => KillAnimationClientRpc(destroy);

    [ClientRpc]
    public void KillAnimationClientRpc(bool destroy)
        => killCoroutine ??= StartCoroutine(KillAnimationCoroutine(destroy));

    public IEnumerator KillAnimationCoroutine(bool destroy)
    {
        creatureSFX.PlayOneShot(deathSound);
        auraParticle.Play();

        if (IsServer || IsHost)
        {
            float interval = deathSound.length / corpses.Count;
            foreach (DoctorCorpseAI corpse in corpses.ToList())
            {
                yield return new WaitForSecondsRealtime(interval);

                if (corpse.NetworkObject == null || !corpse.NetworkObject.IsSpawned) continue;
                corpse.KillEnemyOnOwnerClient();
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(deathSound.length);
        }

        SwitchCamera(true);
        SpawnParticle(transform.position);
        Landmine.SpawnExplosion(transform.position + Vector3.up, spawnExplosionEffect: true, 6f, 6.3f);

        if (IsServer || IsHost)
        {
            base.KillEnemy(destroy);
            SpawnBrain();
        }
    }

    public void SwitchCamera(bool switchingBack)
    {
        if (isEnemyDead || camera == null || playerCamera == null) return;

        PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;

        camera.enabled = !switchingBack;
        player.gameplayCamera = switchingBack ? playerCamera : camera;
        player.thisPlayerModel.shadowCastingMode = switchingBack ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;

        if (!switchingBack)
        {
            cameraPivot.transform.LookAt(player.transform.position);
            CustomPassManager.SetupAuraForObjects([player.gameObject], TheDoctor.yellowShader);
            corpses.ForEach(c => CustomPassManager.SetupAuraForObjects([c.gameObject], TheDoctor.redShader));
            return;
        }
        CustomPassManager.ClearAura();
    }

    public void SpawnParticle(Vector3 explosionPosition)
    {
        GameObject particleObject = Instantiate(TheDoctor.electroExplosionParticle, explosionPosition + transform.up, Quaternion.identity);
        ParticleSystem explosionParticle = particleObject.GetComponent<ParticleSystem>();
        Destroy(particleObject, explosionParticle.main.duration + explosionParticle.main.startLifetime.constantMax);
    }

    public void SpawnBrain()
    {
        GameObject gameObject = Instantiate(TheDoctor.doctorBrain.spawnPrefab, transform.position + (Vector3.up * 0.5f), Quaternion.identity, StartOfRound.Instance.propsContainer);
        DoctorBrain doctorBrainItem = gameObject.GetComponent<DoctorBrain>();
        doctorBrainItem.fallTime = 0f;
        doctorBrainItem.isInFactory = !isOutside;
        gameObject.GetComponent<NetworkObject>().Spawn();
    }
}
