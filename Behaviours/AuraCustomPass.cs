using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace TheDoctor.Behaviours;

public class AuraCustomPass : CustomPass
{
    private readonly Dictionary<Renderer, Material> targetRenderers = [];

    public void AddTargetRenderers(Renderer[] renderers, Material material)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || targetRenderers.ContainsKey(renderer)) continue;
            targetRenderers.Add(renderer, material);
        }
    }

    public void RemoveTargetRenderers(Renderer[] renderers)
    {
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !targetRenderers.ContainsKey(renderer)) continue;
            _ = targetRenderers.Remove(renderer);
        }
    }

    public void ClearTargetRenderers()
        => targetRenderers.Clear();

    public override void Execute(CustomPassContext ctx)
    {
        if (targetRenderers == null || targetRenderers.Count == 0) return;

        foreach (KeyValuePair<Renderer, Material> keyValue in targetRenderers)
        {
            Renderer renderer = keyValue.Key;
            Material material = keyValue.Value;

            if (renderer == null || material == null || renderer.sharedMaterials == null) continue;
            for (int i = 0; i < renderer.sharedMaterials.Length; i++) ctx.cmd.DrawRenderer(renderer, material);
        }
    }
}
