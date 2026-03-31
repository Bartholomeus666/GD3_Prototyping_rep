using UnityEngine;

public class ObjectTriggerController : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "CheckOnPrisonerButton")
        {
            BitGamey.EasySplitScreen.ToggleEffect();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "CheckOnPrisonerButton")
        {
            BitGamey.EasySplitScreen.ToggleEffect();
        }
    }
}
