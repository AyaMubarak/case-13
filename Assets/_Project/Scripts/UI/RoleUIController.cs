using UnityEngine;

public class RoleUIController : MonoBehaviour
{
    [SerializeField] private GameObject detectivePanel;
    [SerializeField] private GameObject interrogatorPanel;
    [SerializeField] private GameObject medicalPanel;
    [SerializeField] private GameObject techPanel;

    public void ShowRoleUI(PlayerRole role)
    {
        detectivePanel.SetActive(role == PlayerRole.Detective);
        interrogatorPanel.SetActive(role == PlayerRole.Interrogator);
        medicalPanel.SetActive(role == PlayerRole.Medical);
        techPanel.SetActive(role == PlayerRole.Tech);
    }
}
