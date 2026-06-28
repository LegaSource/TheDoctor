using TheDoctor.Managers;
using Unity.Netcode;
using UnityEngine;

namespace TheDoctor.Behaviours.Items;

public class DoctorBrain : PhysicsProp
{
    public void InitializeForServer()
    {
        int value = Random.Range(ConfigManager.brainMinValue.Value, ConfigManager.brainMaxValue.Value);
        InitializeEveryoneRpc(value);
    }

    [Rpc(SendTo.Everyone, RequireOwnership = false)]
    public void InitializeEveryoneRpc(int value) => SetScrapValue(value);
}
