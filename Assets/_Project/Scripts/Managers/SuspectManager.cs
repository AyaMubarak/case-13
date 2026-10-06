using System.Collections.Generic;
using UnityEngine;

public class SuspectManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> suspectObjects = new();

    public void SetSuspectCount(int count)
    {
        int clamped = Mathf.Clamp(count, 0, suspectObjects.Count);

        for (int i = 0; i < suspectObjects.Count; i++)
        {
            suspectObjects[i].SetActive(i < clamped);
        }
    }
}
