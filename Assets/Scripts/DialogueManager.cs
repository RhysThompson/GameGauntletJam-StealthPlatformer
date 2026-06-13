using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*public enum DialogueActions
{
    NONE,
    ACTION_MOVEMENT,
    ACTION_ATTACKING,
    ACTION_CASTFIRE,
    ACTION_CASTRESURRECT,
    HP,
    MP,
    RESET_GAME
}*/

[Serializable]
public enum CHARACTER_VOICE
{
    NONE,
    PLAYER,
}

[Serializable]
public class VoiceClipSet
{
    [Tooltip("Doesn't do anything, use to label which element corresponds to which voice")]
    public CHARACTER_VOICE Voice;
    public AudioClip[] VoiceClips;
}

[Serializable]
public class DialogueInstruction
{
    [TextArea(3, 10)]
    public string Text;
    public bool ClearExistingText;
    public bool NewLine;
    public bool WaitForPlayerInput = false;
    public float StartDelay;
    public float PerLetterDelay = 0.05f;
    //public DialogueActions Action;
    //public int StatBoostAmount;
    public CHARACTER_VOICE VoiceClipGroup;
    public bool PlaySoundOnce = false;
    public int LetterDelayBetweenSounds = 1;

    public DialogueInstruction Clone()
    {
        DialogueInstruction newObj = new DialogueInstruction();
        newObj.Text = Text;
        newObj.ClearExistingText = ClearExistingText;
        newObj.NewLine = NewLine;
        newObj.WaitForPlayerInput = WaitForPlayerInput;
        newObj.StartDelay = StartDelay;
        newObj.PerLetterDelay = PerLetterDelay;
        newObj.VoiceClipGroup = VoiceClipGroup;
        newObj.PlaySoundOnce = PlaySoundOnce;
        newObj.LetterDelayBetweenSounds = LetterDelayBetweenSounds;
        return newObj;
    }
}

[CreateAssetMenu(fileName = "Data", menuName = "Dialogue Set", order = 1)]
public class DialogueSet : ScriptableObject
{
    public List<DialogueInstruction> DialogueInstructions;
}

[Serializable]
public class DialogueGroup
{
    public List<DialogueInstruction> DialogueInstructions;
}

public class DialogueManager : MonoBehaviour
{
    public GameObject DialogueCanvasPrefab;
    GameObject DialogueCanvas;
    RectTransform DialogueBox;
    GameObject DialogueBoxIndicator;
    GameObject TalkIndicator;
    TextMeshProUGUI DialogueBoxText;

    public InputActionAsset ActionAsset;
    InputAction NextAction;


    public List<VoiceClipSet> VoiceClipGroups;
    private int VoiceClipDelay = 0;

    public List<DialogueInstruction> DialogueInstructions;
    private bool InDialogue = true;

    private float DialogueDelayTimer = 0f;

    private PlayerScript Player;
    private AudioSource AudioPlayer;
    CinemachineCamera CurrentCamera;

    enum DialogueState
    {
        NONE,
        PREPARING,
        OPENINGBOX,
        WRITING,
        WAITING,
        CLOSINGBOX
    }

    private DialogueState CurrentState = DialogueState.NONE;

