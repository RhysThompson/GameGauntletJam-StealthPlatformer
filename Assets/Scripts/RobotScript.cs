using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum MovementTypes
{
    MOVE_BY,
    ROTATE_LEFT_BY_DEGREES,
}

[Serializable]
public class RobotInstruction
{
    public MovementTypes MovementType;
    public float Amount;
    public float StartDelay = 0f;
}

public class RobotScript : MonoBehaviour
{
    public Animator MyAnimator;
    public float MoveSpeed = 5f;
    public float RotateSpeed = 5f;

    public RobotInstruction[] InstructionList;
    int CurrentInstruction = -1;
    float Delay = 0f;

    Vector3 TargetPosition;
    Quaternion TargetRotation;

    void Start()
    {
        IncrementInstruction();
    }

    void Update()
    {
        if (InstructionList.Length == 0)
            return;

        if(Delay > 0f)
        {
            Delay -= Time.deltaTime;
            return;
        }

        if (InstructionList[CurrentInstruction].MovementType == MovementTypes.MOVE_BY)
        {
            
            if (Vector3.Distance(this.transform.position, TargetPosition) > MoveSpeed * Time.deltaTime)
            {
                this.transform.position = Vector3.MoveTowards(this.transform.position, TargetPosition, MoveSpeed * Time.deltaTime);
                MyAnimator.SetBool("Walking", true);
            }
            else
            {
                this.transform.position = TargetPosition;
                IncrementInstruction();
                MyAnimator.SetBool("Walking", false);
            }
        }
        else if (InstructionList[CurrentInstruction].MovementType == MovementTypes.ROTATE_LEFT_BY_DEGREES)
        {
            if (Quaternion.Angle(this.transform.rotation, TargetRotation) > RotateSpeed * Time.deltaTime)
            {
                this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, TargetRotation, RotateSpeed * Time.deltaTime);
                MyAnimator.SetBool("Walking", true);
            }
            else
            {
                this.transform.rotation = TargetRotation;
                IncrementInstruction();
                MyAnimator.SetBool("Walking", false);
            }
        }
    }

    void IncrementInstruction()
    {
        CurrentInstruction++;
        if (CurrentInstruction >= InstructionList.Length)
            CurrentInstruction = 0;
        Delay = InstructionList[CurrentInstruction].StartDelay;

        // Setup the next
        if (InstructionList[CurrentInstruction].MovementType == MovementTypes.MOVE_BY)
        {
            TargetPosition = this.transform.position;
            TargetPosition += this.transform.forward * InstructionList[CurrentInstruction].Amount;
        }
        else if (InstructionList[CurrentInstruction].MovementType == MovementTypes.ROTATE_LEFT_BY_DEGREES)
        {
            Vector3 rot = this.transform.rotation.eulerAngles;
            rot.y += InstructionList[CurrentInstruction].Amount;
            TargetRotation = Quaternion.Euler(rot);
        }
    }
}
