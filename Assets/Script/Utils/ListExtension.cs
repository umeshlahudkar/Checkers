using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ListExtension 
{
    public static void Shuffle<T>(this List<T> list)
    {
        int count = list.Count;

        while(count > 0)
        {
            int randomIndex = Random.Range(0, count);
            T temp = list[randomIndex];
            list[randomIndex] = list[count-1];
            list[count - 1] = temp;

            count--;
        }
    }
}
