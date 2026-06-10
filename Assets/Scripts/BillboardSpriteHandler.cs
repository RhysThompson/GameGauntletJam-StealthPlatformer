using UnityEngine;

public class BillboardSpriteHandler : MonoBehaviour
{
    public bool LockVerticalRotation = true;

    void Update()
    {
        this.transform.LookAt(Camera.main.transform, Vector3.up);
        Vector3 rot = this.transform.rotation.eulerAngles;
        if (LockVerticalRotation)
            rot.x = 0;
        this.transform.rotation = Quaternion.Euler(rot);
    }
}
