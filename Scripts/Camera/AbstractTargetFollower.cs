using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Developed by C-Lex31 (uid 31)
//Contact cpplexicon@gmail.com
public abstract class AbstractTargetFollower : MonoBehaviour
{

    public enum UpdateType
    {
        FixedUpdate, // Update in FixedUpdate (for tracking rigidbodies).
        LateUpdate, // Update in LateUpdate. (for tracking objects that are moved in Update)
        ManualUpdate
    }

    [Tooltip("Target to follow")] [SerializeField] protected Transform m_Target;
    [Tooltip("Whether the rig should auto target the actor")] [SerializeField] private bool m_AutoTargetActor = true;
    [SerializeField] private UpdateType m_UpdateType;

    protected Rigidbody TargetBody;

    protected virtual void Start()
    {
        if (m_AutoTargetActor)
        {
            // if auto targeting is used, find the object tagged "Player"
            // any class inheriting from this should call base.Start() to perform this action
            FindAndFocusActor();
        }
        if (m_Target == null) return;
        TargetBody = m_Target.GetComponent<Rigidbody>();
    }
   virtual protected void FixedUpdate()
    {
        // we update from here if updatetype is set to Fixed, or in auto mode,
        // if the target has a rigidbody, and isn't kinematic.
        if (m_AutoTargetActor && (m_Target == null || !m_Target.gameObject.activeSelf))
        {
            FindAndFocusActor();
        }
        if (m_UpdateType == UpdateType.FixedUpdate)
        {
            FollowTarget(Time.deltaTime);
        }

    }
   virtual protected void LateUpdate()
    {
        // we update from here if updatetype is set to Late, or in auto mode,
        // if the target does not have a rigidbody, or - does have a rigidbody but is set to kinematic.
        if (m_AutoTargetActor && (m_Target == null || !m_Target.gameObject.activeSelf))
        {
            FindAndFocusActor();
        }
        if (m_UpdateType == UpdateType.LateUpdate)
        {
            FollowTarget(Time.deltaTime);
        }
    }


    public void ManualUpdate()
    {
        // we update from here if updatetype is set to Late, or in auto mode,
        // if the target does not have a rigidbody, or - does have a rigidbody but is set to kinematic.
        if (m_AutoTargetActor && (m_Target == null || !m_Target.gameObject.activeSelf))
        {
            //           FindAndFocusActor();
        }
        if (m_UpdateType == UpdateType.ManualUpdate)
        {
            FollowTarget(Time.deltaTime);
        }
    }



    protected abstract void FollowTarget(float deltaTime);

    public void FindAndFocusActor()
    {
        // auto target an object tagged player, if no target has been assigned
        var targetObj = GameObject.FindGameObjectWithTag("Player");
        if (targetObj)
        {
            SetTarget(targetObj.transform);
        }

    }
    public virtual void SetTarget(Transform newTransform)
    {
        m_Target = newTransform;
    }
    public Transform Target
    {
        get { return m_Target; }
    }

}
