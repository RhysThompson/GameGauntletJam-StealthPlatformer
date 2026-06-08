using UnityEngine;
using UnityEngine.InputSystem;

public class RobotScript : MonoBehaviour
{
    public InputActionAsset ActionAsset;
    InputAction JumpAction;
    Animator MyAnimator;
    float MoveSpeed = 1f;
    bool Waliking = false;

    void OnEnable()
    {
        ActionAsset.FindActionMap("Player").Enable();
    }

    void OnDisable()
    {
        ActionAsset.FindActionMap("Player").Disable();
    }

    void Start()
    {
        MyAnimator = GetComponent<Animator>();
        JumpAction = InputSystem.actions.FindAction("Jump");
    }

    void Update()
    {
        if(JumpAction.WasPressedThisFrame())
        {
            Waliking = !Waliking;
            MyAnimator.SetBool("Walking", Waliking);
        }

        if (Waliking)
            this.transform.Translate(0, 0, -MoveSpeed * Time.deltaTime);
    }
}
