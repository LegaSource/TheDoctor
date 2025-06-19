using System.Collections;
using System.Linq;
using TheDoctor.Behaviours.Enemies;
using Unity.Netcode;
using UnityEngine;

namespace TheDoctor.Behaviours.Items;

public class DoctorItem : PhysicsProp
{
    public bool hasBeenUsed = false;
    public bool isTracking = false;
    public int currentTimeLeft;

    public DoctorBrainAI doctorBrain;

    [ClientRpc]
    public void InitializeDoctorItemClientRpc(NetworkObjectReference obj, int value)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        doctorBrain = networkObject.gameObject.GetComponentInChildren<DoctorBrainAI>();
        SetScrapValue(value);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!hasBeenUsed && !isTracking) StartTrackingServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartTrackingServerRpc()
        => StartTrackingClientRpc();

    [ClientRpc]
    public void StartTrackingClientRpc()
    {
        if (doctorBrain == null)
        {
            TheDoctor.mls.LogError("Doctor's Brain not found for the Doctor's item");
            return;
        }

        isTracking = true;
        StartTrackingForClients();
    }

    public virtual void StartTrackingForClients()
        => _ = StartCoroutine(StartTrackingCoroutine());

    public IEnumerator StartTrackingCoroutine()
    {
        while (currentTimeLeft > 0)
        {
            yield return new WaitForSecondsRealtime(1f);

            currentTimeLeft--;
            SetControlTipsForItem();
        }

        ItemDeactivate();
    }

    public override void SetControlTipsForItem()
    {
        if (playerHeldBy == null || isPocketed || playerHeldBy != GameNetworkManager.Instance.localPlayerController) return;

        string toolTip = isTracking ? $"[Time Left : {currentTimeLeft}]" : "";
        HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips.Concat([toolTip]).ToArray(), holdingItem: true, itemProperties);
    }

    public virtual void ItemDeactivate()
    {
        hasBeenUsed = true;
        isTracking = false;
        SetControlTipsForItem();
    }
}
