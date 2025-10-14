using System.Collections;
using System.Collections.Generic;
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

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void InitializeDoctorItemEveryoneRpc(NetworkObjectReference obj, int value)
    {
        if (!obj.TryGet(out NetworkObject networkObject)) return;

        doctorBrain = networkObject.gameObject.GetComponentInChildren<DoctorBrainAI>();
        SetScrapValue(value);
    }

    public override void GrabItem()
    {
        base.GrabItem();

        if (playerHeldBy == null || playerHeldBy != GameNetworkManager.Instance.localPlayerController) return;
        HUDManager.Instance.DisplayTip(Constants.MSG_INFORMATION, Constants.MSG_DOCTOR_ITEM_TIP);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (!hasBeenUsed && !isTracking) StartTrackingEveryoneRpc();
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void StartTrackingEveryoneRpc()
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
    {
        _ = StartCoroutine(StartTrackingCoroutine());

        doctorBrain.canLoseChase = false;
        IEnumerable<DoctorCorpseAI> corpses = [];
        if (!doctorBrain.corpses.Any(c => c != null && c.currentBehaviourStateIndex == (int)DoctorCorpseAI.State.CHASING))
        {
            corpses = doctorBrain.corpses
                .Where(c => c != null && c.currentBehaviourStateIndex != (int)DoctorCorpseAI.State.CHASING)
                .Take(3);
        }
        foreach (DoctorCorpseAI corpse in corpses)
        {
            corpse.targetPlayer = playerHeldBy;
            corpse.SwitchToBehaviourStateOnLocalClient((int)DoctorCorpseAI.State.CHASING);
        }
    }

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
        if (isPocketed || playerHeldBy == null || playerHeldBy != GameNetworkManager.Instance.localPlayerController) return;

        string toolTip = isTracking ? $"[Time Left : {currentTimeLeft}]" : "";
        HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips.Concat([toolTip]).ToArray(), holdingItem: true, itemProperties);
    }

    public virtual void ItemDeactivate()
    {
        hasBeenUsed = true;
        isTracking = false;
        doctorBrain.canLoseChase = true;
        SetControlTipsForItem();
    }
}
