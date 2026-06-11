using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    public InputActionAsset ActionAsset;
    InputAction InteractAction;

    public CinemachineCamera TargetedCamera;

    public List<DialogueSet> Dialogue;
    [Tooltip("Used for one off triggers when entering an area.")]
    public bool AutoTrigger = false;
    [Tooltip("When off the last dialogue in the list will be used ever and over.")]
    public bool LoopDialogue = false;
    int DialogueCounter = 0;

    DialogueManager CollidingPlayer;


    void Start()
    {
        InteractAction = InputSystem.actions.FindAction("Interact");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (string.Compare(other.tag, "Player") == 0)
        {
            if (AutoTrigger)
            {
                other.GetComponent<DialogueManager>().SetupDialogue(Dialogue[DialogueCounter], TargetedCamera);
                Destroy(this.gameObject);
            }
            else
            {
                CollidingPlayer = other.gameObject.GetComponent<DialogueManager>();
                CollidingPlayer.ShowTalkIndicator(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (string.Compare(other.tag, "Player") == 0)
        {
            CollidingPlayer.ShowTalkIndicator(false);
            CollidingPlayer = null;
        }
    }
        
    void Update()
    {
        if(CollidingPlayer != null) 
        {
            if(!AutoTrigger)
            {
                if (InteractAction.WasPressedThisFrame())
                {
                    if (CollidingPlayer.SetupDialogue(Dialogue[DialogueCounter], TargetedCamera))
                    {
                        if (DialogueCounter < Dialogue.Count - 1)
                            DialogueCounter++;
                        else if (LoopDialogue)
                            DialogueCounter = 0;
                    }
                }
                else
                {
                    if(!CollidingPlayer.IsInDialogue())
                    {
                        CollidingPlayer.ShowTalkIndicator(true);
                    }
                }
            }
        }
    }
}
