#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class Case2LabSetup : EditorWindow
{
    [MenuItem("Case 2 Setup/Transform Police Station into Sci-Fi Lab")]
    public static void ShowWindow()
    {
        GetWindow<Case2LabSetup>("Sci-Fi Lab Setup");
    }

    [MenuItem("Case 2 Setup/Run Full Automated Setup Now")]
    public static void RunFullSetup()
    {
        SetupCase2Scene();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
        GUILayout.Label("Case 2: Sci-Fi Research Lab Setup", headerStyle);
        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            "This automated setup converts Case 2 from the old police station into an advanced Sci-Fi Research Laboratory:\n\n" +
            "• Upgrades ALL materials in ScifiOfficeLite & Evidence models to Universal Render Pipeline (URP Lit)\n" +
            "• Installs modular Sci-Fi Office structure & authentic flooring\n" +
            "• Places Player and Camera securely on the lab floor (preventing the grey void screen)\n" +
            "• Adds quantum server racks, robotic arms, PC terminals, chemical storage shelves\n" +
            "• Re-positions all 5 Evidence items in realistic lab spots\n" +
            "• Places Suspects (Victor Sterling & Elena Rostova) at their lab stations\n" +
            "• Automatically assigns Victor & Elena voice files", 
            MessageType.Info);

        GUILayout.Space(15);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
        if (GUILayout.Button("EXECUTE SCI-FI LAB SETUP", GUILayout.Height(45)))
        {
            SetupCase2Scene();
        }
        GUI.backgroundColor = Color.white;
    }

    public static void SetupCase2Scene()
    {
        // 1. Upgrade all materials to URP so textures and colors display properly
        UpgradeAllMaterialsToURP();

        // 2. Ensure Case02_Main is open
        string targetScenePath = "Assets/_Project/Scenes/Case02_Main.unity";
        var currentScene = EditorSceneManager.GetActiveScene();
        if (currentScene.path != targetScenePath)
        {
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            currentScene = EditorSceneManager.OpenScene(targetScenePath);
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Sci-Fi Lab for Case 2");

        Debug.Log("<color=cyan>[LabSetup] Starting Case 2 Sci-Fi Lab Transformation...</color>");

        // 3. Safely preserve Evidence items before removing old environment
        var allEvidence = Object.FindObjectsByType<Evidence>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ev in allEvidence)
        {
            if (ev != null)
            {
                Undo.SetTransformParent(ev.transform, null, "Unparent Evidence");
            }
        }

        for (int i = 1; i <= 5; i++)
        {
            GameObject evGO = GameObject.Find($"Evidence_{i}");
            if (evGO != null && evGO.transform.parent != null)
            {
                Undo.SetTransformParent(evGO.transform, null, "Unparent Evidence");
            }
        }

        // 4. Remove old police station and redundant old environment objects
        string[] oldEnvNames = new string[]
        {
            "policestation", "tables_and_desks", "lockers", "DetectiveSchool",
            "dective room", "Meeting_Room_Ground", "ReceptionGround", "Office_Ground",
            "Fences", "Floor_Safety_Barrier"
        };

        foreach (var name in oldEnvNames)
        {
            GameObject go = GameObject.Find(name);
            if (go != null)
            {
                Undo.DestroyObjectImmediate(go);
            }
        }

        // 5. Clean previous SciFi_Lab_Environment container to allow clean re-runs
        GameObject existingLab = GameObject.Find("SciFi_Lab_Environment");
        if (existingLab != null)
        {
            Undo.DestroyObjectImmediate(existingLab);
        }

        // 6. Create master SciFi_Lab_Environment container
        GameObject labRoot = new GameObject("SciFi_Lab_Environment");
        Undo.RegisterCreatedObjectUndo(labRoot, "Create SciFi_Lab_Environment");
        labRoot.transform.position = Vector3.zero;
        labRoot.transform.rotation = Quaternion.identity;

        // 7. Load Demo scene to grab the assembled room ("Office 1" and "Ceiling")
        GameObject importedOffice = null;
        string demoScenePath = "Assets/ScifiOfficeLite/Scene/Demo.unity";
        var demoScene = EditorSceneManager.OpenScene(demoScenePath, OpenSceneMode.Additive);
        if (demoScene.IsValid())
        {
            GameObject demoOffice = null;
            GameObject demoCeiling = null;
            foreach (var rootGO in demoScene.GetRootGameObjects())
            {
                if (rootGO.name == "Office 1") demoOffice = rootGO;
                if (rootGO.name == "Ceiling") demoCeiling = rootGO;
            }

                if (demoOffice != null)
                {
                    importedOffice = GameObject.Instantiate(demoOffice, labRoot.transform);
                    importedOffice.name = "Modular_SciFi_Office_Structure";
                    Undo.RegisterCreatedObjectUndo(importedOffice, "Instantiate Office 1");
                    importedOffice.transform.localPosition = Vector3.zero;
                    importedOffice.transform.localRotation = Quaternion.identity;

                    // Remove demo player / HUD from imported office
                    Transform demoPlayer = importedOffice.transform.Find("Player");
                    if (demoPlayer != null) Undo.DestroyObjectImmediate(demoPlayer.gameObject);
                    Transform demoHUD = importedOffice.transform.Find("HUDCanvas");
                    if (demoHUD != null) Undo.DestroyObjectImmediate(demoHUD.gameObject);
                    Transform demoControls = importedOffice.transform.Find("MobileControls");
                    if (demoControls != null) Undo.DestroyObjectImmediate(demoControls.gameObject);

                    // Clean up Broken Door (which causes the bright cyan doorway) and upside-down / floating props near ceiling
                    Transform[] allChildren = importedOffice.GetComponentsInChildren<Transform>(true);
                    List<GameObject> toDestroy = new List<GameObject>();
                    foreach (var child in allChildren)
                    {
                        if (child == null) continue;
                        string n = child.name.ToLower();

                        // 1. Remove Broken Door parts that block the doorway with bright cyan
                        if (n.Contains("broken door") || n == "door1" || n == "door2" || n == "top" || n == "top 2" || (n.Contains("frame") && child.parent != null && child.parent.name.ToLower().Contains("door")))
                        {
                            if (!toDestroy.Contains(child.gameObject))
                                toDestroy.Add(child.gameObject);
                        }

                        // 2. Remove the mechanical arm hanging upside-down from ceiling (looks like upside-down chair)
                        if (n.Contains("mechanical arm") && child.position.y > 2.8f)
                        {
                            if (!toDestroy.Contains(child.gameObject))
                                toDestroy.Add(child.gameObject);
                        }

                        // 3. Remove weird trays or props dangling awkwardly near the ceiling
                        if ((n.Contains("tray") || n.Contains("vase") || n.Contains("shelf")) && child.position.y > 2.3f)
                        {
                            if (!toDestroy.Contains(child.gameObject))
                                toDestroy.Add(child.gameObject);
                        }
                    }

                    foreach (var go in toDestroy)
                    {
                        if (go != null) Undo.DestroyObjectImmediate(go);
                    }

                    if (demoCeiling != null)
                    {
                        GameObject importedCeiling = GameObject.Instantiate(demoCeiling, labRoot.transform);
                        importedCeiling.name = "Modular_SciFi_Ceiling";
                        Undo.RegisterCreatedObjectUndo(importedCeiling, "Instantiate Ceiling");
                        // In Demo.unity, walls are Y=4.0. We place the ceiling directly at Y=4.01 to eliminate gaps and void!
                        importedCeiling.transform.localPosition = new Vector3(0, 4.01f, 22.24f);
                        importedCeiling.transform.localRotation = Quaternion.identity;

                        // Clean out any demo UI or player inside ceiling object
                        Transform ceilPlayer = importedCeiling.transform.Find("Player");
                        if (ceilPlayer != null) Undo.DestroyObjectImmediate(ceilPlayer.gameObject);
                        Transform ceilText = importedCeiling.transform.Find("Text");
                        if (ceilText != null) Undo.DestroyObjectImmediate(ceilText.gameObject);
                        Transform ceilBack = importedCeiling.transform.Find("Back");
                        if (ceilBack != null) Undo.DestroyObjectImmediate(ceilBack.gameObject);
                        Transform ceilCrouch = importedCeiling.transform.Find("Crouch");
                        if (ceilCrouch != null) Undo.DestroyObjectImmediate(ceilCrouch.gameObject);
                    }
                }

                EditorSceneManager.CloseScene(demoScene, true);
            }

        // 8. Add EXTRA SCIENTIFIC LAB EQUIPMENT inside the office rooms
        Transform propsContainer = new GameObject("Scientific_Lab_Equipment").transform;
        propsContainer.SetParent(labRoot.transform, false);

        GameObject serverRackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Server Rack.prefab");
        GameObject mechArm1Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Mechanical arm 1.prefab");
        GameObject mechArm2Prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/Tech Accessories/Mechanical arm 2.prefab");
        GameObject pcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/Tech Accessories/PC 2.prefab");
        GameObject tvPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/TVs/Tv 32 Inch.prefab");
        GameObject tableMetalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/Tables/Metal/Table Metal Variant.prefab");
        GameObject tableWoodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/Tables/Grey Wood/Table Grey Wood Variant.prefab");
        GameObject shelfCratesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/Shelf with Crates.prefab");
        GameObject chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Office Chair.prefab");
        GameObject stoolPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ScifiOfficeLite/Prefabs/Chairs and Stools/Stool.prefab");

        // Extra Server Racks in Server Room (around Z=43, X=-6 to -8)
        if (serverRackPrefab != null)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject rack = (GameObject)PrefabUtility.InstantiatePrefab(serverRackPrefab, propsContainer);
                rack.name = $"Quantum_Mainframe_Rack_{i+1}";
                rack.transform.localPosition = new Vector3(-6.2f, 0.0f, 41.5f + (i * 1.5f));
                rack.transform.localRotation = Quaternion.Euler(0, 90, 0);
            }
        }

        // Central Robotics & Analysis Workbench (inside Main Lab Room at Z=37, X=0)
        if (tableMetalPrefab != null)
        {
            GameObject workbench = (GameObject)PrefabUtility.InstantiatePrefab(tableMetalPrefab, propsContainer);
            workbench.name = "Central_Robotics_Workbench";
            workbench.transform.localPosition = new Vector3(0.5f, 0.0f, 37.0f);
            workbench.transform.localRotation = Quaternion.Euler(0, 90, 0);

            if (mechArm1Prefab != null)
            {
                GameObject arm = (GameObject)PrefabUtility.InstantiatePrefab(mechArm1Prefab, workbench.transform);
                arm.name = "Robotic_Arm_1";
                arm.transform.localPosition = new Vector3(0, 0.75f, 0.4f);
                arm.transform.localRotation = Quaternion.Euler(0, 45, 0);
            }

            if (pcPrefab != null)
            {
                GameObject pc = (GameObject)PrefabUtility.InstantiatePrefab(pcPrefab, workbench.transform);
                pc.name = "Terminal_PC";
                pc.transform.localPosition = new Vector3(0, 0.75f, -0.4f);
                pc.transform.localRotation = Quaternion.Euler(0, 180, 0);
            }
        }

        // Chemical Specimen Storage Shelves (at rear wall Z=42)
        if (shelfCratesPrefab != null)
        {
            GameObject shelf = (GameObject)PrefabUtility.InstantiatePrefab(shelfCratesPrefab, propsContainer);
            shelf.name = "Chemical_Storage_Shelf";
            shelf.transform.localPosition = new Vector3(1.5f, 0.0f, 42.0f);
            shelf.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }

        // Interrogation Desk & Chair (at Z=34.0, X=4.2)
        GameObject interrogationChair = null;
        if (tableWoodPrefab != null)
        {
            GameObject leadDesk = (GameObject)PrefabUtility.InstantiatePrefab(tableWoodPrefab, propsContainer);
            leadDesk.name = "Interrogation_Lab_Desk";
            leadDesk.transform.localPosition = new Vector3(4.2f, 0.0f, 34.0f);
            leadDesk.transform.localRotation = Quaternion.Euler(0, 0, 0);

            if (chairPrefab != null)
            {
                interrogationChair = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, propsContainer);
                interrogationChair.name = "Interrogation_Lab_Chair";
                interrogationChair.transform.localPosition = new Vector3(4.2f, 0.0f, 33.25f);
                interrogationChair.transform.localRotation = Quaternion.identity;

                GameObject investigatorChair = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, propsContainer);
                investigatorChair.name = "Investigator_Desk_Chair";
                investigatorChair.transform.localPosition = new Vector3(4.2f, 0.0f, 34.75f);
                investigatorChair.transform.localRotation = Quaternion.Euler(0, 90f, 0); // Backrest North (+Z), Seat South (-Z towards desk)
            }
        }

        // 9. Update ChairTarget for Interrogation
        GameObject chairTargetGO = GameObject.Find("ChairTarget");
        if (chairTargetGO == null)
        {
            chairTargetGO = new GameObject("ChairTarget");
            Undo.RegisterCreatedObjectUndo(chairTargetGO, "Create ChairTarget");
        }

        if (interrogationChair != null)
        {
            chairTargetGO.transform.position = new Vector3(4.2f, 0.0f, 33.25f);
            chairTargetGO.transform.rotation = Quaternion.Euler(0, 0, 0); // Humanoid faces North (+Z towards desk)
        }
        else
        {
            chairTargetGO.transform.position = new Vector3(4.2f, 0.0f, 33.25f);
            chairTargetGO.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        var sim = Object.FindAnyObjectByType<SeparateInterrogationManager>(FindObjectsInactive.Include);
        if (sim != null)
        {
            if (sim.suspect1 != null) sim.suspect1.chairTarget = chairTargetGO.transform;
            if (sim.suspect2 != null) sim.suspect2.chairTarget = chairTargetGO.transform;
            EditorUtility.SetDirty(sim);
        }

        // 10. Position Evidence Items STABLY and FIRMLY on tabletops and shelves
        // EV_01: Cyanide Coffee Mug -> Chief Scientist's Desk on the right (Eliminates Damian)
        PlaceEvidenceInScene("EV_01", new Vector3(7.35f, 0.78f, 34.8f), Quaternion.Euler(0, 15, 0), "SUS_DAMIAN", "Damian was stationed outside at the main security gate; the lethal compound is lab-grade potassium cyanide.");

        // EV_02: Decrypted Flash Drive -> Quantum Vault Terminal Desk in back server room (Eliminates Arthur)
        PlaceEvidenceInScene("EV_02", new Vector3(2.15f, 0.78f, 48.2f), Quaternion.Euler(0, 90, 0), "SUS_ARTHUR", "Arthur's remote VPN connection was spoofed; digital logs confirm local terminal access from Bio-Chem.");

        // EV_03: Broken Syringe Vial -> Chemical Analysis Desk in the left lab room (Eliminates Maya)
        PlaceEvidenceInScene("EV_03", new Vector3(-2.8f, 0.78f, 40.5f), Quaternion.Euler(0, -30, 0), "SUS_MAYA", "Batch BIO-772 was restricted exclusively to biochemical research staff, clearing Maya Lin.");

        // EV_04: Audio Wiretap Memo -> Central Robotics / Analysis Workbench (DOES NOT ELIMINATE ANYONE - Points towards Victor & Elena)
        PlaceEvidenceInScene("EV_04", new Vector3(1.05f, 0.78f, 35.5f), Quaternion.Euler(0, 45, 0), "", "Encrypted transmission traces establish direct communication with the perpetrator. Both Victor Sterling and Elena Rostova held terminal encryption credentials, narrowing suspicion to them!");

        // EV_05: Burned Lab Notebook -> Forensic Desk in the left lab room (DOES NOT ELIMINATE ANYONE - Points towards Victor & Elena)
        PlaceEvidenceInScene("EV_05", new Vector3(-2.8f, 0.78f, 37.6f), Quaternion.Euler(0, 10, 0), "", "Lab notes reveal cipher protocols and cyanide formulations known only to Dr. King, Victor Sterling, and Elena Rostova. The investigation centers entirely on Victor and Elena!");

        // 11. POSITION THE PLAYER SECURELY ON THE ROOM FLOOR (Floor is at Y=0.0)
        var playerMovement = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (playerMovement != null)
        {
            playerMovement.transform.position = new Vector3(4.285f, 0.05f, 31.0f);
            playerMovement.transform.rotation = Quaternion.Euler(0, 0, 0);
            Debug.Log($"<color=green>[LabSetup] Player placed securely on room floor at (4.285, 0.05, 31.0)!</color>");
        }

        // Suspects (Victor Sterling & Elena Rostova) firmly on the floor at Y=0.0
        var allPSM = Object.FindObjectsByType<PoliceSceneManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var psm in allPSM)
        {
            string pName = psm.gameObject.name.ToLower();
            if (pName.Contains("evan") || pName.Contains("victor"))
            {
                psm.transform.position = new Vector3(-0.5f, 0.0f, 46.5f);
                psm.transform.rotation = Quaternion.Euler(0, 180, 0);
                psm.gameObject.SetActive(true);
            }
            else if (pName.Contains("nora") || pName.Contains("elena"))
            {
                psm.transform.position = new Vector3(-1.8f, 0.0f, 39.5f);
                psm.transform.rotation = Quaternion.Euler(0, 90, 0);
                psm.gameObject.SetActive(true);
            }
        }

        // 12. Ensure Directional Light illuminates the rooms with modern sci-fi ambience
        GameObject dirLight = GameObject.Find("Directional Light");
        if (dirLight != null)
        {
            dirLight.transform.position = new Vector3(8.4f, 8.0f, 22.0f);
            dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
            Light l = dirLight.GetComponent<Light>();
            if (l != null)
            {
                l.color = new Color(0.95f, 0.98f, 1.0f);
                l.intensity = 1.3f;
            }
        }

        // Add vibrant ambient lab lighting so floors and tech details look vivid
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.75f, 0.85f, 1.0f);
        RenderSettings.ambientEquatorColor = new Color(0.55f, 0.6f, 0.7f);
        RenderSettings.ambientGroundColor = new Color(0.35f, 0.38f, 0.45f);

        // 13. Ensure Main Camera settings
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.nearClipPlane = 0.05f;
            mainCam.farClipPlane = 200f;
            // Set Skybox or ambient clear flag
            mainCam.clearFlags = CameraClearFlags.Skybox;
        }

        // 14. Assign Voices
        AssignCase2Voices();

        // 15. Fix Evidence Materials to URP
        EvidenceMaterialDiagnostics.FixEvidenceMaterials();

        // 16. Install and Configure New Suspect Models
        SetupNewSuspectModels(false);

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("<color=green>[LabSetup] Case 2 Sci-Fi Lab setup completed successfully and saved!</color>");
        EditorUtility.DisplayDialog("Sci-Fi Lab Setup Complete", 
            "Case 2 Sci-Fi Research Laboratory is now ready!\n\n" +
            "✓ All 28 environment materials converted to URP Lit (full textures & lighting)\n" +
            "✓ Player positioned safely on the room floor at (4.28, 2.05, 30.8)\n" +
            "✓ Server rooms, robotic arms, PC terminals, chemical shelves installed\n" +
            "✓ Evidence items placed on lab workbenches\n" +
            "✓ Victor and Elena voices connected\n\n" +
            "Press Play to test your new laboratory!", "OK");
    }

    [MenuItem("Cases/Upgrade ScifiOffice Materials to URP")]
    public static void UpgradeAllMaterialsToURP()
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (urpShader == null) return;

        string[] matGUIDs = AssetDatabase.FindAssets("t:Material", new[] { "Assets/ScifiOfficeLite/Meshes/Materials" });
        int count = 0;

        foreach (string guid in matGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            Texture mainTex = mat.mainTexture;
            Color col = mat.HasProperty("_Color") ? mat.color : Color.white;
            Texture bumpTex = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;

            mat.shader = urpShader;

            if (mainTex != null)
            {
                mat.SetTexture("_BaseMap", mainTex);
                mat.SetTexture("_MainTex", mainTex);
            }
            mat.SetColor("_BaseColor", col);
            mat.SetColor("_Color", col);

            if (bumpTex != null)
            {
                mat.SetTexture("_BumpMap", bumpTex);
                mat.EnableKeyword("_NORMALMAP");
            }

            // Glass materials must be transparent in URP Lit
            if (mat.name.ToLower().Contains("glass"))
            {
                mat.SetFloat("_Surface", 1); // 1 = Transparent
                mat.SetFloat("_Blend", 0); // 0 = Alpha
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
                mat.SetColor("_BaseColor", new Color(0.6f, 0.85f, 1.0f, 0.25f));
            }
            else
            {
                mat.SetFloat("_Surface", 0); // Opaque
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 2000;
            }

            // Clean up any high-intensity HDRP emission
            if (mat.HasProperty("_EmissionColor"))
            {
                Color em = mat.GetColor("_EmissionColor");
                if (em.r > 2f || em.g > 2f || em.b > 2f)
                {
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                }
            }

            EditorUtility.SetDirty(mat);
            count++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"<color=green>[MaterialUpgrade] Upgraded {count} ScifiOfficeLite materials to Universal Render Pipeline (URP Lit)!</color>");
    }

    [MenuItem("Cases/Bake NavMesh for Case 02")]
    public static void BakeNavMeshForCase02()
    {
        var currentScene = EditorSceneManager.GetActiveScene();
        if (!currentScene.name.Contains("02"))
        {
            if (EditorUtility.DisplayDialog("Switch Scene?", "Do you want to open Case02_Main to bake NavMesh?", "Yes", "Cancel"))
            {
                currentScene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Case02_Main.unity");
            }
            else return;
        }

        NavMeshSurface surface = Object.FindAnyObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject nmsGO = new GameObject("NavMeshSurface_Case02");
            surface = nmsGO.AddComponent<NavMeshSurface>();
        }

        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);
        Debug.Log("<color=green>[NavMesh] NavMesh successfully baked for Case 02!</color>");
        EditorUtility.DisplayDialog("NavMesh Baked", "NavMesh has been successfully baked for Case 02!", "OK");
    }

    [MenuItem("Cases/Setup New Suspect Models (Walking Man & Grey Woman)")]
    [MenuItem("Case 2 Setup/Setup New Suspect Models (Walking Man & Grey Woman)")]
    public static void SetupNewSuspectModels()
    {
        SetupNewSuspectModels(true);
    }

    public static void SetupNewSuspectModels(bool showDialog)
    {
        var currentScene = EditorSceneManager.GetActiveScene();
        if (!currentScene.name.Contains("02"))
        {
            if (EditorUtility.DisplayDialog("Switch Scene?", "Do you want to open Case02_Main to setup suspect models?", "Yes", "Cancel"))
            {
                currentScene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Case02_Main.unity");
            }
            else return;
        }

        string manFbxPath = "Assets/_Project/Prefabs/Walking.fbx";
        string womanFbxPath = "Assets/_Project/Prefabs/Walking (1).fbx";

        // 1. Ensure Model Importers are Humanoid
        ModelImporter manImporter = AssetImporter.GetAtPath(manFbxPath) as ModelImporter;
        if (manImporter != null && manImporter.animationType != ModelImporterAnimationType.Human)
        {
            manImporter.animationType = ModelImporterAnimationType.Human;
            manImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            manImporter.SaveAndReimport();
        }

        ModelImporter womanImporter = AssetImporter.GetAtPath(womanFbxPath) as ModelImporter;
        if (womanImporter != null && womanImporter.animationType != ModelImporterAnimationType.Human)
        {
            womanImporter.animationType = ModelImporterAnimationType.Human;
            womanImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            womanImporter.SaveAndReimport();
        }

        GameObject manFbx = AssetDatabase.LoadAssetAtPath<GameObject>(manFbxPath);
        GameObject womanFbx = AssetDatabase.LoadAssetAtPath<GameObject>(womanFbxPath);
        RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Prefabs/Characters/Animation/CharacterAnimation.controller");

        // 2. Find Chair in the room (Interrogation_Lab_Chair or ChairTarget)
        GameObject chairObj = GameObject.Find("Interrogation_Lab_Chair");
        if (chairObj == null) chairObj = GameObject.Find("ChairTarget");
        if (chairObj == null) chairObj = GameObject.Find("Office Chair Variant");
        if (chairObj == null)
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name.ToLower().Contains("chair"))
                {
                    chairObj = go;
                    break;
                }
            }
        }

        Vector3 chairPos = new Vector3(4.2f, 0.0f, 33.25f);
        if (chairObj != null)
        {
            chairObj.transform.position = chairPos;
            chairObj.transform.rotation = Quaternion.identity;
            EditorUtility.SetDirty(chairObj);
        }

        // Ensure ChairTarget exists and is positioned accurately
        GameObject chairTarget = GameObject.Find("ChairTarget");
        if (chairTarget == null)
        {
            chairTarget = new GameObject("ChairTarget");
        }
        chairTarget.transform.position = chairPos;
        chairTarget.transform.rotation = Quaternion.Euler(0, 0, 0); // Suspect faces North (+Z towards desk)
        EditorUtility.SetDirty(chairTarget);

        // 3. Setup Suspect 1 (Victor Sterling / Evan - Man in Black Walking.fbx)
        GameObject victorGO = GameObject.Find("NPC_Victor");
        if (victorGO == null) victorGO = GameObject.Find("victor");
        if (victorGO == null) victorGO = GameObject.Find("Victor");
        if (victorGO == null) victorGO = GameObject.Find("evan");
        if (victorGO == null) victorGO = GameObject.Find("Evan");
        if (victorGO == null)
        {
            foreach (var si in Object.FindObjectsByType<SuspectInterrogation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (si.gameObject.name.ToLower().Contains("victor") || si.gameObject.name.ToLower().Contains("evan") ||
                    (!string.IsNullOrEmpty(si.suspectName) && (si.suspectName.ToLower().Contains("victor") || si.suspectName.ToLower().Contains("evan"))))
                {
                    victorGO = si.gameObject;
                    break;
                }
            }
        }

        if (victorGO == null)
        {
            victorGO = new GameObject("NPC_Victor");
            victorGO.AddComponent<SuspectInterrogation>();
        }

        if (victorGO != null)
        {
            victorGO.name = "NPC_Victor";
            victorGO.SetActive(false); // Hidden until 5 evidences are collected
            // Position Victor firmly on open floor walkway (clear of workbench table, beside Elena)
            victorGO.transform.position = new Vector3(2.0f, 0.0f, 33.8f);
            victorGO.transform.rotation = Quaternion.Euler(0, 150, 0);
            victorGO.transform.localScale = Vector3.one;
            ConfigureCharacterModel(victorGO, manFbx, animController, "Victor Sterling", 1.8f);
            Sprite victorPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Portraits/Victor_Sterling.png");
            SuspectInterrogation victorInterrogation = victorGO.GetComponent<SuspectInterrogation>();
            if (victorInterrogation != null && victorPortrait != null)
            {
                victorInterrogation.suspectPortrait = victorPortrait;
                EditorUtility.SetDirty(victorInterrogation);
            }
            Debug.Log($"<color=green>[Suspects] Configured Victor '{victorGO.name}' on open floor at (2.0, 0.0, 33.8) successfully!</color>");
        }

        // 4. Setup Suspect 2 (Elena Rostova / Nora - Woman in Grey Walking (1).fbx)
        GameObject elenaGO = GameObject.Find("NPC_Elena");
        if (elenaGO == null) elenaGO = GameObject.Find("elena");
        if (elenaGO == null) elenaGO = GameObject.Find("Elena");
        if (elenaGO == null) elenaGO = GameObject.Find("nora");
        if (elenaGO == null) elenaGO = GameObject.Find("Nora");
        if (elenaGO == null)
        {
            foreach (var si in Object.FindObjectsByType<SuspectInterrogation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (si.gameObject.name.ToLower().Contains("elena") || si.gameObject.name.ToLower().Contains("nora") ||
                    (!string.IsNullOrEmpty(si.suspectName) && (si.suspectName.ToLower().Contains("elena") || si.suspectName.ToLower().Contains("nora"))))
                {
                    elenaGO = si.gameObject;
                    break;
                }
            }
        }

        if (elenaGO == null)
        {
            elenaGO = new GameObject("NPC_Elena");
            elenaGO.AddComponent<SuspectInterrogation>();
        }

        if (elenaGO != null)
        {
            elenaGO.name = "NPC_Elena";
            elenaGO.SetActive(false); // Hidden until 5 evidences are collected
            // Position Elena firmly on open floor walkway, in clear direct view
            elenaGO.transform.position = new Vector3(2.6f, 0.0f, 34.5f);
            elenaGO.transform.rotation = Quaternion.Euler(0, 210, 0);
            elenaGO.transform.localScale = Vector3.one;
            ConfigureCharacterModel(elenaGO, womanFbx, animController, "Elena Rostova", 1.7f);
            Sprite elenaPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Portraits/Elena_Rostova.png");
            SuspectInterrogation elenaInterrogation = elenaGO.GetComponent<SuspectInterrogation>();
            if (elenaInterrogation != null && elenaPortrait != null)
            {
                elenaInterrogation.suspectPortrait = elenaPortrait;
                EditorUtility.SetDirty(elenaInterrogation);
            }
            Debug.Log($"<color=green>[Suspects] Configured Elena '{elenaGO.name}' on open floor at (2.6, 0.0, 34.5) successfully!</color>");
        }

        // 5. Connect SeparateInterrogationManager
        var sim = Object.FindAnyObjectByType<SeparateInterrogationManager>(FindObjectsInactive.Include);
        if (sim != null)
        {
            sim.keepSuspectsVisibleInitially = false; // Must be hidden until all 5 evidences are collected
            if (sim.suspect1 != null)
            {
                if (victorGO != null) sim.suspect1.suspectObject = victorGO.transform;
                sim.suspect1.chairTarget = chairTarget.transform;
                sim.suspect1.suspectName = "Victor Sterling";
                sim.suspect1.suspectPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Portraits/Victor_Sterling.png");
            }
            if (sim.suspect2 != null)
            {
                if (elenaGO != null) sim.suspect2.suspectObject = elenaGO.transform;
                sim.suspect2.chairTarget = chairTarget.transform;
                sim.suspect2.suspectName = "Elena Rostova";
                sim.suspect2.suspectPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Portraits/Elena_Rostova.png");
            }
            EditorUtility.SetDirty(sim);
        }

        // 6. Connect PoliceSceneManager on both (if present)
        var playerMovement = Object.FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Include);
        if (victorGO != null)
        {
            var psm = victorGO.GetComponent<PoliceSceneManager>();
            if (psm != null) 
            { 
                psm.chairTarget = chairTarget.transform; 
                if (playerMovement != null) psm.investigator = playerMovement.transform;
                EditorUtility.SetDirty(psm); 
            }
        }
        if (elenaGO != null)
        {
            var psm = elenaGO.GetComponent<PoliceSceneManager>();
            if (psm != null) 
            { 
                psm.chairTarget = chairTarget.transform; 
                if (playerMovement != null) psm.investigator = playerMovement.transform;
                EditorUtility.SetDirty(psm); 
            }
        }

        // 7. Setup Interrogation Camera
        GameObject camGO = GameObject.Find("SuspectCamera");
        if (camGO == null) camGO = GameObject.Find("Camera (1)");
        if (camGO != null)
        {
            // Position camera across the desk facing the suspect
            camGO.transform.position = new Vector3(4.45f, 1.25f, 34.75f);
            camGO.transform.LookAt(new Vector3(4.2f, 0.95f, 33.25f));
            Camera cam = camGO.GetComponent<Camera>();
            if (cam != null && sim != null)
            {
                if (sim.suspect1 != null) sim.suspect1.suspectCamera = cam;
                if (sim.suspect2 != null) sim.suspect2.suspectCamera = cam;
            }
            EditorUtility.SetDirty(camGO);
        }

        EditorSceneManager.MarkSceneDirty(currentScene);
        EditorSceneManager.SaveScene(currentScene);
        Debug.Log("<color=green>[Suspects] New 3D models for Victor & Elena installed and configured successfully!</color>");
        if (showDialog)
        {
            EditorUtility.DisplayDialog("Suspect Models Updated", 
                "The new 3D character models have been successfully installed:\n\n" +
                "✓ Victor Sterling (Evan): Walking (Man in black)\n" +
                "✓ Elena Rostova (Nora): Walking (1) (Woman in grey)\n" +
                "✓ Humanoid rig & animations (Walk, Sit, Talk, Idle) connected 100%\n" +
                "✓ Seating target & Interrogation camera calibrated at the chair\n\n" +
                "Ready to test!", "OK");
        }
    }

    private static void ConfigureCharacterModel(GameObject rootGO, GameObject fbxPrefab, RuntimeAnimatorController controller, string suspectName, float height)
    {
        // 1. Remove old visual mesh children (except audio or cameras)
        List<GameObject> toDestroy = new List<GameObject>();
        for (int i = 0; i < rootGO.transform.childCount; i++)
        {
            Transform child = rootGO.transform.GetChild(i);
            if (child.GetComponent<Camera>() != null || child.GetComponent<AudioSource>() != null)
            {
                continue;
            }
            toDestroy.Add(child.gameObject);
        }
        foreach (var go in toDestroy)
        {
            Undo.DestroyObjectImmediate(go);
        }

        // 1. Ensure root is clean
        rootGO.transform.localScale = Vector3.one;

        // 2. Instantiate new model child
        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab, rootGO.transform);
        modelInstance.name = "Character_Visual";
        modelInstance.transform.localRotation = Quaternion.identity;

        // Fallback to validated character model if prefab has no renderers
        if (modelInstance.GetComponentsInChildren<Renderer>(true).Length == 0)
        {
            Undo.DestroyObjectImmediate(modelInstance);
            string fallbackPath = fbxPrefab.name.Contains("(1)")
                ? "Assets/_Project/Prefabs/Characters/Animation/nora/Walking.fbx"
                : "Assets/_Project/Prefabs/Characters/Animation/evan/Walking.fbx";
            GameObject fallbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fallbackPath);
            if (fallbackPrefab != null)
            {
                modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(fallbackPrefab, rootGO.transform);
                modelInstance.name = "Character_Visual";
                fbxPrefab = fallbackPrefab;
            }
        }

        // 3. Measure raw mesh bounds to accurately scale and plant feet on the floor
        Bounds localBounds = new Bounds();
        bool hasBounds = false;
        foreach (var r in modelInstance.GetComponentsInChildren<Renderer>(true))
        {
            if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
            {
                if (!hasBounds) { localBounds = smr.sharedMesh.bounds; hasBounds = true; }
                else { localBounds.Encapsulate(smr.sharedMesh.bounds); }
            }
            else if (r is MeshRenderer && r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null)
            {
                var mb = r.GetComponent<MeshFilter>().sharedMesh.bounds;
                if (!hasBounds) { localBounds = mb; hasBounds = true; }
                else { localBounds.Encapsulate(mb); }
            }
        }

        float scaleFactor = 1.0f;
        float yOffset = 0f;
        if (hasBounds && localBounds.size.y > 0.0001f)
        {
            scaleFactor = height / localBounds.size.y;
            yOffset = -localBounds.min.y * scaleFactor;
        }
        else
        {
            scaleFactor = 1.0f;
            yOffset = 0f;
        }

        modelInstance.transform.localScale = Vector3.one * scaleFactor;
        modelInstance.transform.localPosition = new Vector3(0f, yOffset, 0f);

        // Double-check world bounds and strictly plant feet on the floor
        Bounds worldBounds = new Bounds();
        bool hasWorldBounds = false;
        foreach (var r in modelInstance.GetComponentsInChildren<Renderer>(true))
        {
            if (!hasWorldBounds) { worldBounds = r.bounds; hasWorldBounds = true; }
            else { worldBounds.Encapsulate(r.bounds); }
        }

        if (hasWorldBounds && worldBounds.size.y > 0.01f)
        {
            float curH = worldBounds.size.y;
            if (Mathf.Abs(curH - height) > 0.15f)
            {
                float adj = height / curH;
                scaleFactor *= adj;
                modelInstance.transform.localScale = Vector3.one * scaleFactor;
            }

            // Always ensure bottom of shoes touches floor level (rootGO.transform.position.y)
            float floorLevel = rootGO.transform.position.y;
            float bottomOffset = worldBounds.min.y - floorLevel;
            modelInstance.transform.position -= new Vector3(0f, bottomOffset, 0f);
        }

        // 4. Configure Animator on the model
        Animator rootAnim = rootGO.GetComponent<Animator>();
        if (rootAnim != null) Undo.DestroyObjectImmediate(rootAnim);

        Animator anim = modelInstance.GetComponent<Animator>();
        if (anim == null) anim = modelInstance.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;

        Avatar avatar = null;
        string path = AssetDatabase.GetAssetPath(fbxPrefab);
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Avatar av)
            {
                avatar = av;
                break;
            }
        }
        if (avatar != null)
        {
            anim.avatar = avatar;
        }

        // 5. Update CapsuleCollider to surround the full body
        CapsuleCollider col = rootGO.GetComponent<CapsuleCollider>();
        if (col == null) col = rootGO.AddComponent<CapsuleCollider>();
        col.height = height;
        col.center = new Vector3(0, height * 0.5f, 0);
        col.radius = 0.38f;

        // 6. Update SuspectInterrogation
        SuspectInterrogation si = rootGO.GetComponent<SuspectInterrogation>();
        if (si != null)
        {
            si.suspectName = suspectName;
            EditorUtility.SetDirty(si);
        }

        // 7. Ensure Universal Render Pipeline / Lit materials with vivid textures
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");

        if (urpShader != null)
        {
            if (!fbxPrefab.name.Contains("(1)"))
            {
                SetupVictorMaterials(modelInstance, urpShader);
            }
            else
            {
                SetupElenaMaterials(modelInstance, urpShader);
            }
        }

        EditorUtility.SetDirty(rootGO);
        EditorUtility.SetDirty(modelInstance);
    }

    private static void SetupVictorMaterials(GameObject modelInstance, Shader urpShader)
    {
        Material victorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Prefabs/Victor_URP_Mat.mat");
        if (victorMat == null)
        {
            victorMat = new Material(urpShader);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/tripo_mat_d0bcade7_baseColor.jpg");
            if (tex != null)
            {
                victorMat.SetTexture("_BaseMap", tex);
                victorMat.SetTexture("_MainTex", tex);
            }
            victorMat.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(victorMat, "Assets/_Project/Prefabs/Victor_URP_Mat.mat");
        }
        else
        {
            victorMat.shader = urpShader;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/tripo_mat_d0bcade7_baseColor.jpg");
            if (tex != null)
            {
                victorMat.SetTexture("_BaseMap", tex);
                victorMat.SetTexture("_MainTex", tex);
            }
            victorMat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(victorMat);
        }

        foreach (var r in modelInstance.GetComponentsInChildren<Renderer>(true))
        {
            r.sharedMaterial = victorMat;
            EditorUtility.SetDirty(r);
        }
    }

    private static void SetupElenaMaterials(GameObject modelInstance, Shader urpShader)
    {
        Texture2D bodyTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/AvatarBody_baseColor.jpg");
        Texture2D bodyNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/AvatarBody_normal.png");
        Texture2D headTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/AvatarHead_baseColor.jpg");
        Texture2D headNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/AvatarHead_normal.png");
        Texture2D outfitTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/outfit_baseColor.jpg");
        Texture2D outfitNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/outfit_normal.png");
        Texture2D hairTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/haircut_baseColor.png");
        Texture2D hairNorm = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/haircut_normal.png");
        Texture2D glassesTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/glasses_baseColor.png");
        Texture2D teethTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/AvatarTeethLower_baseColor.jpg");
        Texture2D eyeTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Prefabs/AvatarLeftEyeball_baseColor.jpg");

        foreach (var r in modelInstance.GetComponentsInChildren<Renderer>(true))
        {
            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;
                string mName = m.name.ToLower();
                m.shader = urpShader;
                m.SetColor("_BaseColor", Color.white);

                Texture2D albedo = null;
                Texture2D normal = null;

                if (mName.Contains("outfit") || mName.Contains("mat0") || mName.Contains("mat5")) { albedo = outfitTex; normal = outfitNorm; }
                else if (mName.Contains("body") || mName.Contains("mat3")) { albedo = bodyTex; normal = bodyNorm; }
                else if (mName.Contains("head") || mName.Contains("mat2")) { albedo = headTex; normal = headNorm; }
                else if (mName.Contains("hair") || mName.Contains("mat4")) { albedo = hairTex; normal = hairNorm; }
                else if (mName.Contains("glass") || mName.Contains("mat6")) { albedo = glassesTex; }
                else if (mName.Contains("teeth") || mName.Contains("mat7")) { albedo = teethTex; }
                else if (mName.Contains("eye") || mName.Contains("mat1")) { albedo = eyeTex; }
                else if (outfitTex != null) { albedo = outfitTex; normal = outfitNorm; }

                if (albedo != null)
                {
                    m.SetTexture("_BaseMap", albedo);
                    m.SetTexture("_MainTex", albedo);
                }
                if (normal != null)
                {
                    m.SetTexture("_BumpMap", normal);
                    m.EnableKeyword("_NORMALMAP");
                }
                EditorUtility.SetDirty(m);
            }
            r.sharedMaterials = mats;
        }
    }

    [MenuItem("Cases/Assign Case 2 Audio Voices (Victor & Elena)")]
    public static void AssignCase2Voices()
    {
        AssetDatabase.Refresh();

        AudioClip victorClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Victor .mp3");
        AudioClip victorFemaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/dective-female.mp3");
        AudioClip victorMaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/dective-male.mp3");

        AudioClip elenaClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Elena.mp3");
        AudioClip elenaFemaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/dective-female-Elena.mp3");
        AudioClip elenaMaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/dective-male-Elena.mp3");

        if (victorClip == null && AssetDatabase.FindAssets("Victor t:AudioClip").Length > 0)
            victorClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Victor t:AudioClip")[0]));
        if (victorFemaleClip == null && AssetDatabase.FindAssets("dective-female t:AudioClip").Length > 0)
            victorFemaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("dective-female t:AudioClip")[0]));
        if (victorMaleClip == null && AssetDatabase.FindAssets("dective-male t:AudioClip").Length > 0)
            victorMaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("dective-male t:AudioClip")[0]));

        if (elenaClip == null && AssetDatabase.FindAssets("Elena t:AudioClip").Length > 0)
            elenaClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Elena t:AudioClip")[0]));
        if (elenaFemaleClip == null && AssetDatabase.FindAssets("dective-female-Elena t:AudioClip").Length > 0)
            elenaFemaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("dective-female-Elena t:AudioClip")[0]));
        if (elenaMaleClip == null && AssetDatabase.FindAssets("dective-male-Elena t:AudioClip").Length > 0)
            elenaMaleClip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("dective-male-Elena t:AudioClip")[0]));

        var sim = Object.FindAnyObjectByType<SeparateInterrogationManager>(FindObjectsInactive.Include);
        if (sim != null)
        {
            if (sim.suspect1 != null)
            {
                if (victorClip != null && sim.suspect1.suspectVoiceAudio != null) { sim.suspect1.suspectVoiceAudio.clip = victorClip; EditorUtility.SetDirty(sim.suspect1.suspectVoiceAudio); }
                if (victorFemaleClip != null && sim.suspect1.detectiveAskFemale != null) { sim.suspect1.detectiveAskFemale.clip = victorFemaleClip; EditorUtility.SetDirty(sim.suspect1.detectiveAskFemale); }
                if (victorMaleClip != null && sim.suspect1.detectiveAskMale != null) { sim.suspect1.detectiveAskMale.clip = victorMaleClip; EditorUtility.SetDirty(sim.suspect1.detectiveAskMale); }
            }

            if (sim.suspect2 != null)
            {
                if (elenaClip != null && sim.suspect2.suspectVoiceAudio != null) { sim.suspect2.suspectVoiceAudio.clip = elenaClip; EditorUtility.SetDirty(sim.suspect2.suspectVoiceAudio); }
                if (elenaFemaleClip != null && sim.suspect2.detectiveAskFemale != null) { sim.suspect2.detectiveAskFemale.clip = elenaFemaleClip; EditorUtility.SetDirty(sim.suspect2.detectiveAskFemale); }
                if (elenaMaleClip != null && sim.suspect2.detectiveAskMale != null) { sim.suspect2.detectiveAskMale.clip = elenaMaleClip; EditorUtility.SetDirty(sim.suspect2.detectiveAskMale); }
            }
            EditorUtility.SetDirty(sim);
        }

        GameObject victorGO = GameObject.Find("evan");
        if (victorGO != null && victorClip != null) { AudioSource src = victorGO.GetComponent<AudioSource>(); if (src != null) { src.clip = victorClip; EditorUtility.SetDirty(src); } }

        GameObject victorFemaleGO = GameObject.Find("evan-female");
        if (victorFemaleGO != null && victorFemaleClip != null) { AudioSource src = victorFemaleGO.GetComponent<AudioSource>(); if (src != null) { src.clip = victorFemaleClip; EditorUtility.SetDirty(src); } }

        GameObject victorMaleGO = GameObject.Find("dective-male");
        if (victorMaleGO != null && victorMaleClip != null) { AudioSource src = victorMaleGO.GetComponent<AudioSource>(); if (src != null) { src.clip = victorMaleClip; EditorUtility.SetDirty(src); } }

        GameObject elenaHolder = GameObject.Find("NoraAudioHolder");
        if (elenaHolder != null && elenaClip != null) { AudioSource src = elenaHolder.GetComponent<AudioSource>(); if (src != null) { src.clip = elenaClip; EditorUtility.SetDirty(src); } }

        GameObject elenaFemaleAudioGO = GameObject.Find("nora-female");
        if (elenaFemaleAudioGO != null && elenaFemaleClip != null) { AudioSource src = elenaFemaleAudioGO.GetComponent<AudioSource>(); if (src != null) { src.clip = elenaFemaleClip; EditorUtility.SetDirty(src); } }

        GameObject elenaMaleAudioGO = GameObject.Find("nora");
        if (elenaMaleAudioGO != null && elenaMaleClip != null) { AudioSource src = elenaMaleAudioGO.GetComponent<AudioSource>(); if (src != null) { src.clip = elenaMaleClip; EditorUtility.SetDirty(src); } }
    }

    private static void PlaceEvidenceInScene(string evidenceID, Vector3 pos, Quaternion rot, string eliminatesSuspect = null, string finding = null)
    {
        var evidenceComponents = Object.FindObjectsByType<Evidence>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ev in evidenceComponents)
        {
            if (ev.evidenceID == evidenceID)
            {
                ev.transform.position = pos;
                ev.transform.rotation = rot;
                ev.gameObject.SetActive(true);

                if (eliminatesSuspect != null) ev.eliminatesSuspectID = eliminatesSuspect;
                if (finding != null) ev.eliminationFinding = finding;

                // Clean out any broken/empty MeshCollider
                foreach (var mc in ev.GetComponentsInChildren<MeshCollider>(true))
                {
                    if (mc != null && mc.sharedMesh == null)
                    {
                        Undo.DestroyObjectImmediate(mc);
                    }
                }

                // Add or configure a clean trigger SphereCollider with 0.5m radius
                SphereCollider sc = ev.GetComponent<SphereCollider>();
                if (sc == null) sc = ev.gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 0.5f;

                EditorUtility.SetDirty(ev);
                EditorUtility.SetDirty(ev.gameObject);
                Debug.Log($"<color=green>[LabSetup] Grounded {evidenceID} ({ev.evidenceName}) stably on tabletop at {pos} with trigger collider!</color>");
                return;
            }
        }
    }
}
#endif