    // Start is called before the first frame update
    void Start()
    {
        DialogueCanvas = Instantiate(DialogueCanvasPrefab);
        DialogueBox = DialogueCanvas.transform.Find("Panel parent").Find("Panel").GetComponent<RectTransform>();
        DialogueBoxIndicator = DialogueCanvas.transform.Find("Panel parent").Find("Indicator Parent").gameObject;
        DialogueBoxText = DialogueCanvas.transform.Find("Panel parent").Find("Text").GetComponent<TextMeshProUGUI>();
        DialogueBoxText.text = "";
        TalkIndicator = DialogueCanvas.transform.Find("Talk").gameObject;
        TalkIndicator.SetActive(false);

        DialogueBox.localScale = new Vector3(1, 0, 1);
        DialogueBoxIndicator.SetActive(false);
        //DialogueInstructions = new List<DialogueInstruction>();
        Player = this.gameObject.GetComponent<PlayerScript>();
        NextAction = InputSystem.actions.FindAction("Next");
        AudioPlayer = this.GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (InDialogue)
        {
            if(NextAction.WasPressedThisFrame())
            {
                SkipDialogue();
                return;
            }

            switch (CurrentState)
            {
                case DialogueState.PREPARING:
                    if (Player.IsInDialogue())
                    {
                        CurrentState = DialogueState.OPENINGBOX;
                        DialogueBoxText.text = "";
                    }
                    break;

                case DialogueState.OPENINGBOX:
                    if (DialogueBox.localScale.y < 1)
                    {
                        DialogueBox.localScale = new Vector3(1, DialogueBox.localScale.y + (3f * Time.deltaTime), 1);
                        break;
                    }

                    DialogueBox.localScale = new Vector3(1, 1, 1);
                    DialogueDelayTimer = 0.5f;
                    DialogueDelayTimer += DialogueInstructions[0].StartDelay;
                    CurrentState = DialogueState.WRITING;
                    break;

                case DialogueState.WRITING:
                    DialogueDelayTimer -= Time.deltaTime;

                    while (DialogueDelayTimer <= 0)
                    {
                        if (DialogueInstructions[0].Text.Length > 0)
                        {
                            if (DialogueInstructions[0].ClearExistingText)
                            {
                                DialogueBoxText.text = "";
                                DialogueInstructions[0].ClearExistingText = false;
                            }

                            if (DialogueInstructions[0].NewLine)
                            {
                                DialogueBoxText.text += "\n";
                                DialogueInstructions[0].NewLine = false;
                            }


                            DialogueBoxText.text += DialogueInstructions[0].Text[0];
                            DialogueInstructions[0].Text = DialogueInstructions[0].Text.Remove(0, 1);
                            DialogueDelayTimer += DialogueInstructions[0].PerLetterDelay;
                            
                            if (DialogueInstructions[0].PlaySoundOnce)
                            {
                                if (VoiceClipDelay == 0)
                                {
                                    VoiceClipDelay = 1;
                                    int numClips = VoiceClipGroups[(int)DialogueInstructions[0].VoiceClipGroup].VoiceClips.Length;
                                    if (numClips > 0)
                                    {
                                        AudioPlayer.PlayOneShot(VoiceClipGroups[(int)DialogueInstructions[0].VoiceClipGroup].VoiceClips[UnityEngine.Random.Range(0, numClips)]);
                                    }
                                }
                            }
                            else
                            {
                                int numClips = VoiceClipGroups[(int)DialogueInstructions[0].VoiceClipGroup].VoiceClips.Length;
                                if (numClips > 0)
                                {
                                    if (VoiceClipDelay == 0)
                                    {
                                        VoiceClipDelay = DialogueInstructions[0].LetterDelayBetweenSounds;
                                        AudioPlayer.PlayOneShot(VoiceClipGroups[(int)DialogueInstructions[0].VoiceClipGroup].VoiceClips[UnityEngine.Random.Range(0, numClips)]);
                                    }
                                    else
                                        VoiceClipDelay--;
                                }
                            }
                        }
                        else
                        {
                            VoiceClipDelay = 0;
                            if (DialogueInstructions.Count == 1 || DialogueInstructions[0].WaitForPlayerInput)
                            {
                                CurrentState = DialogueState.WAITING;
                                DialogueBoxIndicator.SetActive(true);
                            }
                            else
                            {
                                //if (DialogueInstructions[0].Action != DialogueActions.NONE)
                                //Player.IncreaseStat(DialogueInstructions[0].Action, DialogueInstructions[0].StatBoostAmount);

                                DialogueInstructions.RemoveAt(0);
                                DialogueDelayTimer = DialogueInstructions[0].StartDelay;
                            }
                            break;
                        }
                    }
                    break;

                case DialogueState.CLOSINGBOX:
                    if (DialogueBox.localScale.y > 0)
                    {
                        DialogueBox.localScale = new Vector3(1, DialogueBox.localScale.y - (3f * Time.deltaTime), 1);
                        break;
                    }

                    DialogueBox.localScale = new Vector3(1, 0, 1);
                    CurrentCamera.Priority = 0;
                    Player.SetInDialogue(false);
                    InDialogue = false;
                    CurrentState = DialogueState.NONE;
                    break;
            }
        }
    }

    public void SkipDialogue()
    {
        if (CurrentState == DialogueState.WAITING)
        {
            DialogueBoxIndicator.SetActive(false);

            //if (DialogueInstructions[0].Action != DialogueActions.NONE)
                //Player.IncreaseStat(DialogueInstructions[0].Action, DialogueInstructions[0].StatBoostAmount);

            DialogueInstructions.RemoveAt(0);
            if (DialogueInstructions.Count == 0)
            {
                DialogueBoxText.text = "";
                CurrentState = DialogueState.CLOSINGBOX;
            }
            else
            {
                DialogueDelayTimer = DialogueInstructions[0].StartDelay;
                CurrentState = DialogueState.WRITING;
            }
        }
        else if (CurrentState == DialogueState.WRITING)
        {

            while (true)
            {
                //if (DialogueInstructions[0].Action != DialogueActions.NONE)
                //Player.IncreaseStat(DialogueInstructions[0].Action, DialogueInstructions[0].StatBoostAmount);

                if (DialogueInstructions[0].ClearExistingText)
                {
                    DialogueBoxText.text = "";
                    DialogueInstructions[0].ClearExistingText = false;
                }

                if (DialogueInstructions[0].NewLine)
                {
                    DialogueBoxText.text += "\n";
                    DialogueInstructions[0].NewLine = false;
                }
                DialogueBoxText.text += DialogueInstructions[0].Text;
                DialogueInstructions[0].Text = "";

                if (DialogueInstructions.Count == 1 || DialogueInstructions[0].WaitForPlayerInput || DialogueInstructions[1].ClearExistingText)
                    break;

                DialogueInstructions.RemoveAt(0);

            }

            CurrentState = DialogueState.WAITING;
            DialogueBoxIndicator.SetActive(true);
        }
    }

    public bool IsInDialogue()
    {
        return InDialogue;
    }

    public bool SetupDialogue(DialogueGroup instructions, CinemachineCamera camera)
    {
        if (CurrentState != DialogueState.NONE)
            return false ;

        foreach (DialogueInstruction i in instructions.DialogueInstructions)
            DialogueInstructions.Add(i.Clone());
        InDialogue = true;
        CurrentState = DialogueState.PREPARING;
        CurrentCamera = camera;
        CurrentCamera.Priority = 20;
        TalkIndicator.SetActive(false);
        Player.SetInDialogue(true);
        return true;
    }

    public void ShowTalkIndicator(bool show)
    {
        TalkIndicator.SetActive(show);
    }
}
