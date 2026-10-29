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
        if (!activeScene.IsValid() || !activeScene.isLoaded || activeScene.rootCount == 0)
            return;
        if (!activeScene.name.Contains("02")) return;

        GameObject victor = GameObject.Find("NPC_Victor");
        bool needsSetup = false;

        GameObject chair = GameObject.Find("Interrogation_Lab_Chair");
        if (chair != null && Mathf.Abs(chair.transform.eulerAngles.y - 270f) > 5f)
        {
            needsSetup = true;
        }

        if (victor == null)
        {
            needsSetup = true;
        }
        else
        {
            Transform visual = victor.transform.Find("Character_Visual");
            if (visual == null)
            {
                needsSetup = true;
            }
            else if (victor.transform.position.x < 1.8f || victor.activeSelf || visual.localScale.x < 0.005f || visual.localScale.x > 50f)
            {
                // Old position on top of the workbench table or currently active at startup
                needsSetup = true;
            }
        }

        if (needsSetup)
        {
            Debug.Log("<color=cyan>[AutoSetup] Calibrating and setting up Suspect Models for Case 02...</color>");
            Case2LabSetup.SetupNewSuspectModels(false);
        }
    }
}
