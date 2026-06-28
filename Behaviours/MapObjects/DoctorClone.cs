using LegaFusionCore.Managers;
using LegaFusionCore.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TheDoctor.Managers;
using Unity.Netcode;
using UnityEngine;

namespace TheDoctor.Behaviours.MapObjects;

public class DoctorClone : NetworkBehaviour
{
    private void Start() => StartCoroutine(AttractEnemiesCoroutine());

    public IEnumerator AttractEnemiesCoroutine()
    {
        GameObject audioObject = Instantiate(TheDoctor.doctorCloneAudio, transform.position, Quaternion.identity);
        audioObject.GetComponent<AudioSource>()?.Play();

        Vector3 explosionPosition = transform.position + (transform.forward * 1.6f);

        List<EnemyAI> enemies = [];
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 10f, 524288, QueryTriggerInteraction.Collide);
        foreach (Collider hitCollider in hitColliders)
        {
            EnemyAI enemy = hitCollider.GetComponent<EnemyAICollisionDetect>()?.mainScript;
            if (enemy != null) enemies.Add(enemy);
        }

        float timePassed = 0f;
        while (timePassed < 5.5f)
        {
            enemies.ForEach(e =>
            {
                _ = e.SetDestinationToPosition(explosionPosition);
                e.moveTowardsDestination = true;
            });

            yield return null;
            timePassed += Time.deltaTime;
        }

        if (LFCUtilities.IsServer)
        {
            enemies.Where(e => Vector3.Distance(e.transform.position, explosionPosition) <= 5f)
                .ToList()
                .ForEach(e => e.HitEnemyOnLocalClient(force: ConfigManager.spectralDecoyDamage.Value));
        }

        LFCGlobalManager.PlayParticle($"{LegaFusionCore.LegaFusionCore.modName}{LegaFusionCore.LegaFusionCore.darkExplosionParticle.name}", explosionPosition + transform.up, Quaternion.identity);
        Destroy(gameObject);
    }
}
