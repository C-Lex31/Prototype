using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Developed by C-Lex31 (uid 31)
//Contact cpplexicon@gmail.com

public class JumpAbility : ThirdPersonAbility
{
    // [SerializeField] private string m_JumpStart = "Air.JumpDivider";
     private float m_MaxHorSpeed;
     private float m_MaxJumpHt;
     private bool m_Mirror = false;
   //  private string startState = string.Empty;

    [Tooltip("The maximum horizontal speed that character can have during the jump")][SerializeField] private float HorizontalJumpDistance=8;
    [Tooltip("The maximum height of the character jump.")][SerializeField] private float m_MaxJumpHeight = 1.6f;
   
    private bool m_MirrorJump = false;


    public override bool TryEnterAbility()
    {
        m_AnimatorManager.SetFloatParameter("LegOffset",m_System.JumpOffset);
        if(m_System.JumpOffset==-1)
            m_AnimatorManager.PerformBoolEvent("InitiateMirror",true);
         else 
            m_AnimatorManager.PerformBoolEvent("InitiateMirror",false);


       return m_System.m_IsGrounded;
    }
    public override void OnEnterAbility()
    {
        base.OnEnterAbility();
         
        m_System.GroundCheckDistance = 0.01f;
        m_UseRootMotion = false;
        m_UseVerticalRootMotion = false;
        if(m_System.val==2)
        {
            m_MaxHorSpeed = 8.0f;
            m_MaxJumpHt =1.78f;
        }
        else{ m_MaxHorSpeed= HorizontalJumpDistance;m_MaxJumpHt=m_MaxJumpHeight;}

        float VerticalSpeed= Mathf.Sqrt(-2 * Physics.gravity.y *m_MaxJumpHt);
        m_System.m_IsGrounded = false;
  
        DoJump(VerticalSpeed);
       
    }
    public override bool TryExitAbility()
    {
        return !m_System.m_IsGrounded && !m_UseRootMotion;
    }

    public override void OnExitAbility()
    {

        base.OnExitAbility();
        
        m_AnimatorManager.PerformBoolEvent("InitiateJump",false);
        m_UseRootMotion = false;
        m_UseVerticalRootMotion = false;
          
    }
    public override void FixedUpdateAbility()
      {
          base.FixedUpdateAbility();
   
          Vector3 vel = transform.forward * m_MaxHorSpeed; // Set velocity vector
          vel.y = m_System.m_Rigidbody.velocity.y; // Keep vertical speed
          m_System.m_Rigidbody.velocity = vel; // Set new velocity
      }

      void DoJump(float power)
      {
          m_AnimatorManager.PerformBoolEvent("InitiateJump",true);
          Vector3 direction = m_InputManager.RelativeInput.normalized;
          Vector3 velocity = direction * m_MaxHorSpeed + Vector3.up * power;
      //   m_System.m_Rigidbody.velocity = new Vector3(m_System.m_Rigidbody.velocity.x, power, m_System.m_Rigidbody.velocity.z);
          m_System.m_Rigidbody.velocity=velocity;
          //Get Rotation target
        transform.rotation = GetRotationFromDirection(direction);
      }
        private void Reset()
      {
           m_EnterState = "Air.FallingLoop";
      }


}
