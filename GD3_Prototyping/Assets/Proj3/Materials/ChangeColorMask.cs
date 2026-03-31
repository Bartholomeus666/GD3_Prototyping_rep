using Unity.VisualScripting;
using UnityEngine;

public class ChangeColorMask : MonoBehaviour
{
    public void Start()
    {
        GetComponent<Renderer>().material.SetInt("_ColorMask", 0);
    }
}
