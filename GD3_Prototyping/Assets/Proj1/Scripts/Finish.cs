using UnityEngine;

public class Finish : MonoBehaviour
{
    public GameObject Canvas;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Time.timeScale = 0;
            Canvas.SetActive(true);
        }
    }

    public void End()
    {
        Application.Quit();
    }
}
