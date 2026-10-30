using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AutoAssignCaseMusic
{
    static AutoAssignCaseMusic()
    {
        EditorApplication.delayCall += AssignIfCaseScene;
    }

    private static void AssignIfCaseScene()
    {
        if (Application.isPlaying) return;
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || (!scene.name.Contains("Case01") && !scene.name.Contains("Case02"))) return;

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/universfield-noir-detective-tension-528072.mp3");
        if (clip == null) return;

        GameObject musicObject = GameObject.Find("GameplayMusic");
        if (musicObject == null)
        {
            musicObject = new GameObject("GameplayMusic");
            Undo.RegisterCreatedObjectUndo(musicObject, "Add Gameplay Music");
        }

        var music = musicObject.GetComponent<GameplayMusicController>();
        if (music == null) music = musicObject.AddComponent<GameplayMusicController>();
        var source = musicObject.GetComponent<AudioSource>();
        if (source == null) source = musicObject.AddComponent<AudioSource>();

        SerializedObject serializedMusic = new SerializedObject(music);
        serializedMusic.FindProperty("musicClip").objectReferenceValue = clip;
        serializedMusic.ApplyModifiedPropertiesWithoutUndo();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0.28f;
        EditorUtility.SetDirty(musicObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
