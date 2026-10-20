#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CaseSwitchHelper
{
    [MenuItem("Cases/Open Case 01 (The Midnight Murder)")]
    public static void OpenCase1()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Case01_Main.unity");
    }

    [MenuItem("Cases/Open Case 02 (The Cypher Vault Protocol)")]
    public static void OpenCase2()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Case02_Main.unity");
    }

    [MenuItem("Cases/Transform Case 02 into Sci-Fi Lab")]
    public static void TransformCase2ToLab()
    {
        Case2LabSetup.RunFullSetup();
    }

    [MenuItem("Cases/Open Main Menu")]
    public static void OpenMainMenu()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
    }
}
#endif
