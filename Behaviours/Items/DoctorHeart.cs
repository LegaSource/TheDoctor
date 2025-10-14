using TheDoctor.Managers;
using UnityEngine;

namespace TheDoctor.Behaviours.Items;

public class DoctorHeart : DoctorItem
{
    public AudioSource audioSource;
    public Animator animator;

    public override void Start()
    {
        base.Start();
        currentTimeLeft = ConfigManager.heartTrackingDuration.Value;
    }

    public override void Update()
    {
        base.Update();
        if (hasBeenUsed || !isTracking || playerHeldBy == null || doctorBrain == null) return;

        float distance = Vector3.Distance(transform.position, doctorBrain.transform.position);
        float proximityFactor = Mathf.Pow(Mathf.Clamp01(1f - (distance / 100f)), 2f);
        audioSource.pitch = Mathf.Lerp(0.75f, 2.5f, proximityFactor);
        animator.speed = Mathf.Lerp(1.2f, 4f, proximityFactor);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        if (!audioSource.isPlaying) base.ItemActivate(used, buttonDown);
    }

    public override void StartTrackingForClients()
    {
        base.StartTrackingForClients();

        audioSource.Play();
        animator.Play("heartbeat", 0, 0f);
    }

    public override void ItemDeactivate()
    {
        base.ItemDeactivate();
        if (audioSource.isPlaying) audioSource.Stop();
    }
}
