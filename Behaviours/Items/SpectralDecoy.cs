using GameNetcodeStuff;
using LegaFusionCore.Behaviours.Addons;
using TheDoctor.Managers;

namespace TheDoctor.Behaviours.Items;

public class SpectralDecoy : AddonComponent
{
    public override void ActivateSpecialAbility()
    {
        if (onCooldown) return;

        PlayerControllerB player = GetComponentInParent<GrabbableObject>()?.playerHeldBy;
        if (player == null) return;

        StartCooldown(ConfigManager.brainAbilityCooldown.Value);
        _ = Instantiate(TheDoctor.doctorClone, player.transform.position + (player.transform.forward * 1.5f), player.transform.rotation);
    }
}
