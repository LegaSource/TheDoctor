using LegaFusionCore.Utilities;
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
        if (LFCUtilities.IsServer)
        {
            int value = Random.Range(ConfigManager.brainMinValue.Value, ConfigManager.brainMaxValue.Value);
            SetScrapValueEveryoneRpc(value);
        }
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SetScrapValueEveryoneRpc(int value) => SetScrapValue(value);
}
