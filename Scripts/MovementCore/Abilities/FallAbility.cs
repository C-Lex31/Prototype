using System.Collections;
using System.Collections.Generic;


public class FallAbility : ThirdPersonAbility
{
    private bool SoftLand, Drop;
    public override bool TryEnterAbility()
    {

        return (!m_System.m_IsGrounded && m_System.m_Rigidbody.velocity.y < 0f); // Only fall if velocity in y is lower than 0
    }

    public override void OnEnterAbility()
    {
        base.OnEnterAbility();
        if (m_System.GroundHitInfo.distance > 1.5f/*&& m_System.GroundHitInfo.distance<=6f*/)
        {
            m_AnimatorManager.PerformBoolEvent("InitiateFall", true);
            Drop = true;
        }
        

    }

    public override bool TryExitAbility()
    {
        if (m_System.m_IsGrounded)
        {
            if (m_System.GroundHitInfo.distance <= 1.5f)
                SoftLand = true;

            m_UseRootMotion = true; // use root motion to avoid character keep moving
            m_FinishOnAnimationEnd = true;

            return true;
        }
        return false;
    }
    public override void OnExitAbility()
    {
        base.OnExitAbility();
        m_AnimatorManager.PerformBoolEvent("InitiateFall", false);
        m_AnimatorManager.PerformBoolEvent("HasTouchedGround", true);
        m_AnimatorManager.PerformBoolEvent("TransitionBreak", false);

        m_AnimatorManager.PerformBoolEvent("SoftLand", SoftLand);
        m_AnimatorManager.PerformBoolEvent("DropRoll", Drop);

        m_UseRootMotion = false;
    }
}
