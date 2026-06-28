using AddonFusion.Behaviours.AddonComponents;
using AddonFusion.Behaviours.Scripts;
using TheDoctor;
using TheDoctor.Managers;
using static AddonFusion.Behaviours.Scripts.AddonTargetDatabase;

namespace TheDoctorAddon.Behaviours.AddonComponents;

[AddonInfo(AddonTargetType.ALL)]
public class SpectralDecoy : AddonComponent
{
    public override string AddonName => Constants.SPECTRAL_DECOY;
    public override bool IsPassive => false;

    public override void ActivateAddonAbility()
    {
        if (!onCooldown && StartOfRound.Instance.shipHasLanded && grabbableObject.playerHeldBy != null)
        {
            StartCooldown(ConfigManager.spectralDecoyCooldown.Value);
            TheDoctorNetworkManager.Instance.SpawnSpectralDecoyEveryoneRpc((int)grabbableObject.playerHeldBy.playerClientId);
        }
    }
}