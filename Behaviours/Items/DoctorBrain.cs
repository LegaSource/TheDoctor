using LegaFusionCore;
using TheDoctor.Managers;
using Unity.Netcode;
using UnityEngine;

namespace TheDoctor.Behaviours.Items;

public class DoctorBrain : PhysicsProp
{
    public override void Start()
    {
        base.Start();

        if (ConfigManager.brainSpecialAbility.Value) LFCUtilities.SetAddonComponent<SpectralDecoy>(this, Constants.SPECTRAL_DECOY);
        if (IsHost || IsServer)
        {
            int value = Random.Range(ConfigManager.brainMinValue.Value, ConfigManager.brainMaxValue.Value);
            SetScrapValueClientRpc(value);
        }
    }

    [ClientRpc]
    public void SetScrapValueClientRpc(int value)
        => SetScrapValue(value);

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        ActivateSpecialAbilityServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ActivateSpecialAbilityServerRpc()
        => ActivateSpecialAbilityClientRpc();

    [ClientRpc]
    public void ActivateSpecialAbilityClientRpc()
        => GetComponent<SpectralDecoy>()?.ActivateSpecialAbility();
}
