using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrouchAbility : ThirdPersonAbility
{
    [SerializeField] private LayerMask m_ObstacleMask = (1 << 0) | (1 << 14) | (1 << 17) | (1 << 18) | (1 << 19) | (1 << 25);

    [Tooltip("Height to enter crouch ability")] [SerializeField] private float m_CapsuleHeight = 1f;
    [Tooltip("If on entering a region with lower height, should the system auto crouch character?")]
    [SerializeField] private bool m_AutoCrouch = true;

    bool ManualCrouch = false;
    bool IsHeightEnoughToCrouch = true;


    public override bool TryUpdateEnterAbility()
    {
        if (m_System.m_IsGrounded && m_InputManager.crouchKey.bWasReleased)
        {
            ManualCrouch = true;
            return true;
        }
        return false;
    }

    public override bool ForceEnterAbility()
    {
        return (!IsFreeAbove() && IsHeightEnoughToCrouch && m_AutoCrouch); //&& !(m_System.ActiveAbility is CrawlAbility);
    }
    public override void OnEnterAbility()
    {
        base.OnEnterAbility();
        m_System.ScaleCollider(m_CapsuleHeight);
        m_AnimatorManager.PerformBoolEvent("EnterCrouchAbility", true);


    }
    public override void FixedUpdateAbility()
    {
        base.FixedUpdateAbility();
        m_System.CalcMovVars();
        m_System.UpdateMovementAnimator();

        m_System.RotateToDirection();

    }

    public override bool TryUpdateExitAbility()
    {
        if (ManualCrouch)
            return (!m_System.m_IsGrounded || m_InputManager.crouchKey.bWasReleased && IsFreeAbove());
        else
            return (!m_System.m_IsGrounded || IsFreeAbove());
    }
    public override void OnExitAbility()
    {
        base.OnExitAbility();
        m_AnimatorManager.PerformBoolEvent("EnterCrouchAbility", false);
        ManualCrouch = false;
    }

    private bool IsFreeAbove()
    {
        Vector3 start = m_System.GroundPoint();
        RaycastHit hit;
        if (Physics.SphereCast(start, m_System.m_Collider.radius, Vector3.up, out hit, m_System.CapsuleOriginalHeight, m_ObstacleMask))
        {
            if (hit.distance <= m_System.CapsuleOriginalHeight)
            {
                IsHeightEnoughToCrouch = hit.distance + m_System.m_Collider.radius >= m_CapsuleHeight;
                return false;
            }
        }

        return true;
    }

}
