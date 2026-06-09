using UnityEngine;

public class BouncerScript : MonoBehaviour
{
    public float BounceForce = 10;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            other.GetComponent<PlayerScript>().SetVelocity(this.transform.up * BounceForce);
        }
    }
}
