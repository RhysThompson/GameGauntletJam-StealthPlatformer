using UnityEngine;

public class BillboardSpriteHandler : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.LookAt(Camera.main.transform, Vector3.up);
        Vector3 rot = this.transform.rotation.eulerAngles;
        rot.x = 0;
        this.transform.rotation = Quaternion.Euler(rot);
    }
}
