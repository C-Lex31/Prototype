using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class FreeLocomotionAbility : ThirdPersonAbility
{
    private bool m_Mirror = false;

    public override bool TryEnterAbility()
    {
        if (m_System.m_IsGrounded)
        {
            return true;
        }
        return false;
    }
    public override void OnEnterAbility()
    {
        base.OnEnterAbility();
        m_AnimatorManager.PerformBoolEvent("SoftLand", false);
        m_AnimatorManager.PerformBoolEvent("DropRoll", false);
        m_AnimatorManager.PerformBoolEvent("HasTouchedGround", false);

    }
    public override void FixedUpdateAbility()
    {
        base.FixedUpdateAbility();
        
        m_System.CalcMovVars();
        m_System.UpdateMovementAnimator();

    }
    public override void UpdateAbility()
    {
        base.UpdateAbility();
     
    }
    public override bool TryExitAbility()
    {

        return !m_System.m_IsGrounded;
    }
    void Reset()
    {
        // m_UseInputStateToEnter = InputEnterType.ButtonDown;
        // InputButton = InputReference.AbilityEnter;
        //    InputButton= InputReference.AbilityExit;
    }
}
