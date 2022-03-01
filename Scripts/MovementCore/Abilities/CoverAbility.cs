using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CoverMovementInputType { Relative, Absolute }
public enum CoverType { Stand, Crouch, MidCrouch }
public class CoverAbility : ThirdPersonAbility
{
    public LayerMask CoverMask;
    public Transform CastPoint_1;
    public Transform CastPoint_1H;
     public Transform CastPoint_2H;
    public Transform CastPoint_2;
    public Transform CastPoint_3;
    public Transform CastPoint_4;

    [SerializeField] private CoverMovementInputType m_MovementInput = CoverMovementInputType.Relative;
    //------------------------------INTERNAL VARS AND COMPONENTS--------------------------
    RaycastHit CoverHitInfo;
    CoverType type;

 
    //-------------------------------------------------------------------------------------
    private Vector3 InputMove
    {

        get
        {
            return (m_MovementInput == CoverMovementInputType.Absolute) ? m_InputManager.Move :
                new Vector3(-m_System.FreeMoveDirection.x, m_System.FreeMoveDirection.z);
        }

    }
    public override bool TryEnterAbility()
    {
        if (Physics.Raycast(CastPoint_3.position, transform.forward, out CoverHitInfo, 0.65f, CoverMask) && Physics.Raycast(CastPoint_4.position, transform.forward, out CoverHitInfo, 0.65f, CoverMask))
        {
            type = CoverType.Stand;
            return true;
        }
        else if (Physics.Raycast(CastPoint_1.position, transform.forward, out CoverHitInfo, 0.65f, CoverMask) && Physics.Raycast(CastPoint_2.position, transform.forward, out CoverHitInfo, 0.65f, CoverMask))
        {
            type = CoverType.Crouch;
           
            return true;
        }
        else
            return false;
    }

    public override void OnEnterAbility()
    {
        base.OnEnterAbility();
        if (type is CoverType.Stand)
        {
            m_AnimatorManager.PerformBoolEvent("IsInCover", true);
            transform.rotation = Quaternion.Lerp(transform.rotation,
          Quaternion.LookRotation(CoverHitInfo.normal), 20f);
        }
        if (type is CoverType.Crouch)
        {
            m_AnimatorManager.PerformBoolEvent("IsInCrouchCover", true);
            
            transform.rotation = Quaternion.Lerp(transform.rotation,
                                   Quaternion.LookRotation(Vector3.Cross(CoverHitInfo.normal, Vector3.up)), 20f *Time.deltaTime );
        }
    }
    public override void FixedUpdateAbility()
    {
        base.FixedUpdateAbility();
        if (type is CoverType.Stand)
            StandCover();
        if (type is CoverType.Crouch)
            CrouchCover();
    }
    void StandCover()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation,
               Quaternion.LookRotation(CoverHitInfo.normal), 20f);


        //-------------------------------MOVEMENT INPUTS---------------------------------------------------------
        if (InputMove.x < -0.1f && (Physics.Raycast(CastPoint_3.position, -transform.forward, 1.0f, CoverMask)))
            m_AnimatorManager.SetFloatParameter("Horizontal", -1);

        else if (InputMove.x > 0.1f && (Physics.Raycast(CastPoint_4.position, -transform.forward, 1.0f, CoverMask)))
            m_AnimatorManager.SetFloatParameter("Horizontal", 1);
        else
        {
            m_AnimatorManager.SetFloatParameter("Horizontal", 0);

            if (InputMove.x > 0.1f)
            {
                m_AnimatorManager.SetFloatParameter("Peek", 1);

            }
            else if (InputMove.x < -0.1f)
            {
                m_AnimatorManager.SetFloatParameter("Peek", -1);
            }
            else
                m_AnimatorManager.SetFloatParameter("Peek", 0);

        }
        //-------------------------------------------------------------------------------------------------------
        Debug.DrawRay(CastPoint_3.position, transform.forward, Color.yellow);
        Debug.DrawRay(CastPoint_4.position, transform.forward, Color.green);
    }

    void CrouchCover()
    {
        transform.rotation = Quaternion.Lerp( transform.rotation,Quaternion.LookRotation(Vector3.Cross(CoverHitInfo.normal, Vector3.up)),
                       20f * Time.deltaTime);
      
       
        if (InputMove.y < -0.1f && (Physics.Raycast(CastPoint_1H.position,Vector3.Cross(transform.forward,Vector3.up)  , 2.0f, CoverMask)))
            m_AnimatorManager.SetFloatParameter("Horizontal", -1);

        else if (InputMove.y > 0.1f && (Physics.Raycast(CastPoint_2H.position, Vector3.Cross(transform.forward,Vector3.up) , 2.0f, CoverMask)))
            m_AnimatorManager.SetFloatParameter("Horizontal", 1);
        else
        {
            m_AnimatorManager.SetFloatParameter("Horizontal", 0);
        }
         Debug.DrawRay(CastPoint_1H.position,Vector3.Cross(transform.forward,Vector3.up) , Color.yellow);
        Debug.DrawRay(CastPoint_2H.position,Vector3.Cross(transform.forward,Vector3.up) , Color.green);
    }

}
