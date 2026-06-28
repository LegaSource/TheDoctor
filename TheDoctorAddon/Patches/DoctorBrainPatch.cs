using AddonFusion;
using HarmonyLib;
using TheDoctor.Behaviours.Items;
using TheDoctorAddon.Behaviours.AddonComponents;

namespace TheDoctorAddon.Patches;

public class DoctorBrainPatch
{
    [HarmonyPatch(typeof(DoctorBrain), nameof(DoctorBrain.InitializeEveryoneRpc))]
    [HarmonyPostfix]
    public static void InitializeForEveryone(DoctorBrain __instance) => AFUtilities.SetAddonComponent<SpectralDecoy>(__instance);
}
