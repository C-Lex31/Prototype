
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//Developed by C-Lex31 (uid 31)
//Contact cpplexicon@gmail.com
// Base class for all locomotion related stuff 

public enum InputEnterType { ButtonDown, ButtonPressing, ButtonReleased, KeyDown, KeyPressing, KeyReleased, Noone }

public abstract class ThirdPersonAbility : MonoBehaviour
{
    public bool Active { get; protected set; } = false; // Controls if this ability is active or not
    public bool Blocked { get; protected set; } = false; // Controls if this ability can proccess conditions to be active

    // ------------------------------------------- ANIMATIONS ----------------------------------------------------------------------------- //

    [Tooltip("Name of enter state of ability")] [SerializeField] protected string m_EnterState = "";
    [Tooltip("Animation transition duration to play this animation")] [SerializeField] protected float m_TransitionDuration = 0.1f;
    [Tooltip("Multiplier of root motion velocity in all axis")] [SerializeField] protected Vector3 m_RootMotionMultiplier = Vector3.one;
    [Tooltip("Should exit this ability when animation ends?")] [SerializeField] protected bool m_FinishOnAnimationEnd = false;

    [Space(5)]

    // -----------------------------------------------------------------------------------------------------------------------------------//

    [Tooltip("Abilities that must be ignored to enter this ability. Means that this ability has higher priority than these ignored abilities")]
    public List<ThirdPersonAbility> IgnoreAbilities;



    [Tooltip("Should this ability uses root motion?")] [SerializeField] protected bool m_UseRootMotion = true;
    [Tooltip("Should this ability uses rotation root motion?")] [SerializeField] protected bool m_UseRotationRootMotion = false;
    [Tooltip("Should this ability uses root motion in vertical direction?")] [SerializeField] protected bool m_UseVerticalRootMotion = false;

    [SerializeField] protected bool m_AllowCameraZoom = false;                   // Allow camera zoom?

    [SerializeField] protected PhysicMaterial m_AbilityPhysicMaterial;

    // ------------------------ PUBLIC GETTERS --------------------------------- //

    public bool UseRootMotion { get { return m_UseRootMotion; } }
    public bool UseRotationRootMotion { get { return m_UseRotationRootMotion; } }
    public bool UseVerticalRootMotion { get { return m_UseVerticalRootMotion; } }
    public Vector3 RootMotionMultiplier { get { return m_RootMotionMultiplier; } }

    public bool AllowCameraZoom { get { return m_AllowCameraZoom; } }
    public float TransitionDuration { get { return m_TransitionDuration; } }

    public PhysicMaterial AbilityPhysicMaterial { get { return m_AbilityPhysicMaterial; } set { m_AbilityPhysicMaterial = value; } }
    public float AbilityEnterFixedTime { get; private set; } = 0;
    public float AbilityExitFixedTime { get; private set; } = 0;

    // ----------------------------------------------------------------------- //

    // Vars
    protected ThirdPersonSystem m_System; // Character Main Controller component reference
    protected AnimManager m_AnimatorManager; // Reference to animator controller
    protected InputHandle m_InputManager; // Reference to Input Manager

    public UnityEvent OnEnterAbilityEvent, OnExitAbilityEvent;

    protected string m_CurrentStatePlaying = ""; // Get current state playing
    public string CurrentStatePlaying { get { return m_CurrentStatePlaying; } }

    protected InputButton m_InputToEnter = null;

    [SerializeField] protected InputEnterType m_UseInputStateToEnter = InputEnterType.Noone;
    [SerializeField] protected InputReference InputButton;

    protected bool m_InputStateSet = false;
    /// <summary>
    /// Initialize ability and get all references that an ability needs to run
    /// </summary>
    /// <param name="mainSystem"></param>
    /// <param name="animatorManager"></param>
    /// <param name="inputManager"></param>
    public virtual void Initialize(ThirdPersonSystem mainSystem, AnimManager animatorManager, InputHandle inputManager)
    {
        m_System = mainSystem;
        m_AnimatorManager = animatorManager;
        m_InputManager = inputManager;
        //   Debug.Log(InputButton);
        m_InputToEnter = m_InputManager.GetInputReference(InputButton);

    }

    /// <summary>
    /// Constantly check to enter or leave ability
    /// </summary>
    protected virtual void FixedUpdate()
    {
        if (Blocked)
        {
            if (Active)
                m_System.ExitAbility(this);

            return;
        }

        // Check if it's active
        if (Active)
        {
            if (m_System.enabled == false)
            {
                m_System.ExitAbility(this);
                return;
            }
            // Check animation finish condition
            if (m_FinishOnAnimationEnd && m_AnimatorManager.HasFinishedAnimation(m_CurrentStatePlaying))
            {
                m_System.ExitAbility(this); // Exit ability in the end of the animation
                return;
            }

            // check conditions to exit ability
            if (TryExitAbility())
                m_System.ExitAbility(this); // Exit ability from controller
        }
        else
        {
            if (ForceEnterAbility())
                m_System.OnTryEnterAbility(this); // enter ability
            else
            {
                // check condition to enter this ability
                if (m_InputStateSet)
                {
                    //   Debug.Log(m_InputStateSet);
                    if (TryEnterAbility())
                        m_System.OnTryEnterAbility(this); // enter ability


                }
            }
        }

        m_InputStateSet = false;


    }

