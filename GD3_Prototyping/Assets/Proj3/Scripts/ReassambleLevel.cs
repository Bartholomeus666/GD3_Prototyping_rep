using System;
using UnityEngine;

public class ReassambleLevel : MonoBehaviour
{
    private DoublePlaneSliceTrigger _slicer;
    public float DistToMiddle = 15f;
    public float AssembleSpeed = 7f;

    private Vector3 _normalA;
    private Vector3 _normalB;

    private Vector3 _newPosA;
    private Vector3 _newPosB;

    private bool Assembling = false;

    GameObject _sideA, _sideB;

    private void Awake()
    {
        _slicer = GetComponent<DoublePlaneSliceTrigger>();
    }

    private void OnEnable()
    {
        _slicer.SliceEvent += PutLevelTogether;
    }

    private void Update()
    {
        if (Assembling)
        {
            _sideA.transform.position = Vector3.MoveTowards(_sideA.transform.position, _newPosA, AssembleSpeed * Time.deltaTime);
            _sideB.transform.position = Vector3.MoveTowards(_sideB.transform.position, _newPosB, AssembleSpeed * Time.deltaTime);

            Debug.Log($"{_sideA.transform.position}, {_sideB.transform.position}");
        }
    }

    private void PutLevelTogether(object sender, TSliceEventArgs e)
    {
        Debug.Log($"{e.SideA.transform.position}, {e.SideB.transform.position}");


        _normalA = _slicer.planeA.transform.up;
        _normalB = _slicer.planeB.transform.up;

        _sideA = e.SideA;
        _sideB = e.SideB;

        _newPosA = _sideA.transform.position + _normalA * DistToMiddle;
        _newPosB = _sideB.transform.position + _normalB * -DistToMiddle;

        Assembling = true;
    }
}
