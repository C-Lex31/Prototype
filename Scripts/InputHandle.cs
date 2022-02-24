
using System.Collections.Generic;
using UnityEngine;
using Ace;

public class InputButton
{
    public bool WasPressed { get; private set; }
    public bool WasReleased { get; private set; }
    public bool IsPressed { get; private set; }
    public bool bWasPressed { get; private set; }
    public bool bWasReleased { get; private set; }
    public bool bIsPressed { get; private set; }
    public KeyCode KeyName;
    public string InputName;

    public InputButton(KeyCode input)
    {
        bWasPressed = false;
        bWasReleased = false;
        bIsPressed = false;
        //   bWasPressed=false;
        KeyName = input;
    }

    public InputButton(string input)
    {
        WasPressed = false;
        WasReleased = false;
        IsPressed = false;
        InputName = input;
    }

    public void OnButtonUpdate()
    {

        if (string.IsNullOrEmpty(InputName))
            return;
        IsPressed = Input.GetButton(InputName);
        WasPressed = Input.GetButtonDown(InputName);
        WasReleased = Input.GetButtonUp(InputName);

    }
    public void OnKeyUpdate()
    {
        if (Input.GetKey(KeyCode.None) || Input.GetKeyDown(KeyCode.None) || Input.GetKeyUp(KeyCode.None))
            return;
        bIsPressed = Input.GetKey(KeyName);
        bWasPressed = Input.GetKeyDown(KeyName);
        bWasReleased = Input.GetKeyUp(KeyName);
    }


    public void SetButtonState(bool wasPressed, bool wasReleased, bool pressing, bool BwasPressed, bool BwasReleased, bool Bpressing)
    {

        WasPressed = wasPressed;
        WasReleased = wasReleased;
        IsPressed = pressing;

        bWasPressed = BwasPressed;
        bWasReleased = BwasPressed;
        bIsPressed = bIsPressed;
    }
}

public enum InputReference
{
    Jump, Sprint, Crouch/*,AbilityEnter,AbilityExit, Roll, Crouch, Crawl, Drop, Interact,
    Toggle, RightWeapon, LeftWeapon, Zoom,
    Fire, Reload, Action01, Action02, Action03*/
}
public class InputHandle : MonoBehaviour
{

    [Tooltip("Camera used in the scene")] [SerializeField] private Transform m_Camera;
    public InputButton jumpButton { get; private set; }
    public InputButton sprintKey { get; private set; }
    public InputButton crouchKey { get; private set; }
    //  public InputButton AbilityEnterButton {get; private set;}
    //  public InputButton AbilityExitButton {get; private set;}


    //[SerializeField]private KeyCode m_JumpInputName = KeyCode.Space;
    [SerializeField] private string m_JumpInputName = "Jump";
    [SerializeField] private KeyCode m_SprintInputName = KeyCode.LeftShift;
    [SerializeField] private KeyCode m_CrouchInputName = KeyCode.C;
    ACEFreeLook[] m_FreeLookCameras;


    private Vector2 m_Move;
    private Vector2 m_ScrollView;
    private Vector3 m_RelativeInput;

    public Vector3 Move { get { return m_Move; } set { m_Move = value; } }
    public Vector3 RelativeInput { get { return m_RelativeInput; } }
    private void Awake()
    {
        // Initialize buttons
        jumpButton = new InputButton(m_JumpInputName);
        sprintKey = new InputButton(m_SprintInputName);
        crouchKey = new InputButton(m_CrouchInputName);

        // Find main camera if it was not attached in hierarchy
        if (m_Camera == null)
        {
            if (Camera.main == null)
            {
                Debug.LogError("There is no Camera to render the scene. Please add a camera component !");
            }
            else
                m_Camera = Camera.main.transform;

        }
        //        ace.ResolveLookAt(m_LookAt);
        m_FreeLookCameras = FindObjectsOfType<ACEFreeLook>();


    }

    private void FixedUpdate()
    {

        m_Move.x = Input.GetAxis("Horizontal");
        m_Move.y = Input.GetAxis("Vertical");
        m_ScrollView.x = Input.GetAxis("Mouse X");
        m_ScrollView.y = Input.GetAxis("Mouse Y");

        // calculate camera relative direction to move:
        Vector3 CamForward = Vector3.Scale(m_Camera.forward, new Vector3(1, 0, 1)).normalized;
        m_RelativeInput = m_Move.y * CamForward + m_Move.x * m_Camera.right;
    }

    private void Update()
    {

        //  Debug.Log(jumpButton);
        //  GetInputReference(InputReference.Jump);
        jumpButton.OnButtonUpdate();
        sprintKey.OnKeyUpdate();
        crouchKey.OnKeyUpdate();

        foreach (ACEFreeLook freeLook in m_FreeLookCameras)
        {
            if (freeLook.IsValid)
            {
                freeLook.m_XAxis.m_InputAxisValue = m_ScrollView.x;
                freeLook.m_YAxis.m_InputAxisValue = m_ScrollView.y;
            }
        }
    }

    // public bool isSprintKeyDown() {return Input.GetKey(Sprint);}
    // public bool isJumpKeyPressed() {return Input.GetKeyUp(Jump);}
    // public bool ExitTestAbility() {return Input.GetKeyDown(ExitAbility);}
    // public bool EnterTestAbility() {return Input.GetKeyDown(EnterAbility);}

    public InputButton GetInputReference(InputReference reference)
    {
        Debug.Log(jumpButton);
        // Debug.Log(reference);
        switch (reference)
        {

            case InputReference.Jump:
                return jumpButton;

            case InputReference.Sprint:
                return sprintKey;
            case InputReference.Crouch:
                return crouchKey;


            default:
                return sprintKey;

                /*case InputReference.Roll:
                    return rollButton;
                case InputReference.Crouch:
                    return crouchButton;
                case InputReference.Crawl:
                    return crawlButton;
                case InputReference.Drop:
                    return dropButton;
                case InputReference.Interact:
                    return interactButton;
                case InputReference.Toggle:
                    return toggleWeaponButton;
                case InputReference.RightWeapon:
                    return rightWeaponButton;
                case InputReference.LeftWeapon:
                    return leftWeaponButton;
                case InputReference.Zoom:
                    return zoomButton;
                case InputReference.Fire:
                    return fireButton;
                case InputReference.Reload:
                    return reloadButton;
                case InputReference.Action01:
                    return action01;
                case InputReference.Action02:
                    return action02;
                case InputReference.Action03:
                    return action03;
                default:
                    return walkButton;*/
        }
    }
}


