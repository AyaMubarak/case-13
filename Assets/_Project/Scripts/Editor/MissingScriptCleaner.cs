#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class MissingScriptCleaner
{
    [MenuItem("Tools/Detect & Clean Missing Scripts")]
    public static void CleanMissingScripts()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int totalMissingCount = 0;
        int objectsWithMissingCount = 0;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Clean Missing Scripts");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (GameObject go in allObjects)
        {
            int missingInGo = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (missingInGo > 0)
            {
                Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                totalMissingCount += missingInGo;
                objectsWithMissingCount++;
                Debug.Log($"<color=yellow>[MissingScriptCleaner]</color> Cleaned {missingInGo} missing script(s) from '{go.name}'", go);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        if (totalMissingCount > 0)
        {
            EditorUtility.DisplayDialog("Missing Scripts Cleaned", 
                $"Successfully found and cleaned {totalMissingCount} missing script(s) across {objectsWithMissingCount} GameObject(s) in the current scene.", "OK");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        else
        {
            EditorUtility.DisplayDialog("Clean Scene", "No missing scripts were found in the active scene!", "OK");
        }
    }
}
#endif
