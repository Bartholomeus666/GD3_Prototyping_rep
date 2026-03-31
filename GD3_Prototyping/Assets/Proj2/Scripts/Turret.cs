using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System;

public class Turret : MonoBehaviour
{
    public List<GameObject> Turrets;
    public List<GameObject> Bullets;
    public GameObject TurretPrefab;
    public GameObject BulletPrefab;
    public float Spawntime = 5f;
    public float Shoottime = 2f;
    private float _spawnTimer;
    private float _shootTimer;

    public int Tally;
    public TMP_Text TallyText;  

    public GameObject Player;

    private List<GameObject> spawners;

    private void Start()
    {
        spawners = GameObject.FindGameObjectsWithTag("Respawn").ToList();
    }

    private void Update()
    {
        _spawnTimer += Time.deltaTime;
        _shootTimer += Time.deltaTime;
        if (_spawnTimer >= Spawntime)
        {
            SpawnTurret();
            _spawnTimer = 0f;
        }
        if(_shootTimer >= Shoottime)
        {
            foreach (GameObject turret in Turrets)
            {
                if (turret != null)
                    SpawnBullet(turret);
            }
            _shootTimer = 0f;
        }

        MoveBullets();
    }

    public void SpawnTurret()
    {
        GameObject spawnpoint = spawners[UnityEngine.Random.Range(0, spawners.Count)];
        GameObject newTurret = Instantiate(TurretPrefab, spawnpoint.transform.position, spawnpoint.transform.rotation);
        Turrets.Add(newTurret);
    }

    public void SpawnBullet(GameObject turret)
    {
        GameObject bullet = Instantiate(BulletPrefab, turret.transform.position, turret.transform.rotation);
        Bullets.Add(bullet);
        bullet.GetComponent<Bullets>().onBulletHit += AddTally;
    }

    public void MoveBullets()
    {
        foreach (GameObject bullet in Bullets)
        {
            if (bullet != null)
            {
                bullet.transform.position = Vector3.MoveTowards(bullet.transform.position, Player.transform.position, 10f * Time.deltaTime);
            }
        }
    }

    public void AddTally(object send, EventArgs e)
    {
        Tally++;
        TallyText.text = $"{Tally}";
    }
}
