using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Developed by C-Lex31 (uid 31)
//Contact cpplexicon@gmail.com
public class FreeLookCam : PivotBasedCamRig
{
    [SerializeField] private float m_MoveSpeed = 1f;                      // How fast the rig will move to keep up with the target's position.
    [Range(0f, 10f)] [SerializeField] private float m_TurnSpeed = 1.5f;   // How fast the rig will rotate from user input.
    [Tooltip("Higher Values More Responsive,Lower values Higher Lag ")][SerializeField] private float m_TurnSmoothing = 0.0f;                // How much smoothing to apply to the turn input, to reduce mouse-turn jerkiness
    [SerializeField] private float m_TiltMax = 75f;                       // The maximum value of the x axis rotation of the pivot.
    [SerializeField] private float m_TiltMin = 45f;                       // The minimum value of the x axis rotation of the pivot.
    [SerializeField] private bool m_LockCursor = false;                   // Whether the cursor should be hidden and locked.
    [SerializeField] private bool m_VerticalAutoReturn = false;           // set wether or not the vertical axis should auto return
    [SerializeField] private bool isLastMouseYpos;
    [SerializeField] private bool isLastMouseYneg;
    private float x, y;
    private float m_LookAngle;                    // The rig's y axis rotation.
    private float m_TiltAngle;                    // The pivot's x axis rotation.
    private const float k_LookDistance = 100f;    // How far in front of the pivot the character's look target is.
    private Vector3 m_PivotEulers;
    private Quaternion m_PivotTargetRot;
    private Quaternion m_TransformTargetRot;
    //public static float CurrentTimeOverride = -1;
    public float CurrentTime;


    /// <summary>If no input has been detected, the camera will wait
    /// this long in seconds before moving its heading to the default heading.</summary>
    [Tooltip("If no user input has been detected on the axis, the axis will wait this long in seconds before recentering.")]
    public float m_WaitTime;
    [Tooltip("How long it takes to reach destination once recentering has started.")]
    public float m_RecenteringTime;
    [SerializeField] private float m_CurrentLastAxisInputTime;
    [SerializeField] private float m_DeltaLastAxisInputTime;

    protected override void Awake()
    {
        base.Awake();
        // Lock or unlock the cursor.
        Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !m_LockCursor;
        m_PivotEulers = m_Pivot.rotation.eulerAngles;

        m_PivotTargetRot = m_Pivot.transform.localRotation;
        m_TransformTargetRot = transform.localRotation;
    }
    protected void Update()
    {
#if UPDATE
        CurrentTime = Time.time;
        if (Time.timeScale < float.Epsilon)
            return;
        x = Input.GetAxis("Mouse X");
        y = Input.GetAxis("Mouse Y");
    //    HandleRotationMovement();
        if (m_LockCursor && Input.GetMouseButtonUp(0))
        {
            Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !m_LockCursor;
        }
#endif
    }
    protected override void LateUpdate()
    {
        base.LateUpdate();
        CurrentTime = Time.time;
        if (Time.timeScale < float.Epsilon)
            return;
        x = Input.GetAxis("Mouse X");
        y = Input.GetAxis("Mouse Y");
        //    HandleRotationMovement();
        if (m_LockCursor && Input.GetMouseButtonUp(0))
        {
            Cursor.lockState = m_LockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !m_LockCursor;
        }
        HandleRotationMovement();
    }
    protected override void FollowTarget(float deltaTime)
    {
        if (m_Target == null) return;
        // Move the rig towards target position.
        transform.position = Vector3.Lerp(transform.position, m_Target.position, deltaTime * m_MoveSpeed);
    }
    private void HandleRotationMovement()
    {
        /* if (Time.timeScale < float.Epsilon)
             return;
         var x = Input.GetAxis("Mouse X");
         var y = Input.GetAxis("Mouse Y");
         */
        m_LookAngle += x * m_TurnSpeed; // Adjusting the look angle by an amount proportional to the turn speed and horizontal input.
        m_TransformTargetRot = Quaternion.Euler(0f, m_LookAngle, 0f);
        if (y > 0)
        {
            isLastMouseYpos = true;
            isLastMouseYneg = false;
            m_CurrentLastAxisInputTime = CurrentTime;
        }
        else if (y < 0)
        {
            isLastMouseYneg = true;
            isLastMouseYpos = false;
            m_CurrentLastAxisInputTime = CurrentTime;
        }

        else if (y == 0)
        {
            m_DeltaLastAxisInputTime = m_CurrentLastAxisInputTime - CurrentTime;
        }
        if (m_VerticalAutoReturn && m_DeltaLastAxisInputTime < -1.5f)
        {
            //  m_DeltaLastAxisInputTime = m_CurrentLastAxisInputTime - CurrentTime;
            if (CurrentTime < (m_CurrentLastAxisInputTime + m_WaitTime))
                return;
            else if (y == 0)
            {
                if (isLastMouseYpos)
                {
                    m_TiltAngle = Mathf.Lerp(0, -m_TiltMin, Time.deltaTime);
                }
                else if (isLastMouseYneg)
                {
                    m_TiltAngle = Mathf.Lerp(0, m_TiltMax, -Time.deltaTime);
                }
            }
        }

        // on platforms with a mouse, we adjust the current angle based on Y mouse input and turn speed
        m_TiltAngle -= y * m_TurnSpeed;
        // and make sure the new value is within the tilt range
        m_TiltAngle = Mathf.Clamp(m_TiltAngle, -m_TiltMin, m_TiltMax);

        // Tilt input around X is applied to the pivot (the child of this object)
        m_PivotTargetRot = Quaternion.Euler(m_TiltAngle, m_PivotEulers.y, m_PivotEulers.z);

        if (m_TurnSmoothing > 0)
        {
            m_Pivot.localRotation = Quaternion.Slerp(m_Pivot.localRotation, m_PivotTargetRot, m_TurnSmoothing * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, m_TransformTargetRot, m_TurnSmoothing * Time.deltaTime);
        }
        else
        {
            m_Pivot.localRotation = m_PivotTargetRot;
            transform.localRotation = m_TransformTargetRot;

        }

    }


}
