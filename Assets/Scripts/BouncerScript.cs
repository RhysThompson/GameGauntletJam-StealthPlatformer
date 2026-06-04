using UnityEngine;

public class BouncerScript : MonoBehaviour
{
    public float BounceForce = 10;

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            other.GetComponent<PlayerScript>().AddForce(this.transform.up * BounceForce);
        }
    }
}
