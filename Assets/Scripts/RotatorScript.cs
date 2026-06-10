using UnityEngine;

public class RotatorScript : MonoBehaviour
{
    public Vector3 Rotation;
    
    void Update()
    {
        this.transform.Rotate(Rotation * Time.deltaTime);
    }
}
