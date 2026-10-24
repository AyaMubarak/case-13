using UnityEngine;

/// <summary>Visual-only copy of a teammate. It never reads local input.</summary>
public class RemotePlayerReplica : MonoBehaviour
{
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Animator[] animators;
    private float movementSpeed;

    public void Initialize(Vector3 position, Quaternion rotation, string playerName)
    {
        transform.SetPositionAndRotation(position, rotation);
        targetPosition = position;
        targetRotation = rotation;
        gameObject.name = $"[Remote] {playerName}";
        animators = GetComponentsInChildren<Animator>(true);
    }

    public void ApplyNetworkState(Vector3 position, Quaternion rotation, float speed)
    {
        targetPosition = position;
        targetRotation = rotation;
        movementSpeed = speed;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 14f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 14f);

        if (animators == null) return;
        foreach (Animator animator in animators)
        {
            if (animator == null) continue;
            SetFloatIfPresent(animator, "Speed", movementSpeed);
            SetFloatIfPresent(animator, "speed", movementSpeed);
            SetBoolIfPresent(animator, "IsWalking", movementSpeed > 0.05f);
            SetBoolIfPresent(animator, "isWalking", movementSpeed > 0.05f);
        }
    }

    private static void SetFloatIfPresent(Animator animator, string parameterName, float value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }

    private static void SetBoolIfPresent(Animator animator, string parameterName, bool value)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                animator.SetBool(parameterName, value);
                return;
            }
        }
    }
}
