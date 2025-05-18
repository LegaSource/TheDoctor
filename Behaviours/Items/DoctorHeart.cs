using GameNetcodeStuff;
using System.Collections;
using TheDoctor.Behaviours.Enemies;
using TheDoctor.Managers;
using Unity.Netcode;
using UnityEngine;

namespace TheDoctor.Behaviours.Items;

public class DoctorHeart : PhysicsProp
{
    public bool isTracking = false;
    public int currentTimeLeft;

    public DoctorBrainAI doctorBrain;
    public AudioSource audioSource;
    public Animator animator;

    public override void Start()
    {
        base.Start();

        if (IsHost || IsServer)
        {
            int value = Random.Range(15, 30);
            SetScrapValueClientRpc(value);
        }

        currentTimeLeft = ConfigManager.trackingDuration.Value;
        doctorBrain = FindFirstObjectByType<DoctorBrainAI>();
        if (doctorBrain == null) TheDoctor.mls.LogError("Doctor's Brain not found for the Doctor's Heart");
    }

    [ClientRpc]
    public void SetScrapValueClientRpc(int value)
        => SetScrapValue(value);

    public override void Update()
    {
        base.Update();

        if (!isTracking || playerHeldBy == null || doctorBrain == null) return;

        float distance = Vector3.Distance(transform.position, doctorBrain.transform.position);
        float proximityFactor = Mathf.Pow(Mathf.Clamp01(1f - (distance / 100f)), 2f);
        audioSource.pitch = Mathf.Lerp(0.75f, 2.5f, proximityFactor);
        animator.speed = Mathf.Lerp(1.2f, 4f, proximityFactor);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (isTracking || doctorBrain == null || audioSource.isPlaying) return;
        StartTrackingServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTrackingServerRpc()
        => StartTrackingClientRpc();

    [ClientRpc]
    public void StartTrackingClientRpc()
    {
        isTracking = true;
        audioSource.Play();
        animator.Play("heartbeat", 0, 0f);
        _ = StartCoroutine(StartTrackingCoroutine());
        SetControlTipsForItem();
    }

    public IEnumerator StartTrackingCoroutine()
    {
        while (currentTimeLeft > 0)
        {
            yield return new WaitForSeconds(1f);

            currentTimeLeft--;
            SetControlTipsForItem();
        }
        DestroyObjectInHand(playerHeldBy);
    }

    public override void SetControlTipsForItem()
    {
        itemProperties.toolTips[1] = isTracking ? $"[Time Left : {currentTimeLeft}]" : "";
        HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips, holdingItem: true, itemProperties);
    }

    public override void DestroyObjectInHand(PlayerControllerB playerHolding)
    {
        base.DestroyObjectInHand(playerHolding);

        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject.IsSpawned) networkObject.Despawn();
    }
}
