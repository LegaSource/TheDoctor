using GameNetcodeStuff;
using LegaFusionCore.Behaviours.Addons;
using TheDoctor.Managers;

namespace TheDoctor.Behaviours.Items;

public class SpectralDecoy : AddonComponent
{
    public override void ActivateAddonAbility()
    {
        if (onCooldown || !StartOfRound.Instance.shipHasLanded) return;

        PlayerControllerB player = GetComponentInParent<GrabbableObject>()?.playerHeldBy;
        if (player == null) return;

        StartCooldown(ConfigManager.spectralDecoyCooldown.Value);
        TheDoctorNetworkManager.Instance.SpawnSpectralDecoyEveryoneRpc((int)player.playerClientId);
    }
}
