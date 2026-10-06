using UnityEngine;

public class WitnessDialogue : MonoBehaviour, IInteractable
{
    [Header("Witness Identity")]
    public string witnessName = "Nora Blake";

    [Header("Statement")]
    [TextArea(2, 4)]
    public string statement = "I saw someone carrying a small sealed box toward the rear exit. They were wearing a dark jacket!";

    public string InteractionPrompt => $"[E] TALK TO {witnessName.ToUpper()}";

    public void Interact()
    {
        if (UIManager.Instance != null)
        {
            // إظهار إفادة الشاهدة كإشعار فوري على الشاشة
            UIManager.Instance.ShowOnScreenNotification($"{witnessName}: \"{statement}\"");
        }
        Debug.Log($"<color=yellow>[WITNESS]</color> {witnessName}: {statement}");
    }
}