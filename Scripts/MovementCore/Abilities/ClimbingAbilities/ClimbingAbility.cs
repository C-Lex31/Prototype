using UnityEngine;

public enum MovementInputType { Relative, Absolute }
public class ClimbingAbility : ThirdPersonAbstractClimbing
{
    private ClimbIKHandle m_ClimbIK;
    private ClimbJump m_ClimbJump;

    private string startState = string.Empty;
    /// <summary>
    /// Desired direction of the jump in a ledge jump
    /// </summary>
    private Vector3 m_JumpDirection = Vector3.zero;
    /// <summary>
    /// Desired climb jump type
    /// </summary>
    private ClimbJumpType m_ClimbJumpType = ClimbJumpType.Back;

    [SerializeField] private string m_BraceGrabTopState = "Climb.Brace From Top";
    [SerializeField] private string m_HangGrabState = "Climb.Begin Hang";

    [Tooltip("Offset from ledge  to be applied when character is hanging")]
    [SerializeField] private Vector3 m_CharacterOffsetOnHang = new Vector3(0, 1.5f, 0.3f);
    private bool bWallOnFoot; // Is there wall in front of feet?
    private float timeWithoutFoundLedge = 0; // Control how much time the actor stays to try climbing but does not find ledge
    private bool SystemCoroutinePlaying = false;
    public bool RefHasFoundLedge = false;
    [SerializeField] private MovementInputType m_MovementInput = MovementInputType.Absolute;

    private Vector3 InputMove
    {
        get
        {
            return (m_MovementInput == MovementInputType.Absolute) ? m_InputManager.Move :
                new Vector3(m_System.FreeMoveDirection.x, m_System.FreeMoveDirection.z);
        }
    }
    protected override void Awake()
    {
        base.Awake();
        m_ClimbIK = GetComponent<ClimbIKHandle>();
    }

    public override void Initialize(ThirdPersonSystem mainSystem, AnimManager animatorManager, InputHandle inputManager)
    {
        base.Initialize(mainSystem, animatorManager, inputManager);

        m_ClimbJump = m_System.CharacterAbilities.Find(x => x is ClimbJump) as ClimbJump;
        //   m_WallRun = m_System.CharacterAbilities.Find(x => x is WallRun) as WallRun;
    }

    /// <summary>
    /// Check conditions to choose the right enter state for climbing
    /// </summary>
    /// <returns>Correct enter state to climb</returns>
    public override string GetEnterState()
    {
        // Check if character is above ledge
        bool IsPlayerAboveLedge = (transform.position.y + characterOffset.y) > topHit.point.y && m_System.m_Rigidbody.velocity.y < -2f;

        WallOnFeet();
        // Check if there is a wall to place feet
        if (bWallOnFoot)
        {
#if Parkour
            float rightDot = Vector3.Dot(frontHit.normal, transform.right);
            float leftDot = Vector3.Dot(frontHit.normal, -transform.right);

            if (m_System.LastAbility == m_ClimbJump && m_ClimbJump.JumpType == ClimbJumpType.Right || rightDot > 0.5f)
                return GetSideState(m_RightSubState, m_HopSideEndState);

            if (m_System.LastAbility == m_ClimbJump && m_ClimbJump.JumpType == ClimbJumpType.Left || leftDot > 0.5f)
                return GetSideState(m_LeftSubState, m_HopSideEndState);

            if (m_ClimbJump.JumpType == ClimbJumpType.Up && m_System.LastAbility == m_ClimbJump)
                return m_HopUpEndState;


            if (m_System.LastAbility == m_WallRun)
            {
                return m_WallRun.CharWallDirection == WallDirection.Right ?
                     GetSideState(m_RightSubState, m_HopSideEndState) : GetSideState(m_LeftSubState, m_HopSideEndState);
            }
#endif

            if (IsPlayerAboveLedge || (m_System.LastAbility == m_ClimbJump && m_ClimbJump.JumpType == ClimbJumpType.Back) || m_System.LastAbility is FallAbility)
                return m_BraceGrabTopState;
        }
        else
            return m_HangGrabState;

        return base.GetEnterState();
    }

    public override bool TryEnterAbility()
    {

        //Check if climb jump is active
        if (m_ClimbJump.Active)
        {
            float multiplier = (m_ClimbJump.JumpType == ClimbJumpType.Right || m_ClimbJump.JumpType == ClimbJumpType.Left) ?
                0.7f : 0.9f;
            if (m_ClimbJump.JumpTimeToTarget * multiplier + m_ClimbJump.AbilityEnterFixedTime > Time.fixedTime)
                return false;
        }

        if (HasFoundLedge(out frontHit, false))
        {
            //   if (m_HasJumpFromLedge && m_CurrentLedgeTransform == frontHit.transform)
            //  return false;
            RefHasFoundLedge = true;
            WallOnFeet();
            return true;
        }

        return base.TryEnterAbility();
    }
    public override void OnEnterAbility()
    {
        base.OnEnterAbility();
        startState = GetEnterState();
        //       m_FinishOnAnimationEnd = false;
        m_AnimatorManager.PerformBoolEvent("Brace From Down", true);

    }


