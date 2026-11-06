using UnityEngine;

public class SafeEnvironmentColliders : MonoBehaviour
{
    [ContextMenu("إضافة كوليدرات للبيئة بأمان")]
    public void AddCollidersSafely()
    {
        // جلب الـ Meshes التابعة لهذا المبنى فقط
        MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);
        int added = 0;

        foreach (MeshFilter mf in filters)
        {
            // شروط الأمان: تجاهل أي عظام أو موديلات لاعبين وتجاهل ما لديه كوليدر مسبقاً
            if (mf.GetComponent<SkinnedMeshRenderer>() != null) continue;
            if (mf.GetComponent<Collider>() != null) continue;

            if (mf.sharedMesh != null && HasTriangles(mf.sharedMesh))
            {
                mf.gameObject.AddComponent<MeshCollider>();
                added++;
            }
        }

        Debug.Log($"تم تأمين المبنى وإضافة {added} كوليدر لجدران وأثاث المحطة بأمان تام!");
    }

    // حذف أي MeshCollider على شبكة خطوط/نقاط (سبب رسالة: Submesh topology is lines or points)
    [ContextMenu("حذف الكوليدرات التالفة (Lines/Points)")]
    public void RemoveInvalidMeshColliders()
    {
        int removed = 0;
        foreach (MeshCollider mc in FindObjectsByType<MeshCollider>(FindObjectsInactive.Include))
        {
            if (mc.sharedMesh != null && HasTriangles(mc.sharedMesh)) continue;
#if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(mc);
#else
            DestroyImmediate(mc);
#endif
            removed++;
        }
        Debug.Log($"[SafeEnvironmentColliders] Removed {removed} invalid MeshColliders. Save the scene now.");
    }

    private static bool HasTriangles(Mesh mesh)
    {
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            MeshTopology t = mesh.GetTopology(i);
            if (t != MeshTopology.Triangles && t != MeshTopology.Quads) return false;
        }
        return mesh.subMeshCount > 0;
    }
}
