using TheDoctor.Managers;
using UnityEngine;

namespace TheDoctor.Behaviours.Items;

public class DoctorEye : DoctorItem
{
    public Animator animator;

    public override void Start()
    {
        base.Start();
        currentTimeLeft = ConfigManager.eyeTrackingDuration.Value;
    }

    public override void Update()
    {
        base.Update();

        if (!isTracking || doctorBrain == null || playerHeldBy == null || playerHeldBy != GameNetworkManager.Instance.localPlayerController) return;
        if (playerHeldBy.quickMenuManager.isMenuOpen) return;

        Vector2 lookInput = playerHeldBy.playerActions.Movement.Look.ReadValue<Vector2>() * IngamePlayerSettings.Instance.settings.lookSensitivity * 0.008f;
        doctorBrain.cameraPivot.Rotate(new Vector3(0f, lookInput.x, 0f));

        // Rotation verticale avec clamping
        float verticalAngle = doctorBrain.cameraPivot.localEulerAngles.x - lookInput.y;
        verticalAngle = (verticalAngle > 180f) ? (verticalAngle - 360f) : verticalAngle;
        verticalAngle = Mathf.Clamp(verticalAngle, -45f, 45f);
        doctorBrain.cameraPivot.localEulerAngles = new Vector3(verticalAngle, doctorBrain.cameraPivot.localEulerAngles.y, 0f);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);

        if (hasBeenUsed || !isTracking || doctorBrain == null) return;
        doctorBrain.SwitchCamera(doctorBrain.camera == GameNetworkManager.Instance.localPlayerController.gameplayCamera);
    }

    public override void StartTrackingForClients()
    {
        base.StartTrackingForClients();
        itemProperties.toolTips[0] = "Switch camera : [LMB]";
    }

    public override void ItemDeactivate()
    {
        base.ItemDeactivate();
        doctorBrain.SwitchCamera(true);
    }

    public override void PocketItem()
    {
        doctorBrain.SwitchCamera(true);
        base.PocketItem();
    }

    public override void DiscardItem()
    {
        doctorBrain.SwitchCamera(true);
        base.DiscardItem();
    }

    public override void OnDestroy()
    {
        doctorBrain.SwitchCamera(true);
        base.OnDestroy();
    }
}