    /// <summary>
    /// Check if exists wall in front of feets
    /// </summary>
    private void WallOnFeet()
    {
        Vector3 Start = transform.position - transform.forward * m_CastCapsuleRadius * 2; // Set Start position to cast

        RaycastHit wallHit;
        Vector3 direction = (frontHit.point - transform.position).normalized; // Get direction to cast
        direction.y = 0;

        // Cast both feet
        bool rightWall = Physics.SphereCast(Start + Vector3.right * 0.25f, m_CastCapsuleRadius * 0.15f, direction, out wallHit, m_MaxDistanceToFindLedge + m_CastCapsuleRadius * 2 + 0.5f, m_System.GroundMask);
        bool leftWall = Physics.SphereCast(Start + Vector3.left * 0.25f, m_CastCapsuleRadius * 0.15f, direction, out wallHit, m_MaxDistanceToFindLedge + m_CastCapsuleRadius * 2 + 0.5f, m_System.GroundMask);

        // Only set bWallOnFoot bool if both feet have the same value
        if (rightWall && leftWall)
            bWallOnFoot = true;

        if (!rightWall && !leftWall)
            bWallOnFoot = false;

        if (Time.fixedTime - AbilityEnterFixedTime < 0.1f)
        {
            if (!rightWall || !leftWall)
                bWallOnFoot = false;
        }
    }

    public override void FixedUpdateAbility()
    {
        base.FixedUpdateAbility();
        characterOffset = (bWallOnFoot) ? m_CharacterOffsetFromLedge : m_CharacterOffsetOnHang;
        m_System.UpdatePositionOnMovableObject(m_CurrentLedgeTransform);

        //       if (m_FinishOnAnimationEnd)
        //        return;

        if (HasFoundLedge(out frontHit, true))
        {
            WallOnFeet();
            timeWithoutFoundLedge = 0;
            if (bWallOnFoot)
            {
                //  if (m_System.m_Animator.GetCurrentAnimatorStateInfo(AnimManager.BaseLayerIndex).IsTag("Hang"))
                // StartCoroutine(ChangeClimbType(ClimbType.Braced));
            }
            else
            {
                // if (m_System.m_Animator.GetCurrentAnimatorStateInfo(AnimManager.BaseLayerIndex).IsTag("Brace"))
                // StartCoroutine(ChangeClimbType(ClimbType.Hang));
            }

            m_CurrentLedgeTransform = topHit.transform; // Set current ledge as the ledge that character is holding
            if (!SystemCoroutinePlaying)
            {
                SetCharacterPositionOnLedge();
                m_ClimbIK.RunIK(topHit, m_ClimbableMask, m_CurrentLedgeTransform);
            }
        }
        else
            timeWithoutFoundLedge += Time.deltaTime; // Count time without finding ledge

    }

    public override void UpdateAbility()
    {
        base.UpdateAbility();


        //Climb up
        if (InputMove.y > 0.99f && Mathf.Abs(InputMove.x) < 0.3f && m_AnimatorManager.IsPlayingState("Braced Idle", 0))
        {
            if (FreeAboveLedge())
            {
                // -------------- CLIMB UP --------------- //
                m_System.m_Collider.enabled = false;
                string state = (bWallOnFoot) ? "BraceClimbUp" : "HangClimbUp";
                m_AnimatorManager.PerformBoolEvent(state, true);
                SystemCoroutinePlaying = true;
                //  m_FinishOnAnimationEnd = true;

                //   return;

                // ------------------------------------- //
            }
            // InputControl();
        }
    }


    public override bool TryExitAbility()
    {
        // if system don't find ledge for a time, exit ability
        if (timeWithoutFoundLedge >= 1.0f)
        // if(HasFoundLedge(out frontHit, true) ==false)
        {
            timeWithoutFoundLedge = 0;
            return true;
        }

        return base.TryExitAbility();
    }

    public override void OnExitAbility()
    {
        m_FinishOnAnimationEnd = false;
        // m_ClimbJump.StartClimbJump(m_ClimbJumpType, m_JumpDirection, GrabPosition, m_VerticalLinecastStartPoint,
        //               UseLaunchMath, CurrentLedgeTransform.GetComponent<Collider>());

        m_System.UpdatePositionOnMovableObject(null);
        m_AnimatorManager.PerformBoolEvent("Brace From Down", false);
        m_AnimatorManager.PerformBoolEvent("BraceClimbUp", false);
        m_AnimatorManager.PerformBoolEvent("HangClimbUp", false);
        m_AnimatorManager.PerformBoolEvent("LedgeGrab", false);
        SystemCoroutinePlaying = false;
        RefHasFoundLedge = false;
        base.OnExitAbility();
    }



}