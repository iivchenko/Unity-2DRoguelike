using UnityEngine;

public class Loader : MonoBehaviour
{
    public GameObject gameManager;

    public void Awake()
    {
        if (GameManager.Instance == null)
            Instantiate(gameManager);
    }
}
