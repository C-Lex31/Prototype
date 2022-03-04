using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Developed by C-Lex31 (uid 31)
//Contact cpplexicon@gmail.com
/*
The following code design follows a Finite State Machine Behaviour.
Any ability that you want to add to the Player (that does not relate to climbing stuff) must derive from the ThirdPeronAbility script.
This script contains(and is being updated) all the neccessary methods common to a Third Person Actor.
*/
public class ThirdPersonSystem : MonoBehaviour
{
    #region Components
    //-------------------------------PUBLIC--------------------------------------------------------
    public Rigidbody m_Rigidbody;
    public ThirdPersonAbility ActiveAbility { get { return m_ActiveAbility; } }

    //------------------------------PRIVATE---------------------------------------------------------  
    private ThirdPersonAbility m_ActiveAbility = null;
    private InputHandle m_InputManager;
    public Animator m_Animator;

    public CapsuleCollider m_Collider { get; set; }
    private List<ThirdPersonAbility> m_Abilities = new List<ThirdPersonAbility>();

    #endregion

    #region ExposedParams
    public bool m_IsGrounded;
    [SerializeField] private float GroundOffset = 0.12f;
    public Vector3 GroundNormal;
    [Tooltip("For Debugging Ground Detection")][SerializeField] private float m_GroundCheckSphereRadius = 0.35f;
    [SerializeField] private float m_MaxAngleSlope = 45f;
    [Tooltip("Layers to treat as a ground")][SerializeField] private LayerMask m_GroundMask = (1 << 0) | (1 << 14) | (1 << 16) | (1 << 17) | (1 << 18) | (1 << 19) | (1 << 20);
    [Range(1f, 10f)] public float m_GravityMultiplier = 2f;

    #endregion

    #region PrivateInternalParams
    private AnimManager m_AnimatorManager;
    private float m_CapsuleOriginHeight;
    private Vector3 m_CapsuleOriginCenter;
    private float m_CapsuleOriginRadius;
    private RaycastHit m_GroundHit;

    private bool TransitionBreak = false;
    private float m_LegOffset = 1f;  // For updating active leg during motion
    public float LegOffset = 1f;  //For setting the next active leg while actor is idle

    #endregion

    #region Public Params and getters
    public LayerMask GroundMask { get { return m_GroundMask; } set { m_GroundMask = value; } }
    public InputHandle InputManager { get { return m_InputManager; } }
    public float MaxAngleSlope { get { return m_MaxAngleSlope; } }
    public float CapsuleOriginalHeight { get { return m_CapsuleOriginHeight; } }
    public float GroundCheckDistance { get; set; }
    public RaycastHit GroundHitInfo { get; private set; }
    public List<ThirdPersonAbility> CharacterAbilities { get { return m_Abilities; } }
    public bool IsCoroutinePlaying { get; set; } // Avoid play more than one coroutine per time
    /// <summary>
    /// It returns the last ability played by the system
    /// </summary>
    public ThirdPersonAbility LastAbility { get; private set; } = null;
    #endregion


    #region  MovementParams
    [SerializeField] float m_MovingTurnSpeed = 360;
    [SerializeField] float m_StationaryTurnSpeed = 180;
    [SerializeField] float m_JumpPower = 12f;
    [SerializeField] float m_MoveSpeedMultiplier = 1f;
    public float m_ForwardAmount = 1.0f;
    public float Rotation_Damp;
    public float val = 0;
    public float speed;
    private float direction;



    #endregion

    #region  Xtras
    public Vector3 extraGravityForce;
    [SerializeField] float m_GravityAcceleration = 19.6f;
    public float JumpOffset;
    public Transform camera;
    private int m_DirectionId = 0;
    // public float m_TurnAmount;
    private int m_SpeedId = 0;

    public float m_SpeedDampTime = 0.1f;
    private int m_angleDirId = 0;
    private bool HasPredictedLedge;
    private bool isJumping = false;
    private bool jump;
    private bool vault, DrawSphere;
    public bool useCurves;

    private AnimatorStateInfo state;
    bool isGrounded;
    // float m_OrigGroundCheckDistance;
    Vector3 moveDirection;
    #endregion
    #region camera
    [Header("Vcam1")]
    public GameObject VcamTarget;
    public float TopClamp = 70.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;
    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;
    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;
    public float _xSpeed = 3f;
    public float _ySpeed = 3f;

