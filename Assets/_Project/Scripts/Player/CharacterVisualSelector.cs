using UnityEngine;

public class CharacterVisualSelector : MonoBehaviour
{
    [Header("مرجع ملف اللاعب")]
    [SerializeField] private PlayerProfile profile;

    [Header("المجسمات النسائية (Female Models)")]
    [SerializeField] private GameObject femaleDetective;
    [SerializeField] private GameObject femaleDoctor;
    [SerializeField] private GameObject femaleTech;
    [SerializeField] private GameObject femaleInterrogator;

    [Header("المجسمات الرجالية (Male Models)")]
    [SerializeField] private GameObject maleDetective;
    [SerializeField] private GameObject maleDoctor;
    [SerializeField] private GameObject maleTech;
    [SerializeField] private GameObject maleInterrogator;

    private void Awake()
    {
        if (profile == null)
            profile = GetComponent<PlayerProfile>();
    }

    private void OnEnable()
    {
        if (profile == null)
            profile = GetComponent<PlayerProfile>();

        if (profile != null)
            profile.OnProfileChanged += ApplySelectedModel;
    }

    private void OnDisable()
    {
        if (profile != null)
            profile.OnProfileChanged -= ApplySelectedModel;
    }

    private void Start()
    {
        ApplySelectedModel();
    }

    public void ApplySelectedModel()
    {
        if (profile == null) return;

        HideAllModels();

        bool isFemale = profile.gender.ToString().ToLower().Contains("female");
        string role = profile.currentRole.ToString().ToLower();

        if (isFemale)
        {
            if (role.Contains("forensic") || role.Contains("doctor"))
                Activate(femaleDoctor);
            else if (role.Contains("tech") || role.Contains("digital") || role.Contains("analyst"))
                Activate(femaleTech);
            else if (role.Contains("interrogat"))
                Activate(femaleInterrogator);
            else
                Activate(femaleDetective);
        }
        else // Male
        {
            if (role.Contains("forensic") || role.Contains("doctor"))
                Activate(maleDoctor);
            else if (role.Contains("tech") || role.Contains("digital") || role.Contains("analyst"))
                Activate(maleTech);
            else if (role.Contains("interrogat"))
                Activate(maleInterrogator);
            else
                Activate(maleDetective);
        }
    }

    private void HideAllModels()
    {
        if (femaleDetective != null) femaleDetective.SetActive(false);
        if (femaleDoctor != null) femaleDoctor.SetActive(false);
        if (femaleTech != null) femaleTech.SetActive(false);
        if (femaleInterrogator != null) femaleInterrogator.SetActive(false);

        if (maleDetective != null) maleDetective.SetActive(false);
        if (maleDoctor != null) maleDoctor.SetActive(false);
        if (maleTech != null) maleTech.SetActive(false);
        if (maleInterrogator != null) maleInterrogator.SetActive(false);
    }

    private void Activate(GameObject model)
    {
        if (model != null)
        {
            model.SetActive(true);
        }
        else
        {
            if (femaleDetective != null) femaleDetective.SetActive(true);
            else if (maleDetective != null) maleDetective.SetActive(true);
        }

        // If this is the local player, hide the meshes but keep shadows
        var networkPlayer = GetComponent<NetworkPlayerController>();
        if (networkPlayer != null && networkPlayer.IsOwner)
        {
            GameObject activeModel = model != null ? model : (femaleDetective != null ? femaleDetective : maleDetective);
            if (activeModel != null)
            {
                Renderer[] renderers = activeModel.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
        }
    }
}