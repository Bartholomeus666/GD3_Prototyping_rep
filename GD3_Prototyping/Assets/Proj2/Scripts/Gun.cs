using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Gun : MonoBehaviour
{
    [SerializeField] private GameObject _bulletPrefab;
    public List<GameObject> Bullets;

    public InputAction ShootAction;

    private void OnEnable()
    {
        ShootAction.Enable();
        ShootAction.performed += Shoot;
    }

    public void Shoot(InputAction.CallbackContext ctx)
    {
        GameObject bullet = Instantiate(_bulletPrefab, Camera.main.transform.position, Camera.main.transform.rotation);
        Bullets.Add(bullet);
    }

    private void Update()
    {
        MoveBullets();
    }

    private void MoveBullets()
    {
        foreach (GameObject bullet in Bullets)
        {
            if (bullet != null)
            {
                bullet.transform.Translate(Vector3.forward * Time.deltaTime * 10f);
            }
        }
    }
}
