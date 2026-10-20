using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class AutoRunSuspectSetup
{
    static AutoRunSuspectSetup()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("02")) return;

        GameObject victor = GameObject.Find("NPC_Victor");
        if (victor != null)
        {
            Transform visual = victor.transform.Find("Character_Visual");
            if (visual == null)
            {
                Debug.Log("<color=cyan>[AutoSetup] NPC_Victor has no 3D mesh. Running SetupNewSuspectModels...</color>");
                Case2LabSetup.SetupNewSuspectModels(false);
            }
        }
    }
}
