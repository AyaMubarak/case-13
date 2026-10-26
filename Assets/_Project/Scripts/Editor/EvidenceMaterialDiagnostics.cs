#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class EvidenceMaterialDiagnostics
{
    [MenuItem("Cases/Fix and Upgrade Evidence Materials to URP")]
    public static void FixEvidenceMaterials()
    {
        string[] evidenceModelPaths = new string[]
        {
            "Assets/_Project/Prefabs/notepad_with_button.glb",
            "Assets/_Project/Prefabs/coffee_mug_school_project.glb",
            "Assets/_Project/Prefabs/flash_drive.glb",
            "Assets/_Project/Prefabs/cassette_audio_k7.glb",
            "Assets/_Project/Prefabs/dc5d0493730f46288471aa7983c806dd.glb"
        };

        Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLitShader == null) urpLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpLitShader == null) urpLitShader = Shader.Find("Standard");

        string materialsFolder = "Assets/_Project/Materials/EvidenceMaterials";
        if (!AssetDatabase.IsValidFolder(materialsFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }
            AssetDatabase.CreateFolder("Assets/_Project/Materials", "EvidenceMaterials");
        }

        foreach (var modelPath in evidenceModelPaths)
        {
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
            if (allAssets == null || allAssets.Length == 0)
            {
                Debug.LogWarning($"[MaterialFixer] Could not load assets at {modelPath}");
                continue;
            }

            Debug.Log($"<color=cyan>[MaterialFixer] Checking: {Path.GetFileName(modelPath)} (Found {allAssets.Length} sub-assets)</color>");

            // Look for Textures and Materials in the GLB
            List<Texture2D> textures = new List<Texture2D>();
            List<Material> internalMats = new List<Material>();

            foreach (var asset in allAssets)
            {
                if (asset is Texture2D tex && !tex.name.Contains("preview"))
                {
                    textures.Add(tex);
                    Debug.Log($"   -> Found Texture: {tex.name} ({tex.width}x{tex.height})");
                }
                else if (asset is Material m)
                {
                    internalMats.Add(m);
                    Debug.Log($"   -> Found Material: {m.name} with shader: {(m.shader != null ? m.shader.name : "null")}");
                }
            }

            // Create or upgrade external URP material for each internal material
            string modelName = Path.GetFileNameWithoutExtension(modelPath);
            int matIndex = 0;

            foreach (var internalMat in internalMats)
            {
                string matPath = $"{materialsFolder}/{modelName}_{internalMat.name}.mat";
                Material urpMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (urpMat == null)
                {
                    urpMat = new Material(urpLitShader);
                    AssetDatabase.CreateAsset(urpMat, matPath);
                }
                else
                {
                    urpMat.shader = urpLitShader;
                }

                // Copy color and texture
                Texture baseTexture = internalMat.mainTexture;
                if (baseTexture == null && textures.Count > 0)
                {
                    // Pick base texture (usually the largest or first RGB texture)
                    baseTexture = textures[Mathf.Min(matIndex, textures.Count - 1)];
                }

                if (baseTexture != null)
                {
                    urpMat.SetTexture("_BaseMap", baseTexture);
                    urpMat.SetTexture("_MainTex", baseTexture);
                    Debug.Log($"   [SUCCESS] Assigned texture '{baseTexture.name}' to URP material '{urpMat.name}'");
                }

                // Copy color
                if (internalMat.HasProperty("_Color"))
                {
                    urpMat.SetColor("_BaseColor", internalMat.color);
                    urpMat.SetColor("_Color", internalMat.color);
                }
                else if (internalMat.HasProperty("_BaseColor"))
                {
                    urpMat.SetColor("_BaseColor", internalMat.GetColor("_BaseColor"));
                }
                else
                {
                    urpMat.SetColor("_BaseColor", Color.white);
                }

                // Enable two-sided if needed
                urpMat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

                EditorUtility.SetDirty(urpMat);
                matIndex++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Now assign these new URP materials to the Evidence GameObjects in the active scene!
        AssignMaterialsToSceneEvidence(materialsFolder);

        EditorUtility.DisplayDialog("Evidence Materials Upgraded", 
            "All Evidence models have been analyzed and converted to Universal Render Pipeline (URP Lit) materials!\n\n" +
            "Colors and textures are now active and visible in the scene.", "OK");
    }

    private static void AssignMaterialsToSceneEvidence(string materialsFolder)
    {
        var evidenceObjects = Object.FindObjectsByType<Evidence>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ev in evidenceObjects)
        {
            var renderers = ev.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                // Match material by name
                string[] guids = AssetDatabase.FindAssets("t:Material", new[] { materialsFolder });
                Material[] newMats = new Material[r.sharedMaterials.Length];
                bool assigned = false;

                for (int i = 0; i < r.sharedMaterials.Length; i++)
                {
                    Material cur = r.sharedMaterials[i];
                    string targetName = cur != null ? cur.name : ev.name;

                    foreach (var g in guids)
                    {
                        string p = AssetDatabase.GUIDToAssetPath(g);
                        Material candidate = AssetDatabase.LoadAssetAtPath<Material>(p);
                        if (candidate != null && (candidate.name.Contains(targetName) || p.ToLower().Contains(ev.name.ToLower())))
                        {
                            newMats[i] = candidate;
                            assigned = true;
                            break;
                        }
                    }

                    if (newMats[i] == null && guids.Length > 0)
                    {
                        // Fallback to first material matching model
                        foreach (var g in guids)
                        {
                            string p = AssetDatabase.GUIDToAssetPath(g);
                            if (p.ToLower().Contains(r.name.ToLower()) || p.ToLower().Contains(ev.name.ToLower()))
                            {
                                newMats[i] = AssetDatabase.LoadAssetAtPath<Material>(p);
                                assigned = true;
                                break;
                            }
                        }
                    }

                    if (newMats[i] == null) newMats[i] = r.sharedMaterials[i];
                }

                if (assigned)
                {
                    Undo.RecordObject(r, "Update Evidence Material");
                    r.sharedMaterials = newMats;
                    EditorUtility.SetDirty(r);
                    Debug.Log($"[MaterialFixer] Updated Renderer '{r.name}' on Evidence '{ev.name}' with URP Material!");
                }
            }
        }
    }
}
#endif
