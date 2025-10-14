using GameNetcodeStuff;
using Unity.Netcode;

namespace TheDoctor.Managers;

internal class TheDoctorNetworkManager : NetworkBehaviour
{
    public static TheDoctorNetworkManager Instance;

    public void Awake() => Instance = this;

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void SpawnSpectralDecoyEveryoneRpc(int playerId)
    {
        PlayerControllerB player = StartOfRound.Instance.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        _ = Instantiate(TheDoctor.doctorClone, player.transform.position + (player.transform.forward * 1.5f), player.transform.rotation);
    }
}