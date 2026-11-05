using UnityEngine;
using UnityEditor;

public static class AutoRunSuspectSetup
{
    [MenuItem("Case 2 Setup/Calibrate Suspect Models & Voices")]
    public static void RunOnce()
    {
        if (Application.isPlaying)
            return;

        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded || activeScene.rootCount == 0)
            return;
        if (!activeScene.name.Contains("02")) return;

        Debug.Log("<color=cyan>[AutoSetup] Calibrating and setting up Suspect Models for Case 02...</color>");
        Case2LabSetup.SetupNewSuspectModels(false);
        Case2LabSetup.AssignCase2Voices();
    }
}
