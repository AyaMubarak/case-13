using System.Collections.Generic;
using UnityEngine;

public class RandomSpawner : MonoBehaviour
{
    [Header("Items to Spawn (الأدلة)")]
    public List<GameObject> objectsToSpawn;

    [Header("Possible Locations (أماكن محتملة)")]
    public List<Transform> spawnPoints;

    private void Start()
    {
        SpawnObjectsRandomly();
    }

    public void SpawnObjectsRandomly()
    {
        if (objectsToSpawn == null || objectsToSpawn.Count == 0) return;
        if (spawnPoints == null || spawnPoints.Count == 0) return;

        // نسخة مؤقتة من الأماكن حتى لا نضع دليلين في نفس المكان
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        foreach (var obj in objectsToSpawn)
        {
            if (availablePoints.Count == 0)
            {
                Debug.LogWarning("[RandomSpawner] Not enough spawn points for all objects!");
                break;
            }

            // اختيار مكان عشوائي من المتاح
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomIndex];

            // وضع الدليل في المكان المختار
            if (obj != null && selectedPoint != null)
            {
                obj.transform.position = selectedPoint.position;
                obj.transform.rotation = selectedPoint.rotation;
                obj.SetActive(true);
            }

            // إزالة المكان من القائمة حتى لا يتم استخدامه مرة أخرى
            availablePoints.RemoveAt(randomIndex);
        }
    }
}
