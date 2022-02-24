using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderTrigger : MonoBehaviour
{
    [SerializeField] private bool sideClimbOn = false;
       bool isOnLadder = false;


    void OnTriggerEnter(Collider col)
    {
     
        if (!isOnLadder && col.CompareTag("Player")&& Vector3.Dot(transform.forward, col.transform.forward) > 0f)
        {
            ClimbLadder(col.gameObject.GetComponent<LadderAbility>());
            isOnLadder = true;
   
        }
    }

    private void OnTriggerExit(Collider other)
    {
        isOnLadder = false;
    }

    private void ClimbLadder(LadderAbility m_Ability)
    {


        LadderVolume.CURRENT_LADDER = transform.parent.gameObject.GetComponent<LadderVolume>();
        
        m_Ability.HasTriggeredLadder =true;
        //m_System.m_Animator.SetTrigger(sideClimbOn ? "LadderSide" : "LadderFront");

     //   m_System.StateMachine.GoToState<Ladder>();
    }
}
