using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Developed by C-Lex31 (uid 31)
//Contact cpplexicon@gmail.com
public class LadderAbility : ThirdPersonAbility
{
    private LadderVolume currentLadder;

    [Tooltip("Box cast size")] [SerializeField] private Vector3 m_BoxCastSize = new Vector3(1.2f, 0.75f, 1.2f);
    public Vector3 GrabPosition { get { return transform.position + Vector3.up * m_System.m_Collider.height; } }
    private enum LadderCastResult { Bottom, Both, Top, Noone }
    private LadderCastResult CurrentCastResult = LadderCastResult.Noone;
    private Collider m_CurrentLadderCollider;
    public bool HasTriggeredLadder;
    private Vector3 InputMove
    {
        get
        {
            return m_InputManager.Move;
        }
    }

    public override bool TryEnterAbility()
    {
        //   if (lt.isOnLadder)
        if (HasTriggeredLadder)
            return true;

        return false;
    }

    public override void OnEnterAbility()
    {
        m_AnimatorManager.PerformBoolEvent("StartClimbLadder", true);
        m_UseRootMotion = true;
        m_UseVerticalRootMotion = true;
        m_System.m_Collider.enabled = false;
        m_System.m_Rigidbody.useGravity = false;
        currentLadder = LadderVolume.CURRENT_LADDER;
        base.OnEnterAbility();
    }
    public override void FixedUpdateAbility()
    {
        Vector3 ladderAdjusted = currentLadder.transform.position - currentLadder.transform.forward * 0.4f;
        transform.position = Vector3.Lerp(transform.position,
            new Vector3(ladderAdjusted.x, transform.position.y, ladderAdjusted.z), 5f * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation,
            Quaternion.LookRotation(currentLadder.transform.forward), 5f * Time.deltaTime);


        if (InputMove.y > 0.1f)
            m_AnimatorManager.SetFloatParameter("Vertical", 1, 0.2f);
        else if (InputMove.y < -0.1f)
            m_AnimatorManager.SetFloatParameter("Vertical", -1, 0.2f);
        else
            m_AnimatorManager.SetFloatParameter("Vertical", 0, 0.2f);

#if USING_TRIGGER
        if (transform.position.y > currentLadder.transform.position.y + (currentLadder.MainCollider.size.y - 2.5f))
        {
            if (currentLadder.offAtTop && InputMove.y > 0.1f)
            {

                m_AnimatorManager.TriggerState("GetOff");

                //  isTransitioning = true;
                return;
            }

            //  forward = Mathf.Clamp(forward, -1f, 0f);
        }
#endif

        CurrentCastResult =FoundLadder();
        if(CurrentCastResult ==LadderCastResult.Bottom)
        {
            Debug.Log("ClimbUp");
        }
    }


    /// <summary>
    /// Overlap ladders around and return Cast Result
    /// </summary>
    /// <returns></returns>
    private LadderCastResult FoundLadder()
    {
        Vector3 topCenter = GrabPosition;
        Vector3 bottomCenter = transform.position + Vector3.down * m_BoxCastSize.y * 0.5f;

        Collider[] topColliders = Physics.OverlapBox(topCenter, m_BoxCastSize * 0.5f, transform.rotation, 0, QueryTriggerInteraction.Collide);
        Collider[] bottomColliders = Physics.OverlapBox(bottomCenter, m_BoxCastSize * 0.5f, transform.rotation, 0, QueryTriggerInteraction.Collide);

        if (topColliders.Length > 0 && bottomColliders.Length > 0)
        {
            m_CurrentLadderCollider = topColliders[0];
            return LadderCastResult.Both;
        }

        if (topColliders.Length > 0)
        {
            m_CurrentLadderCollider = topColliders[0];
            return LadderCastResult.Top;
        }

        if (bottomColliders.Length > 0)
        {
            m_CurrentLadderCollider = bottomColliders[0];
            return LadderCastResult.Bottom;
        }

        return LadderCastResult.Noone;
    }



}