    protected virtual void Update()
    {
        if (Time.timeScale <= 0.05f || Blocked)
            return;

        SetButtonState();

        if (Active)
        {
            // Check animation finish condition
            if (m_FinishOnAnimationEnd && m_AnimatorManager.HasFinishedAnimation(m_CurrentStatePlaying))
            {
                m_System.ExitAbility(this); // Exit ability in the end of the animation
                return;
            }

            // check conditions to exit ability
            if (TryUpdateExitAbility())
                m_System.ExitAbility(this); // Exit ability from controller
        }
        else
        {
            if (ForceUpdateEnterAbility())
                m_System.OnTryUpdateEnterAbility(this); // enter ability
            else
            {
                // check condition to enter this ability
                if (m_InputStateSet)
                {
                    if (TryUpdateEnterAbility())
                        m_System.OnTryUpdateEnterAbility(this); // enter ability
                }
            }
        }

    }

    protected void SetButtonState()
    {
        if (m_InputToEnter == null)
            return;

        switch (m_UseInputStateToEnter)
        {
            case InputEnterType.ButtonDown:
                if (m_InputToEnter.WasPressed)
                    m_InputStateSet = true;
                break;
            case InputEnterType.ButtonPressing:
                m_InputStateSet = m_InputToEnter.IsPressed;
                break;
            case InputEnterType.ButtonReleased:
                if (m_InputToEnter.WasReleased)
                    m_InputStateSet = true;
                break;
            case InputEnterType.KeyDown:
                if (m_InputToEnter.bWasPressed)
                    m_InputStateSet = true;
                break;
            case InputEnterType.KeyPressing:
                m_InputStateSet = m_InputToEnter.bIsPressed;
                break;
            case InputEnterType.KeyReleased:
                if (m_InputToEnter.bWasReleased)
                    m_InputStateSet = true;
                break;
            case InputEnterType.Noone:
                m_InputStateSet = true;
                break;
        }
    }

    /// <summary>
    /// Method to check conditions to enter ability
    /// </summary>
    /// <returns>True: enter ability</returns>
    public virtual bool TryEnterAbility() { return false; }
    /// <summary>
    /// Method to continuously check conditions to enter ability
    /// </summary>
    /// <returns>True: enter ability</returns>
    public virtual bool TryUpdateEnterAbility() { return false; }

    /// <summary>
    /// Method to force enter this ability because some specific conditions
    /// </summary>
    /// <returns>True: enter ability</returns>
    public virtual bool ForceEnterAbility() { return false; }
    /// <summary>
    /// Method called per frame to force enter this ability because some specific conditions
    /// </summary>
    /// <returns>True: enter ability</returns>
    public virtual bool ForceUpdateEnterAbility() { return false; }

    /// <summary>
    /// Method called in the moment that ability is entered. Called once.
    /// </summary>
    public virtual void OnEnterAbility()
    {
        //OnEnterAbilityEvent.Invoke();
        Active = true;
        SetState(GetEnterState());
    }

    /// <summary>
    /// Fixed update for each ability. It's updated in the controller
    /// </summary>
    public virtual void FixedUpdateAbility()
    {
    }

    /// <summary>
    /// Update for each ability. It's updated in the controller
    /// </summary>
    public virtual void UpdateAbility()
    {
    }


    /// <summary>
    /// Method to check conditions to exit ability
    /// </summary>
    /// <returns>True: exit ability</returns>
    public virtual bool TryExitAbility() { return false; }


    /// <summary>
    /// Method called in the moment that ability exit. Called once.
    /// </summary>
    public virtual void OnExitAbility()
    {
        // OnExitAbilityEvent.Invoke();
        Active = false;
        m_CurrentStatePlaying = string.Empty;
    }

    /// <summary>
    /// Method to continuously check conditions to exit ability.
    /// </summary>
    public virtual bool TryUpdateExitAbility() { return false; }
    /// <summary>
    /// Method called in the moment that ability exits. Called once per frame in update loop.
    /// </summary>
    public virtual void OnUpdateExitAbility()
    {
        // OnExitAbilityEvent.Invoke();
        Active = false;
        m_CurrentStatePlaying = string.Empty;
    }

    /// <summary>
    /// Method to set enter state. Called only in the TryEnterAbility() method
    /// Can be overwritten to start more than one animation
    /// </summary>
    /// <returns>Name of the state</returns>
    public virtual string GetEnterState() { return m_EnterState; }

    /// Sets a new state for this ability
    /// </summary>
    /// <param name="newState">Name of the new state</param>
    protected void SetState(string newState)
    {
        SetState(newState, m_TransitionDuration);
    }
    /// <summary>
    /// Sets a new state for this ability
    /// </summary>
    /// <param name="newState">Name of the new state</param>
    /// <param name="transitionDuration">Duration of transition</param>
    protected virtual void SetState(string newState, float transitionDuration)
    {
        if (m_CurrentStatePlaying == newState)
            return;

        m_AnimatorManager.SetAnimatorState(newState, transitionDuration, AnimManager.BaseLayerIndex);
        m_CurrentStatePlaying = newState; // Set new current state
    }


    /// <summary>
    /// Get rotation to face desired direction
    /// </summary>
    /// <returns></returns>
    protected Quaternion GetRotationFromDirection(Vector3 direction)
    {
        float yaw = Mathf.Atan2(direction.x, direction.z);
        return Quaternion.Euler(0, yaw * Mathf.Rad2Deg, 0);
    }

    // Adds offset in Yaw rotation
    protected Quaternion GetRotationFromDirection(Vector3 direction, float yawOffset)
    {
        float yaw = Mathf.Atan2(direction.x, direction.z);
        return Quaternion.Euler(0, (yaw * Mathf.Rad2Deg) + yawOffset, 0);
    }

    // Check if character can walk on desired direction

    // Check if character can walk on desired direction

    public void BlockAbility()
    {
        Blocked = true;
    }

    public void UnblockAbility()
    {
        Blocked = false;
    }


    private void Reset()
    {
        m_InputToEnter = GetComponent<InputHandle>().jumpButton;
    }
}
