using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Creates the missing Case 1 roof and keeps players inside the investigation room.</summary>
public class Case01RoomShell : MonoBehaviour
{
    private const float WallThickness = 0.35f;
    private const float RoomHeight = 5.2f;
    private static Bounds playableBounds;
    private static bool hasPlayableBounds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForCaseOne()
    {
        if (SceneManager.GetActiveScene().name != "Case01_Main") return;
        if (Object.FindAnyObjectByType<Case01RoomShell>() != null) return;

        GameObject shell = new GameObject("Case01_RoomShell");
        shell.AddComponent<Case01RoomShell>();
    }

    private void Awake()
    {
        if (!TryGetNavigationBounds(out Bounds bounds))
        {
            bounds = new Bounds(Vector3.zero, new Vector3(28f, RoomHeight, 28f));
        }

        playableBounds = bounds;
        playableBounds.Expand(-WallThickness * 2f);
        hasPlayableBounds = true;

        CreateCeiling(bounds);
        CreateBoundaryCollider("North_Boundary", new Vector3(bounds.center.x, RoomHeight * 0.5f, bounds.max.z), new Vector3(bounds.size.x, RoomHeight, WallThickness));
        CreateBoundaryCollider("South_Boundary", new Vector3(bounds.center.x, RoomHeight * 0.5f, bounds.min.z), new Vector3(bounds.size.x, RoomHeight, WallThickness));
        CreateBoundaryCollider("East_Boundary", new Vector3(bounds.max.x, RoomHeight * 0.5f, bounds.center.z), new Vector3(WallThickness, RoomHeight, bounds.size.z));
        CreateBoundaryCollider("West_Boundary", new Vector3(bounds.min.x, RoomHeight * 0.5f, bounds.center.z), new Vector3(WallThickness, RoomHeight, bounds.size.z));
        CreateLockedDoor(bounds);
    }

    public static Vector3 ClampToRoom(Vector3 position)
    {
        if (!hasPlayableBounds) return position;
        position.x = Mathf.Clamp(position.x, playableBounds.min.x, playableBounds.max.x);
        position.z = Mathf.Clamp(position.z, playableBounds.min.z, playableBounds.max.z);
        return position;
    }

    private bool TryGetNavigationBounds(out Bounds bounds)
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices == null || triangulation.vertices.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
        foreach (Vector3 vertex in triangulation.vertices)
            bounds.Encapsulate(vertex);

        bounds.min = new Vector3(bounds.min.x, 0f, bounds.min.z);
        bounds.max = new Vector3(bounds.max.x, RoomHeight, bounds.max.z);
        bounds.Expand(0.8f);
        return bounds.size.x > 2f && bounds.size.z > 2f;
    }

    private void CreateCeiling(Bounds bounds)
    {
        GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ceiling.name = "Case01_Ceiling";
        ceiling.transform.SetParent(transform, false);
        ceiling.transform.position = new Vector3(bounds.center.x, RoomHeight, bounds.center.z);
        ceiling.transform.localScale = new Vector3(bounds.size.x, 0.16f, bounds.size.z);

        Renderer renderer = ceiling.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            renderer.sharedMaterial.color = new Color(0.12f, 0.14f, 0.17f, 1f);
        }
    }

    private void CreateBoundaryCollider(string objectName, Vector3 position, Vector3 size)
    {
        GameObject boundary = new GameObject(objectName);
        boundary.transform.SetParent(transform, false);
        boundary.transform.position = position;
        BoxCollider collider = boundary.AddComponent<BoxCollider>();
        collider.size = size;
    }

    private void CreateLockedDoor(Bounds bounds)
    {
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = "Case01_LockedExitDoor";
        door.transform.SetParent(transform, false);
        door.transform.position = new Vector3(bounds.center.x, 2.2f, bounds.max.z - WallThickness);
        door.transform.localScale = new Vector3(2.1f, 4.4f, 0.22f);

        Renderer renderer = door.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            renderer.sharedMaterial.color = new Color(0.18f, 0.2f, 0.23f, 1f);
        }
    }
}
