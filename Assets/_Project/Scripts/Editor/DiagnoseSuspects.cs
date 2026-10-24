#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class DiagnoseSuspects
{
    static DiagnoseSuspects()
    {
        EditorApplication.delayCall += RunDiagnosis;
    }

    [MenuItem("Case 2 Setup/FIX SUSPECTS: On Floor & Hidden Until All Evidences Collected")]
    public static void FixAllSuspectsNow()
    {
        Case2LabSetup.SetupNewSuspectModels(true);
        RunDiagnosis();
    }

    [MenuItem("Case 2 Setup/Diagnose Suspect Visibility & Movement")]
    public static void RunDiagnosis()
    {
        var scene = EditorSceneManager.GetActiveScene();
        Debug.Log($"<color=yellow>=== SUSPECT DIAGNOSTIC REPORT (Scene: {scene.name}) ===</color>");

        // MainMenu intentionally contains no player, suspects, or interrogation
        // manager. Do not report that normal menu scene as a broken gameplay scene.
        if (scene.name == "MainMenu")
        {
            Debug.Log("<color=cyan>[INFO]</color> MainMenu is a UI-only scene; suspect diagnostics are skipped. Open Case02_Main to diagnose suspects.");
            Debug.Log("<color=yellow>=== END DIAGNOSTIC REPORT ===</color>");
            return;
        }

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        Debug.Log($"Total GameObjects in scene: {allObjects.Length}");

        var playerMovement = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (playerMovement != null)
        {
            Debug.Log($"<color=cyan>[Player]</color> Found at position: {playerMovement.transform.position}, Active: {playerMovement.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning("[Player] PlayerMovement NOT FOUND in scene!");
        }

        var mainCam = Camera.main;
        if (mainCam != null)
        {
            Debug.Log($"<color=cyan>[MainCamera]</color> Found at position: {mainCam.transform.position}, Active: {mainCam.gameObject.activeInHierarchy}");
        }

        var sim = Object.FindAnyObjectByType<SeparateInterrogationManager>(FindObjectsInactive.Include);
        if (sim != null)
        {
            Debug.Log($"<color=cyan>[SeparateInterrogationManager]</color> Found. Active: {sim.gameObject.activeInHierarchy}");
            Debug.Log($"  Suspect1: '{(sim.suspect1 != null ? sim.suspect1.suspectName : "null")}', obj: '{(sim.suspect1?.suspectObject != null ? sim.suspect1.suspectObject.name : "null")}'");
            Debug.Log($"  Suspect2: '{(sim.suspect2 != null ? sim.suspect2.suspectName : "null")}', obj: '{(sim.suspect2?.suspectObject != null ? sim.suspect2.suspectObject.name : "null")}'");
            Debug.Log($"  ChairTarget: '{(sim.suspect1?.chairTarget != null ? sim.suspect1.chairTarget.name : "null")}' at {(sim.suspect1?.chairTarget != null ? sim.suspect1.chairTarget.position.ToString() : "none")}");
        }
        else
        {
            Debug.LogWarning("[SeparateInterrogationManager] NOT FOUND in scene!");
        }

        // Look for all suspects
        string[] searchNames = { "victor", "elena", "evan", "nora", "suspect" };
        int suspectsFound = 0;

        foreach (var go in allObjects)
        {
            string lowerName = go.name.ToLower();
            bool isSuspectCandidate = false;
            foreach (var s in searchNames)
            {
                if (lowerName.Contains(s)) { isSuspectCandidate = true; break; }
            }

            if (isSuspectCandidate && (go.GetComponent<SuspectInterrogation>() != null || go.GetComponent<PoliceSceneManager>() != null || lowerName.StartsWith("npc_")))
            {
                suspectsFound++;
                DiagnoseSingleSuspect(go);
            }
        }

        if (suspectsFound == 0)
        {
            Debug.LogError("<color=red>[DIAGNOSIS FAILED]</color> No suspect GameObjects found matching victor/elena/evan/nora with suspect components!");
        }

        Debug.Log("<color=yellow>=== END DIAGNOSTIC REPORT ===</color>");
    }

    private static void DiagnoseSingleSuspect(GameObject go)
    {
        Debug.Log($"<color=orange>--- Checking Suspect: '{go.name}' ---</color>");
        Debug.Log($"  ActiveSelf: {go.activeSelf}, ActiveInHierarchy: {go.activeInHierarchy}");
        Debug.Log($"  Position: {go.transform.position}, Rotation: {go.transform.eulerAngles}, LocalScale: {go.transform.localScale}, LossyScale: {go.transform.lossyScale}");

        var agent = go.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            Debug.Log($"  NavMeshAgent: Enabled={agent.enabled}, IsOnNavMesh={agent.isOnNavMesh}, BaseOffset={agent.baseOffset}, Radius={agent.radius}, Height={agent.height}");
            bool sampled = NavMesh.SamplePosition(go.transform.position, out NavMeshHit hit, 2.0f, NavMesh.AllAreas);
            Debug.Log($"  NavMesh Sample at position (dist 2.0m): {sampled} (Hit pos: {(sampled ? hit.position.ToString() : "N/A")})");
        }
        else
        {
            Debug.LogWarning($"  No NavMeshAgent on '{go.name}'!");
        }

        var col = go.GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"  Collider: Type={col.GetType().Name}, Enabled={col.enabled}, Bounds={col.bounds}");
        }

        var anim = go.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            Debug.Log($"  Animator on '{anim.gameObject.name}': Controller={(anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "null")}, Avatar={(anim.avatar != null ? anim.avatar.name : "null")}");
        }
        else
        {
            Debug.LogWarning($"  No Animator found on '{go.name}' or its children!");
        }

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        Debug.Log($"  Total Renderers on suspect: {renderers.Length}");
        foreach (var r in renderers)
        {
            Debug.Log($"    Renderer on '{r.gameObject.name}': Enabled={r.enabled}, Type={r.GetType().Name}, Bounds={r.bounds.size}, Center={r.bounds.center}");
            if (r.sharedMaterials == null || r.sharedMaterials.Length == 0)
            {
                Debug.LogError($"      MISSING MATERIAL on '{r.gameObject.name}'!");
            }
            else
            {
                for (int m = 0; m < r.sharedMaterials.Length; m++)
                {
                    var mat = r.sharedMaterials[m];
                    if (mat == null)
                    {
                        Debug.LogError($"      Material[{m}] is NULL on '{r.gameObject.name}'!");
                    }
                    else
                    {
                        Debug.Log($"      Material[{m}]: '{mat.name}', Shader='{mat.shader?.name}'");
                    }
                }
            }
        }
    }
}
#endif
