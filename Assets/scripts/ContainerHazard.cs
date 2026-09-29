using UnityEngine;
using UnityEngine.SceneManagement;

public class ContainerHazard : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Grace>() == null)
            return;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
