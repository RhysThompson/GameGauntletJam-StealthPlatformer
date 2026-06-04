using Unity.VisualScripting;
using UnityEngine;

public class WindCurrentScript : MonoBehaviour
{
    public float WindForce;
    public float GliderOnlyWindForce; // stacks with regular wind force

    [DoNotSerialize]
    public Vector3 WindDirection;
    [DoNotSerialize]
    public Vector3 GliderWindDirection;

    void Update()
    {
        WindDirection = this.transform.up * WindForce;
        GliderWindDirection = this.transform.up * GliderOnlyWindForce;
    }
}
