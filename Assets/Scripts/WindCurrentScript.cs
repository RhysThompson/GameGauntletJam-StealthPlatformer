using Unity.VisualScripting;
using UnityEngine;

public class WindCurrentScript : MonoBehaviour
{
    public float WindForce;
    public float GliderOnlyWindForce; // stacks with regular wind force

    [HideInInspector]
    public Vector3 WindDirection;
    [HideInInspector]
    public Vector3 GliderWindDirection;

    void Update()
    {
        WindDirection = this.transform.up * WindForce;
        GliderWindDirection = this.transform.up * GliderOnlyWindForce;
    }
}