    private float _AceTargetYaw;
    private float _AceTargetPitch;

    #endregion

    void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Animator = GetComponent<Animator>();
        m_Collider = GetComponent<CapsuleCollider>();
        m_AnimatorManager = GetComponent<AnimManager>();
        m_InputManager = GetComponent<InputHandle>();

        IsCoroutinePlaying = false;
        //Set Initial Capsule Dimensions
        m_CapsuleOriginCenter = m_Collider.center;
        m_CapsuleOriginHeight = m_Collider.height;
        m_CapsuleOriginRadius = m_Collider.radius;
        // extraGravityForce.y = (Physics.gravity.y * m_GravityMultiplier) - Physics.gravity.y;
        //  extraGravityForce = new Vector3(Physics.gravity.x, -m_GravityAcceleration, Physics.gravity.z);
        Physics.gravity = new Vector3(Physics.gravity.x, -m_GravityAcceleration, Physics.gravity.z);
        //    m_Rigidbody.AddForce(extraGravityForce,ForceMode.Acceleration);
        GroundCheckDistance = m_GroundCheckSphereRadius;
        m_Abilities.Clear();
        m_Abilities.AddRange(GetComponents<ThirdPersonAbility>());
        foreach (ThirdPersonAbility ability in m_Abilities)
            ability.Initialize(this, m_AnimatorManager, m_InputManager);
    }

    void FixedUpdate()
    {
        // GroundCheck1();
        GroundCheck2();
        Physics.gravity = new Vector3(Physics.gravity.x, -m_GravityAcceleration, Physics.gravity.z);
        //  extraGravityForce = new Vector3(Physics.gravity.x, -m_GravityAcceleration, Physics.gravity.z);
        // m_Rigidbody.AddForce(extraGravityForce,ForceMode.Acceleration);
        // m_Rigidbody.AddForce(extraGravityForce);
        if (!m_IsGrounded)
            HandleAirborne();
        else
            HandleGrounded();

        // ----------------------- ABILITY FIXED UPDATE --------------------------- //
        if (m_ActiveAbility != null)
            m_ActiveAbility.FixedUpdateAbility();

        // ----------------------------------------------------------------- //
//
    }

    // Update is called once per frame
    void Update()
    {
        ApplyExtraTurnRotation();
        //  ----------------------- ABILITY  UPDATE --------------------------- //

        if (m_ActiveAbility != null)
            m_ActiveAbility.UpdateAbility();

        // ----------------------------------------------------------------- //
    }


    void CameraRotation()
    {
        if (!LockCameraPosition)
        {
            _AceTargetYaw += Input.GetAxis("Mouse X") * _xSpeed;
            _AceTargetPitch += -Input.GetAxis("Mouse Y") * _ySpeed;
        }
        _AceTargetYaw = Mathf.Clamp(_AceTargetYaw, float.MinValue, float.MaxValue);
        _AceTargetPitch = Mathf.Clamp(_AceTargetPitch, BottomClamp, TopClamp);

        VcamTarget.transform.rotation = Quaternion.Euler(_AceTargetPitch + CameraAngleOverride, _AceTargetYaw, 0.0f);
    
    }

    void GroundCheck2()
    {
        if (GroundCheckDistance > 0.05f)
        {
            if (Physics.CheckSphere(transform.position + (Vector3.up * GroundOffset), m_Collider.radius, m_GroundMask, QueryTriggerInteraction.Ignore))
            {
                m_IsGrounded = true;
                return;
            }
        }

        m_IsGrounded = false;
        if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out m_GroundHit, Mathf.Infinity))
        {
            GroundHitInfo = m_GroundHit;
            if (GroundHitInfo.distance > 1.3f)
                m_AnimatorManager.PerformBoolEvent("TransitionBreak", true);

        }
    }

    void OnDrawGizmos()
    {
        if (m_IsGrounded) Gizmos.color = Color.green;
        else Gizmos.color = Color.red;

        Gizmos.DrawSphere(transform.position + (Vector3.up * GroundOffset), 0.3f);
    }

    void HandleAirborne()
    {


        if (!m_Rigidbody.useGravity)
        {
            GroundCheckDistance = m_GroundCheckSphereRadius;
            return;
        } // Don't apply force if gravity is not being applied.
          //       else
          //      {
        GroundCheckDistance = m_Rigidbody.velocity.y < 2 ? m_GroundCheckSphereRadius : 0.01f; // change ground distance to allow Jump
        if (m_ActiveAbility is ClimbJump)
        {
            //     Debug.Log("ENTERED");
            m_Rigidbody.AddForce(1, 1.5f, 1f);
            //   HasPredictedLedge = true;
            return;
        }
        // //         if ((m_ActiveAbility is FallAbility && HasPredictedLedge) || HasPredictedLedge)
        //         {
        //    m_Rigidbody.AddForce(0f, 0.0f, 0f);
        //   HasPredictedLedge=false;
        // return;
        //   }                                                                                      //  if (m_ActiveAbility is ClimbJump) return;
        //  else if(HasPredictedLedge==false)
        // {
        //  Debug.Log("entered");
        //      m_Rigidbody.AddForce(0, extraGravityForce.y, 0);
        // m_Rigidbody.AddForce(extraGravityForce);
        //  HasPredictedLedge = false;

        // }                                                                                     //   m_Rigidbody.AddForce(extraGravityForce);
        //  }
    }

    void HandleGrounded()
    {
        UpdatePositionOnMovableObject(m_GroundHit.transform);
        //   ApplyExtraTurnRotation();
        if (!m_Rigidbody.useGravity) { return; } // Uses only with gravity applied and idle and walking states

        Vector3 vel = m_Rigidbody.velocity;
        vel.y = Mathf.Clamp(vel.y, -50, 0); // Avoid character go up

        m_Rigidbody.velocity = vel;
    }

    public Vector3 FreeMoveDirection
    {
        get
        {
            Vector3 m_FreeMoveDirection = InputManager.RelativeInput;


            // convert the world relative moveInput vector into a local-relative
            // turn amount and forward amount required to head in the desired
            // direction.
            if (m_FreeMoveDirection.magnitude > 1f)
                m_FreeMoveDirection.Normalize();

            m_FreeMoveDirection = transform.InverseTransformDirection(m_FreeMoveDirection);
            return m_FreeMoveDirection;
        }
    }

    public void CalcMovVars()
    {
        JoystickToEvents.Do(transform, camera, ref speed, ref direction);
        if (m_InputManager.sprintKey.bIsPressed && FreeOnMove(InputManager.RelativeInput))
        {
            val = 6.0f;
        }
        else if (FreeOnMove(InputManager.RelativeInput))
            val = 2.0f;
        else val = 0f;


        if (speed * val >= 2f)
        {
            m_LegOffset = (m_AnimatorManager.GetFloatParameter("LeftLeg") > 0.6f) ? -1 : 1;
            JumpOffset = m_LegOffset;
        }
        else if (speed * val == 0f && !m_Animator.IsInTransition(0) && m_AnimatorManager.IsPlayingState("Idle", 0))
        {
            LegOffset = m_LegOffset;
            m_AnimatorManager.SetFloatParameter("LegOffset", LegOffset);
        }

    }

    // Check if character can walk on desired direction
    private bool FreeOnMove(Vector3 direction)
    {
        Vector3 p1 = transform.position + Vector3.up * (m_Collider.radius * 2);
        Vector3 p2 = transform.position + Vector3.up * (m_Collider.height - m_Collider.radius);

        RaycastHit[] hits = Physics.CapsuleCastAll(p1, p2, m_Collider.radius, direction,
                                                    m_Rigidbody.velocity.sqrMagnitude * Time.fixedDeltaTime + 0.25f, GroundMask,
                                                    QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.normal.y <= Mathf.Cos(MaxAngleSlope * Mathf.Deg2Rad) && hit.collider.tag != "Player")
                return false;
        }

        return true;
    }
    /// <summary>
    /// Scale capsule collider
    /// </summary>
    /// <param name="height">How much to scale (Uses initial dimension as reference) </param>
    public void ScaleCollider(float height)
    {
        Vector3 start = GroundPoint();
        RaycastHit hit;
        if (Physics.SphereCast(start, m_Collider.radius, Vector3.up, out hit, height, GroundMask, QueryTriggerInteraction.Ignore))
            height = hit.distance + 0.05f;

        m_Collider.center = height * 0.5f * Vector3.up;
        m_Collider.radius = height < m_CapsuleOriginRadius * 2 ? height * 0.5f : m_CapsuleOriginRadius;
        m_Collider.height = height;
    }

    /// <summary>
    /// Returns the position
    /// </summary>
    /// <returns></returns>
    public Vector3 GroundPoint()
    {
        Vector3 start = transform.position + Vector3.up * CapsuleOriginalHeight;
        RaycastHit hit;
        if (Physics.SphereCast(start, 0.1f, Vector3.down, out hit, CapsuleOriginalHeight * 1.5f, m_GroundMask, QueryTriggerInteraction.Ignore))
            return hit.point;

        return Vector3.zero;
    }

    public void UpdateMovementAnimator()
    {

        m_AnimatorManager.SetHorizontalParameter("Speed", speed * val, m_SpeedDampTime);
        m_AnimatorManager.SetHorizontalParameter("Direction", direction, 0.0f);

    }
    void ApplyExtraTurnRotation()
    {
        // help the character turn faster (this is in addition to root rotation in the animation)
        if (speed * val >= 2 && m_AnimatorManager.IsPlayingState("WalkRun", 0))
        {
            float turnSpeed = Mathf.Lerp(m_StationaryTurnSpeed, m_MovingTurnSpeed, speed * m_ForwardAmount);
            transform.Rotate(0, (direction / 180) * turnSpeed * Rotation_Damp, 0);
        }
    }


    /// <summary>
    /// Rotates the character to direction of movement
    /// </summary>
    public void RotateToDirection()
    {
        RotateToDirection(m_StationaryTurnSpeed, m_MovingTurnSpeed);
    }


    /// <summary>
    /// Rotates the character to direction of movement
    /// </summary>
    public void RotateToDirection(float stationarySpeed, float movingTurnSpeed)
    {
        // help the character turn faster (this is in addition to root rotation in the animation)
        float turnSpeed = Mathf.Lerp(stationarySpeed, movingTurnSpeed, m_ForwardAmount);
        transform.Rotate(0, (direction / 180) * turnSpeed * 0.04f, 0);
    }

    public void OnAnimatorMove()
    {
        // Vars that control root motion
        bool useRootMotion = false;
        bool verticalMotion = false;
        bool rotationMotion = false;
        Vector3 multiplier = Vector3.one;

        // Check if some ability is activated
        if (m_ActiveAbility != null)
        {
            useRootMotion = m_ActiveAbility.UseRootMotion;
            verticalMotion = m_ActiveAbility.UseVerticalRootMotion;
            rotationMotion = m_ActiveAbility.UseRotationRootMotion;
            multiplier = m_ActiveAbility.RootMotionMultiplier;
        }
        if (Mathf.Approximately(Time.deltaTime, 0f) || !useRootMotion) { return; } // Conditions to avoid animation root motion
                                                                                   // Vector3 delta = m_Animator.deltaPosition;
                                                                                   // I implement this function to override the default root motion.
                                                                                   // this allows me to modify the positional speed before it's applied.
                                                                                   //     if (m_IsGrounded && Time.deltaTime > 0)
                                                                                   //    {
        Vector3 v = (m_Animator.deltaPosition * m_MoveSpeedMultiplier) / Time.deltaTime;

        if (!verticalMotion)
        {

            v.y = m_Rigidbody.velocity.y;   //I preserve the existing y part of the current velocity.
        }
        m_Rigidbody.velocity = v;
        //   transform.rotation = m_Animator.rootRotation;
        Vector3 deltaRot = m_Animator.deltaRotation.eulerAngles;
        transform.rotation *= Quaternion.Euler(deltaRot);
        //    }
        //   else
        //   m_Rigidbody.velocity = (m_Animator.deltaPosition * 1) / Time.deltaTime;

    }

    /// <summary>
    /// Method called by any ability to try enter ability
    /// </summary>
    /// <param name="ability"></param>
    public void OnTryEnterAbility(ThirdPersonAbility ability)
    {
        if (m_ActiveAbility == null)
            EnterAbility(ability);
        else
        {
            // Check if new ability has priority above current ability
            foreach (ThirdPersonAbility stopAbility in ability.IgnoreAbilities)
            {
                if (stopAbility == m_ActiveAbility)
                    EnterAbility(ability);
            }
        }

    }
    /// <summary>
    /// Method called by any ability to try enter ability
    /// </summary>
    /// <param name="ability"></param>
    public void OnTryUpdateEnterAbility(ThirdPersonAbility ability)
    {
        if (m_ActiveAbility == null)
            EnterAbility(ability);
        else
        {
            // Check if new ability has priority above current ability
            foreach (ThirdPersonAbility stopAbility in ability.IgnoreAbilities)
            {
                if (stopAbility == m_ActiveAbility)
                    EnterAbility(ability);
            }
        }

    }

    /// <summary>
    /// Method that enter an ability. Can be also called to force any ability to enter
    /// </summary>
    /// <param name="ability"></param>
    public void EnterAbility(ThirdPersonAbility ability, bool forceAbility = false)
    {
        ExitActiveAbility();

        m_ActiveAbility = ability;
        Debug.Log(m_ActiveAbility);

        m_ActiveAbility.OnEnterAbility();
        m_Animator.applyRootMotion = m_ActiveAbility.UseRootMotion;

        UpdatePositionOnMovableObject(null);

    }

    public void ExitAbility(ThirdPersonAbility ability)
    {
        if (m_ActiveAbility == ability)
        {
            LastAbility = m_ActiveAbility;
            m_ActiveAbility = null;

            if (ability.Active)
                ability.OnExitAbility();

            // m_Capsule.sharedMaterial = capsuleOriginalMaterial;
            //OnAnyAbilityExits.Invoke();
        }
    }

    /// <summary>
    /// Force current active ability to exit
    /// </summary>
    /// <param name="ability"></param>
    public void ExitActiveAbility()
    {
        if (m_ActiveAbility != null)
        {
            LastAbility = m_ActiveAbility;
            if (m_ActiveAbility.Active)
                m_ActiveAbility.OnExitAbility();

            m_ActiveAbility = null;

            //       m_Capsule.sharedMaterial = capsuleOriginalMaterial;
            //OnAnyAbilityExits.Invoke();
        }
    }

    private Vector3 m_LastGroundPos = Vector3.zero;
    private float m_LastAngle = 0;
    private Transform m_CurrentTarget = null;

    public Vector3 DeltaPos { get; private set; }
    public float DeltaYAngle { get; private set; }
    public void UpdatePositionOnMovableObject(Transform target)
    {
        if (target == null)
        {
            m_CurrentTarget = null;
            return;
        }

        if (m_CurrentTarget != target)
        {
            m_CurrentTarget = target;

            DeltaPos = Vector3.zero;
            DeltaYAngle = 0;
        }
        else
        {
            DeltaPos = target.transform.position - m_LastGroundPos;
            DeltaYAngle = target.transform.rotation.eulerAngles.y - m_LastAngle;

            Vector3 direction = transform.position - target.transform.position;
            direction.y = 0;

            float FinalAngle = Vector3.SignedAngle(Vector3.forward, direction.normalized, Vector3.up) + DeltaYAngle;

            float xMult = Vector3.Dot(Vector3.forward, direction.normalized) > 0 ? 1 : -1;
            float zMult = Vector3.Dot(Vector3.right, direction.normalized) > 0 ? -1 : 1;

            float cosine = Mathf.Abs(Mathf.Cos(FinalAngle * Mathf.Deg2Rad));
            Vector3 deltaRotPos = new Vector3(cosine * xMult, 0,
                 Mathf.Abs(Mathf.Sin(FinalAngle * Mathf.Deg2Rad)) * zMult) * Mathf.Abs(direction.magnitude);

            DeltaPos += deltaRotPos * (DeltaYAngle * Mathf.Deg2Rad);
        }

        if (DeltaPos.magnitude > 3f)
            DeltaPos = Vector3.zero;

        transform.position += DeltaPos;
        transform.Rotate(0, DeltaYAngle, 0);

        m_LastGroundPos = target.transform.position;
        m_LastAngle = target.transform.rotation.eulerAngles.y;
    }


}
