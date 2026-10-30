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
        // This is an Editor-only scene migration tool. Never run it while the
        // game is playing: SetupNewSuspectModels marks/saves the scene and
        // Unity throws InvalidOperationException during Play Mode.
        if (Application.isPlaying)
            return;

        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded || activeScene.rootCount == 0)
            return;
        if (!activeScene.name.Contains("02")) return;

        GameObject victor = GameObject.Find("NPC_Victor");
        bool needsSetup = false;

        // The Case 2 chair is deliberately positioned by hand in the scene.
        // Do not use its rotation as a reason to run the migration again:
        // the old check expected Y=270 and overwrote the designer's Y=180.

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

        // Case 2 has its own interrogation voices. Repair the serialized
        // AudioSource clips whenever this scene is opened, without touching
        // Case 1.
        Case2LabSetup.AssignCase2Voices();
    }
}
