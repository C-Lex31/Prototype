using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CoverMovementInputType { Relative, Absolute }
public class CoverAbility : ThirdPersonAbility
{
    public LayerMask CoverMask;
    public Transform CastPoint_1;
    public Transform CastPoint_2;
    public Transform CastPoint_3;
    public Transform CastPoint_4;
    RaycastHit CoverHitInfo;
    [SerializeField] private CoverMovementInputType m_MovementInput = CoverMovementInputType.Relative;
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
        // Physics.Raycast()
        // Physics.Raycast(CastPoint_3.localPosition , transform.forward, 1.5f,CoverMask) && Physics.Raycast(CastPoint_4.localPosition, transform.forward, 1.5f,CoverMask);
        if (Physics.Raycast(CastPoint_3.position, transform.forward, out CoverHitInfo, 1f, CoverMask) && Physics.Raycast(CastPoint_4.position, transform.forward, out CoverHitInfo, 1f, CoverMask))
        {

            return true;
        }
        else
            return false;
    }

    public override void OnEnterAbility()
    {
        base.OnEnterAbility();
        m_AnimatorManager.PerformBoolEvent("IsInCover", true);
        transform.rotation = Quaternion.Lerp(transform.rotation,
          Quaternion.LookRotation(CoverHitInfo.normal), 20f);

    }
    public override void FixedUpdateAbility()
    {
        base.FixedUpdateAbility();
        transform.rotation = Quaternion.Lerp(transform.rotation,
         Quaternion.LookRotation(CoverHitInfo.normal), 20f);
        if (InputMove.x < -0.1f)
            m_AnimatorManager.SetFloatParameter("Horizontal", -1);
        //  else
        //    m_AnimatorManager.SetFloatParameter("Horizontal", 0);
        else if (InputMove.x > 0.1f)
            m_AnimatorManager.SetFloatParameter("Horizontal", 1);
        else
            m_AnimatorManager.SetFloatParameter("Horizontal", 0);

        if(!(Physics.Raycast(CastPoint_4.position,-transform.forward,1.0f,CoverMask)))
        {
            m_AnimatorManager.SetFloatParameter("Horizontal", 0);
        }


Debug.DrawRay(CastPoint_3.position, transform.forward,Color.yellow);
Debug.DrawRay(CastPoint_4.position, transform.forward,Color.green);

    }
}
