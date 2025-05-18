using System.Collections.Generic;
using UnityEngine;

namespace TheDoctor;

internal class TDUtilities
{
    public static void Shuffle<T>(IList<T> collection)
    {
        for (int i = collection.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (collection[randomIndex], collection[i]) = (collection[i], collection[randomIndex]);
        }
    }
}
