
using UnityEngine;


public class LowerStepUpAbility : ThirdPersonAbstractClimbing
{
    public override bool TryEnterAbility()
    {
        if (!m_System.m_IsGrounded)
            return false;

        if (HasFoundLedge(out frontHit) && m_System.speed>=0.8f)
        {
            
            if (FreeAboveLedge())
                return true;
        }

        return base.TryEnterAbility();
    }

    public override void OnEnterAbility()
    {
        base.OnEnterAbility();
        m_AnimatorManager.PerformBoolEvent("StepUp", true);
        m_System.m_Collider.enabled = false; // Deactivate collider
        

    }

    public override bool TryExitAbility()
    {
        return m_AnimatorManager.HasFinishedAnimation("Climb.StepUp");
    }
    public override void OnExitAbility()
    {
        base.OnExitAbility();
        m_AnimatorManager.PerformBoolEvent("StepUp", false);
    }
    private void Reset()
    {
        m_EnterState = "Climb.Step Up";
        m_TransitionDuration = 0.1f;
        m_FinishOnAnimationEnd = true;
        m_UseRootMotion = true;
        m_UseVerticalRootMotion = true;
        m_UseLaunchMath = false;

        m_CastCapsuleRadius = 0.15f;
        m_VerticalLinecastStartPoint = 0.6f;
        m_VerticalLinecastEndPoint = 0.15f;
        m_MaxDistanceToFindLedge = 0.5f;

        m_CharacterOffsetFromLedge = new Vector3(0, 0.55f, 0.3f);
    }
}
