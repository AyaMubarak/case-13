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

            if (mf.sharedMesh != null)
            {
                mf.gameObject.AddComponent<MeshCollider>();
                added++;
            }
        }

        Debug.Log($"تم تأمين المبنى وإضافة {added} كوليدر لجدران وأثاث المحطة بأمان تام!");
    }
}