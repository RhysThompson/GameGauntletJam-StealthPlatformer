using UnityEngine;

public class DeathTransitionHelper : MonoBehaviour
{
    public GameObject ObjectToDestroy;

    public void DestroyObject()
    {
        Destroy(ObjectToDestroy);
    }

    public void RespawnPlayer()
    {
        GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>().Respawn();
    }
}
