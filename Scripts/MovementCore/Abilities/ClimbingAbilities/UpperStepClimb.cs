
using UnityEngine;



public class UpperStepClimb : ThirdPersonAbstractClimbing
{
    public override bool TryEnterAbility()
    {
        if (HasFoundLedge(out frontHit))
        {
            if (FreeAboveLedge())
                return true;
        }

        return base.TryEnterAbility();
    }

    public override bool ForceEnterAbility()
    {
        if (m_UseInputStateToEnter == InputEnterType.Noone)
            return false;

        if (!m_System.m_IsGrounded)
            return TryEnterAbility();

        return false;
    }

    public override void OnEnterAbility()
    {
        base.OnEnterAbility();
        m_System.m_Collider.enabled = false; // Deactivate collider
    }
    public override bool TryExitAbility()
    {
        return m_AnimatorManager.HasFinishedAnimation("Climb.LowerClimb");
    }
    public override void OnExitAbility()
    {
        base.OnExitAbility();
        m_AnimatorManager.PerformBoolEvent("StepUp", false);
    }

    private void Reset()
    {
        m_EnterState = "Climb.LowerClimb";
        m_TransitionDuration = 0.1f;
        m_FinishOnAnimationEnd = true;
        m_UseRootMotion = true;
        m_UseVerticalRootMotion = true;

        m_CastCapsuleRadius = 0.2f;
        m_VerticalLinecastStartPoint = 1.1f;
        m_VerticalLinecastEndPoint = 0.4f;
        m_MaxDistanceToFindLedge = 1f;
        m_CharacterOffsetFromLedge = new Vector3(0, 0.75f, 0.45f);
    }
}

