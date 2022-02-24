using Ace.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

#if CINEMACHINE_HDRP || CINEMACHINE_LWRP_7_0_0
    #if CINEMACHINE_HDRP_7_0_0
    using UnityEngine.Rendering.HighDefinition;
    #else
        #if CINEMACHINE_LWRP_7_0_0
        using UnityEngine.Rendering.Universal;
        #else
        using UnityEngine.Experimental.Rendering.HDPipeline;
        #endif
    #endif
#endif

namespace Ace
{
    /// <summary>
    /// ACEBrain is the link between the Unity Camera and the Cinemachine Virtual
    /// Cameras in the scene.  It monitors the priority stack to choose the current
    /// Virtual Camera, and blend with another if necessary.  Finally and most importantly,
    /// it applies the Virtual Camera state to the attached Unity Camera.
    ///
    /// The ACEBrain is also the place where rules for blending between virtual cameras
    /// are defined.  Camera blending is an interpolation over time of one virtual camera
    /// position and state to another. If you think of virtual cameras as cameramen, then
    /// blending is a little like one cameraman smoothly passing the camera to another cameraman.
    /// You can specify the time over which to blend, as well as the blend curve shape.
    /// Note that a camera cut is just a zero-time blend.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
//    [RequireComponent(typeof(Camera))] // strange but true: we can live without it
    [DisallowMultipleComponent]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    [AddComponentMenu("Ace/Ace Brain")]
    [SaveDuringPlay]
    [HelpURL(Documentation.BaseURL + "manual/ACEBrainProperties.html")]
    public class ACEBrain : MonoBehaviour
    {
        /// <summary>
        /// When enabled, the current camera and blend will be indicated in the 
        /// game window, for debugging.
        /// </summary>
        [Tooltip("When enabled, the current camera and blend will be indicated in "
            + "the game window, for debugging")]
        public bool m_ShowDebugText = false;

        /// <summary>
        /// When enabled, shows the camera's frustum in the scene view.
        /// </summary>
        [Tooltip("When enabled, the camera's frustum will be shown at all times "
            + "in the scene view")]
        public bool m_ShowCameraFrustum = true;

        /// <summary>
        /// When enabled, the cameras will always respond in real-time to user input and damping,
        /// even if the game is running in slow motion
        /// </summary>
        [Tooltip("When enabled, the cameras will always respond in real-time to user input "
            + "and damping, even if the game is running in slow motion")]
        public bool m_IgnoreTimeScale = false;

        /// <summary>
        /// If set, this object's Y axis will define the worldspace Up vector for all the
        /// virtual cameras.  This is useful in top-down game environments.  If not set, Up is worldspace Y.
        /// </summary>
        [Tooltip("If set, this object's Y axis will define the worldspace Up vector for all the "
            + "virtual cameras.  This is useful for instance in top-down game environments.  "
            + "If not set, Up is worldspace Y.  Setting this appropriately is important, "
            + "because Virtual Cameras don't like looking straight up or straight down.")]
        public Transform m_WorldUpOverride;

        /// <summary>This enum defines the options available for the update method.</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        public enum UpdateMethod
        {
            /// <summary>Virtual cameras are updated in sync with the Physics module, in FixedUpdate</summary>
            FixedUpdate,
            /// <summary>Virtual cameras are updated in MonoBehaviour LateUpdate.</summary>
            LateUpdate,
            /// <summary>Virtual cameras are updated according to how the target is updated.</summary>
            SmartUpdate,
            /// <summary>Virtual cameras are not automatically updated, client must explicitly call 
            /// the ACEBrain's ManualUpdate() method.</summary>
            ManualUpdate
        };

        /// <summary>Depending on how the target objects are animated, adjust the update method to
        /// minimize the potential jitter.  Use FixedUpdate if all your targets are animated with for RigidBody animation.
        /// SmartUpdate will choose the best method for each virtual camera, depending
        /// on how the target is animated.</summary>
        [Tooltip("The update time for the vcams.  Use FixedUpdate if all your targets are animated "
            + "during FixedUpdate (e.g. RigidBodies), LateUpdate if all your targets are animated "
            + "during the normal Update loop, and SmartUpdate if you want Cinemachine to do the "
            + "appropriate thing on a per-target basis.  SmartUpdate is the recommended setting")]
        public UpdateMethod m_UpdateMethod = UpdateMethod.SmartUpdate;

        /// <summary>This enum defines the options available for the update method.</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        public enum BrainUpdateMethod
        {
            /// <summary>Camera is updated in sync with the Physics module, in FixedUpdate</summary>
            FixedUpdate,
            /// <summary>Camera is updated in MonoBehaviour LateUpdate (or when ManualUpdate is called).</summary>
            LateUpdate
        };

        /// <summary>The update time for the Brain, i.e. when the blends are evaluated and the
        /// brain's transform is updated.</summary>
        [Tooltip("The update time for the Brain, i.e. when the blends are evaluated and "
            + "the brain's transform is updated")]
        public BrainUpdateMethod m_BlendUpdateMethod = BrainUpdateMethod.LateUpdate;

        /// <summary>
        /// The blend which is used if you don't explicitly define a blend between two Virtual Cameras.
        /// </summary>
        [CinemachineBlendDefinitionProperty]
        [Tooltip("The blend that is used in cases where you haven't explicitly defined a "
            + "blend between two Virtual Cameras")]
        public CinemachineBlendDefinition m_DefaultBlend
            = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 2f);

        /// <summary>
        /// This is the asset which contains custom settings for specific blends.
        /// </summary>
        [Tooltip("This is the asset that contains custom settings for blends between "
            + "specific virtual cameras in your scene")]
        public ACEBlenderConfig m_CustomBlends = null;

        /// <summary>
        /// Get the Unity Camera that is attached to this GameObject.  This is the camera
        /// that will be controlled by the brain.
        /// </summary>
        public Camera OutputCamera
        {
            get
            {
                if (m_OutputCamera == null && !Application.isPlaying)
#if UNITY_2019_2_OR_NEWER
                    TryGetComponent(out m_OutputCamera);
#else
                    m_OutputCamera = GetComponent<Camera>();
#endif
                return m_OutputCamera;
            }
        }
        private Camera m_OutputCamera = null; // never use directly - use accessor

        /// <summary>Event with a ACEBrain parameter</summary>
        [Serializable] public class BrainEvent : UnityEvent<ACEBrain> {}

        /// <summary>
        /// Event that is fired when a virtual camera is activated.
        /// The parameters are (incoming_vcam, outgoing_vcam), in that order.
        /// </summary>
        [Serializable] public class VcamActivatedEvent : UnityEvent<ICamera, ICamera> {}

        /// <summary>This event will fire whenever a virtual camera goes live and there is no blend</summary>
        [Tooltip("This event will fire whenever a virtual camera goes live and there is no blend")]
        public BrainEvent m_CameraCutEvent = new BrainEvent();

        /// <summary>This event will fire whenever a virtual camera goes live.  If a blend is involved,
        /// then the event will fire on the first frame of the blend.
        /// 
        /// The Parameters are (incoming_vcam, outgoing_vcam), in that order.</summary>
        [Tooltip("This event will fire whenever a virtual camera goes live.  If a blend is "
            + "involved, then the event will fire on the first frame of the blend.")]
        public VcamActivatedEvent m_CameraActivatedEvent = new VcamActivatedEvent();

        /// <summary>
        /// API for the Unity Editor.
        /// Show this camera no matter what.  This is static, and so affects all Cinemachine brains.
        /// </summary>
        public static ICamera SoloCamera
        {
            get { return mSoloCamera; }
            set
            {
                if (value != null && !ACECore.Instance.IsLive(value))
                    value.OnTransitionFromCamera(null, Vector3.up, ACECore.DeltaTime);
                mSoloCamera = value;
            }
        }

        /// <summary>API for the Unity Editor.</summary>
        /// <returns>Color used to indicate that a camera is in Solo mode.</returns>
        public static Color GetSoloGUIColor() { return Color.Lerp(Color.red, Color.yellow, 0.8f); }

        /// <summary>Get the default world up for the virtual cameras.</summary>
        public Vector3 DefaultWorldUp
            { get { return (m_WorldUpOverride != null) ? m_WorldUpOverride.transform.up : Vector3.up; } }

        private static ICamera mSoloCamera;
        private Coroutine mPhysicsCoroutine;

        private int m_LastFrameUpdated;

        private void OnEnable()
        {
            // Make sure there is a first stack frame
            if (mFrameStack.Count == 0)
                mFrameStack.Add(new BrainFrame());

            m_OutputCamera = GetComponent<Camera>();
            ACECore.Instance.AddActiveBrain(this);
            ACEDebug.OnGUIHandlers -= OnGuiHandler;
            ACEDebug.OnGUIHandlers += OnGuiHandler;

            // We check in after the physics system has had a chance to move things
            mPhysicsCoroutine = StartCoroutine(AfterPhysics());

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            ACEDebug.OnGUIHandlers -= OnGuiHandler;
            ACECore.Instance.RemoveActiveBrain(this);
            mFrameStack.Clear();
            StopCoroutine(mPhysicsCoroutine);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) 
        { 
            if (Time.frameCount == m_LastFrameUpdated && mFrameStack.Count > 0)
                ManualUpdate();
        }
        void OnSceneUnloaded(Scene scene)
        {
            if (Time.frameCount == m_LastFrameUpdated && mFrameStack.Count > 0)
                ManualUpdate();
        }
        
        private void Start()
        {
            m_LastFrameUpdated = -1;
            UpdateVirtualCameras(ACECore.UpdateFilter.Late, -1f);
        }

        private void OnGuiHandler()
        {
            if (!m_ShowDebugText)
                ACEDebug.ReleaseScreenPos(this);
            else
            {
                // Show the active camera and blend
                var sb = ACEDebug.SBFromPool();
                Color color = GUI.color;
                sb.Length = 0;
                sb.Append("CM ");
                sb.Append(gameObject.name);
                sb.Append(": ");
                if (SoloCamera != null)
                {
                    sb.Append("SOLO ");
                    GUI.color = GetSoloGUIColor();
                }

                if (IsBlending)
                    sb.Append(ActiveBlend.Description);
                else
                {
                    ICamera vcam = ActiveVirtualCamera;
                    if (vcam == null)
                        sb.Append("(none)");
                    else
                    {
                        sb.Append("[");
                        sb.Append(vcam.Name);
                        sb.Append("]");
                    }
                }
                string text = sb.ToString();
                Rect r = ACEDebug.GetScreenPos(this, text, GUI.skin.box);
                GUI.Label(r, text, GUI.skin.box);
                GUI.color = color;
                ACEDebug.ReturnToPool(sb);
            }
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (ACEDebug.OnGUIHandlers != null)
                ACEDebug.OnGUIHandlers();
        }
#endif

        WaitForFixedUpdate mWaitForFixedUpdate = new WaitForFixedUpdate();
        private IEnumerator AfterPhysics()
        {
            while (true)
            {
                // FixedUpdate can be called multiple times per frame
                yield return mWaitForFixedUpdate;
                if (m_UpdateMethod == UpdateMethod.FixedUpdate
                    || m_UpdateMethod == UpdateMethod.SmartUpdate)
                {
                    ACECore.UpdateFilter filter = ACECore.UpdateFilter.Fixed;
                    if (m_UpdateMethod == UpdateMethod.SmartUpdate)
                    {
                        // Track the targets
                        UpdateTracker.OnUpdate(UpdateTracker.UpdateClock.Fixed);
                        filter = ACECore.UpdateFilter.SmartFixed;
                    }
                    UpdateVirtualCameras(filter, GetEffectiveDeltaTime(true));
                }
                // Choose the active vcam and apply it to the Unity camera
                if (m_BlendUpdateMethod == BrainUpdateMethod.FixedUpdate)
                {
                    UpdateFrame0(Time.fixedDeltaTime);
                    ProcessActiveCamera(Time.fixedDeltaTime);
                }
            }
        }

        private void LateUpdate()
        {
            if (m_UpdateMethod != UpdateMethod.ManualUpdate)
                ManualUpdate();
        }

        /// <summary>
        /// Call this method explicitly from an external script to update the virtual cameras
        /// and position the main camera, if the UpdateMode is set to ManualUpdate.
        /// For other update modes, this method is called automatically, and should not be
        /// called from elsewhere.
        /// </summary>
        public void ManualUpdate()
        {
            m_LastFrameUpdated = Time.frameCount;

            float deltaTime = GetEffectiveDeltaTime(false);
            if (!Application.isPlaying || m_BlendUpdateMethod != BrainUpdateMethod.FixedUpdate)
                UpdateFrame0(deltaTime);

            ComputeCurrentBlend(ref mCurrentLiveCameras, 0);

            if (m_UpdateMethod == UpdateMethod.FixedUpdate)
            {
                // Special handling for fixed update: cameras that have been enabled
                // since the last physics frame must be updated now
                if (m_BlendUpdateMethod != BrainUpdateMethod.FixedUpdate)
                {
                    ACECore.Instance.CurrentUpdateFilter = ACECore.UpdateFilter.Fixed;
                    if (SoloCamera == null)
                        mCurrentLiveCameras.UpdateCameraState(
                            DefaultWorldUp, GetEffectiveDeltaTime(true));
                }
            }
            else
            {
                ACECore.UpdateFilter filter = ACECore.UpdateFilter.Late;
                if (m_UpdateMethod == UpdateMethod.SmartUpdate)
                {
                    // Track the targets
                    UpdateTracker.OnUpdate(UpdateTracker.UpdateClock.Late);
                    filter = ACECore.UpdateFilter.SmartLate;
                }
                UpdateVirtualCameras(filter, deltaTime);
            }

            // Choose the active vcam and apply it to the Unity camera
            if (!Application.isPlaying || m_BlendUpdateMethod != BrainUpdateMethod.FixedUpdate)
                ProcessActiveCamera(deltaTime);
        }

#if UNITY_EDITOR
        /// This is only needed in editor mode to force timeline to call OnGUI while
        /// timeline is up and the game is not running, in order to allow dragging
        /// the composer guide in the game view.
        private void OnPreCull()
        {
            if (!Application.isPlaying)
            {
                // Note: this call will cause any screen canvas attached to the camera
                // to be painted one frame out of sync.  It will only happen in the editor when not playing.
                ProcessActiveCamera(GetEffectiveDeltaTime(false));
            }
        }
#endif

        private float GetEffectiveDeltaTime(bool fixedDelta)
        {
            if (ACECore.UniformDeltaTimeOverride >= 0)
                return ACECore.UniformDeltaTimeOverride;

            if (SoloCamera != null)
                return Time.unscaledDeltaTime;

            if (!Application.isPlaying)
            {
                for (int i = mFrameStack.Count - 1; i > 0; --i)
                {
                    var frame = mFrameStack[i];
                    if (frame.Active)
                        return frame.deltaTimeOverride;
                }
                return -1;
            }
            if (m_IgnoreTimeScale)
                return fixedDelta ? Time.fixedDeltaTime : Time.unscaledDeltaTime;
            return fixedDelta ? Time.fixedDeltaTime : Time.deltaTime;
        }

        private void UpdateVirtualCameras(ACECore.UpdateFilter updateFilter, float deltaTime)
        {
            // We always update all active virtual cameras
            ACECore.Instance.CurrentUpdateFilter = updateFilter;
            Camera camera = OutputCamera;
            ACECore.Instance.UpdateAllActiveVirtualCameras(
                camera == null ? -1 : camera.cullingMask, DefaultWorldUp, deltaTime);

            // Make sure all live cameras get updated, in case some of them are deactivated
            if (SoloCamera != null)
                SoloCamera.UpdateCameraState(DefaultWorldUp, deltaTime);
            mCurrentLiveCameras.UpdateCameraState(DefaultWorldUp, deltaTime);

            // Restore the filter for general use
            updateFilter = ACECore.UpdateFilter.Late;
            if (Application.isPlaying)
            {
                if (m_UpdateMethod == UpdateMethod.SmartUpdate)
                    updateFilter |= ACECore.UpdateFilter.Smart;
                else if (m_UpdateMethod == UpdateMethod.FixedUpdate)
                    updateFilter = ACECore.UpdateFilter.Fixed;
            }
            ACECore.Instance.CurrentUpdateFilter = updateFilter;
        }

        /// <summary>
        /// Get the current active virtual camera.
        /// </summary>
        public ICamera ActiveVirtualCamera
        {
            get
            {
                if (SoloCamera != null)
                    return SoloCamera;
                return DeepCamBFromBlend(mCurrentLiveCameras);
            }
        }

        static ICamera DeepCamBFromBlend(ACEBlend blend)
        {
            ICamera vcam = blend.CamB;
            while (vcam != null)
            {
                if (!vcam.IsValid)
                    return null;    // deleted!
                BlendSourceVirtualCamera bs = vcam as BlendSourceVirtualCamera;
                if (bs == null)
                    break;
                vcam = bs.Blend.CamB;
            }
            return vcam;
        }

        /// <summary>
        /// Is there a blend in progress?
        /// </summary>
        public bool IsBlending { get { return ActiveBlend != null; } }

        /// <summary>
        /// Get the current blend in progress.  Returns null if none.
        /// </summary>
        public ACEBlend ActiveBlend
        {
            get
            {
                if (SoloCamera != null)
                    return null;
                if (mCurrentLiveCameras.CamA == null || mCurrentLiveCameras.Equals(null) || mCurrentLiveCameras.IsComplete)
                    return null;
                return mCurrentLiveCameras;
            }
        }

        private class BrainFrame
        {
            public int id;
            public ACEBlend blend = new ACEBlend(null, null, null, 0, 0);
            public bool Active { get { return blend.IsValid; } }

            // Working data - updated every frame
            public ACEBlend workingBlend = new ACEBlend(null, null, null, 0, 0);
            public BlendSourceVirtualCamera workingBlendSource = new BlendSourceVirtualCamera(null);

            // Used by Timeline Preview for overriding the current value of deltaTime
            public float deltaTimeOverride;
        }

        // Current game state is always frame 0, overrides are subsequent frames
        private List<BrainFrame> mFrameStack = new List<BrainFrame>();
        private int mNextFrameId = 1;

        /// Get the frame index corresponding to the ID
        private int GetBrainFrame(int withId)
        {
            int count = mFrameStack.Count;
            for (int i = mFrameStack.Count - 1; i > 0; --i)
                if (mFrameStack[i].id == withId)
                    return i;
            // Not found - add it
            mFrameStack.Add(new BrainFrame() { id = withId });
            return mFrameStack.Count - 1;
        }

        // Current Brain State - result of all frames.  Blend camB is "current" camera always
        ACEBlend mCurrentLiveCameras = new ACEBlend(null, null, null, 0, 0);
        
        // To avoid GC memory alloc every frame
        private static readonly AnimationCurve mDefaultLinearAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);
        
        /// <summary>
        /// This API is specifically for Timeline.  Do not use it.
        /// Override the current camera and current blend.  This setting will trump
        /// any in-game logic that sets virtual camera priorities and Enabled states.
        /// This is the main API for the timeline.
        /// </summary>
        /// <param name="overrideId">Id to represent a specific client.  An internal
        /// stack is maintained, with the most recent non-empty override taking precenence.
        /// This id must be > 0.  If you pass -1, a new id will be created, and returned.
        /// Use that id for subsequent calls.  Don't forget to
        /// call ReleaseCameraOverride after all overriding is finished, to
        /// free the OverideStack resources.</param>
        /// <param name="camA"> The camera to set, corresponding to weight=0</param>
        /// <param name="camB"> The camera to set, corresponding to weight=1</param>
        /// <param name="weightB">The blend weight.  0=camA, 1=camB</param>
        /// <param name="deltaTime">override for deltaTime.  Should be Time.FixedDelta for
        /// time-based calculations to be included, -1 otherwise</param>
        /// <returns>The oiverride ID.  Don't forget to call ReleaseCameraOverride
        /// after all overriding is finished, to free the OverideStack resources.</returns>
        public int SetCameraOverride(
            int overrideId,
            ICamera camA, ICamera camB,
            float weightB, float deltaTime)
        {
            if (overrideId < 0)
                overrideId = mNextFrameId++;

            BrainFrame frame = mFrameStack[GetBrainFrame(overrideId)];
            frame.deltaTimeOverride = deltaTime;
            frame.blend.CamA = camA;
            frame.blend.CamB = camB;
            frame.blend.BlendCurve = mDefaultLinearAnimationCurve;
            frame.blend.Duration = 1;
            frame.blend.TimeInBlend = weightB;

            // In case vcams are inactive game objects, make sure they get initialized properly
            var cam = camA as VirtualCamBase;
            if (cam != null)
                cam.EnsureStarted();
            cam = camB as VirtualCamBase;
            if (cam != null)
                cam.EnsureStarted();

            return overrideId;
        }

        /// <summary>
        /// This API is specifically for Timeline.  Do not use it.
        /// Release the resources used for a camera override client.
        /// See SetCameraOverride.
        /// </summary>
        /// <param name="overrideId">The ID to released.  This is the value that
        /// was returned by SetCameraOverride</param>
        public void ReleaseCameraOverride(int overrideId)
        {
            for (int i = mFrameStack.Count - 1; i > 0; --i)
            {
                if (mFrameStack[i].id == overrideId)
                {
                    mFrameStack.RemoveAt(i);
                    return;
                }
            }
        }

        ICamera mActiveCameraPreviousFrame;
        private void ProcessActiveCamera(float deltaTime)
        {
            var activeCamera = ActiveVirtualCamera;
            if (activeCamera != null)
            {
                // Has the current camera changed this frame?
                if (activeCamera != mActiveCameraPreviousFrame)
                {
                    // Notify incoming camera of transition
                    activeCamera.OnTransitionFromCamera(
                        mActiveCameraPreviousFrame, DefaultWorldUp, deltaTime);
                    if (m_CameraActivatedEvent != null)
                        m_CameraActivatedEvent.Invoke(activeCamera, mActiveCameraPreviousFrame);

                    // If we're cutting without a blend, send an event
                    if (!IsBlending || (mActiveCameraPreviousFrame != null
                                && !ActiveBlend.Uses(mActiveCameraPreviousFrame)))
                    {
                        if (m_CameraCutEvent != null)
                            m_CameraCutEvent.Invoke(this);
                        if (ACECore.CameraCutEvent != null)
                            ACECore.CameraCutEvent.Invoke(this);
                    }
                    // Re-update in case it's inactive
                    activeCamera.UpdateCameraState(DefaultWorldUp, deltaTime);
                }
                // Apply the vcam state to the Unity camera
                PushStateToUnityCamera(
                    SoloCamera != null ? SoloCamera.State : mCurrentLiveCameras.State);
            }
            mActiveCameraPreviousFrame = activeCamera;
        }

        private void UpdateFrame0(float deltaTime)
        {
            // Update the in-game frame (frame 0)
            BrainFrame frame = mFrameStack[0];

            // Are we transitioning cameras?
            var activeCamera = TopCameraFromPriorityQueue();
            var outGoingCamera = frame.blend.CamB;
            if (activeCamera != outGoingCamera)
            {
                // Do we need to create a game-play blend?
                if ((UnityEngine.Object)activeCamera != null
                    && (UnityEngine.Object)outGoingCamera != null && deltaTime >= 0)
                {
                    // Create a blend (curve will be null if a cut)
                    var blendDef = LookupBlend(outGoingCamera, activeCamera);
                    if (blendDef.BlendCurve != null && blendDef.BlendTime > 0)
                    {
                        if (frame.blend.IsComplete)
                            frame.blend.CamA = outGoingCamera;  // new blend
                        else
                        {
                            // Special case: if backing out of a blend-in-progress
                            // with the same blend in reverse, adjust the blend time
                            if ((frame.blend.CamA == activeCamera 
                                    || (frame.blend.CamA as BlendSourceVirtualCamera)?.Blend.CamB == activeCamera) 
                                && frame.blend.CamB == outGoingCamera 
                                && frame.blend.Duration <= blendDef.BlendTime)
                            {
                                blendDef.m_Time = 
                                    (frame.blend.TimeInBlend / frame.blend.Duration) * blendDef.BlendTime;
                            }

                            // Chain to existing blend
                            frame.blend.CamA = new BlendSourceVirtualCamera(
                                new ACEBlend(
                                    frame.blend.CamA, frame.blend.CamB,
                                    frame.blend.BlendCurve, frame.blend.Duration,
                                    frame.blend.TimeInBlend));
                        }
                    }
                    frame.blend.BlendCurve = blendDef.BlendCurve;
                    frame.blend.Duration = blendDef.BlendTime;
                    frame.blend.TimeInBlend = 0;
                }
                // Set the current active camera
                frame.blend.CamB = activeCamera;
            }

            // Advance the current blend (if any)
            if (frame.blend.CamA != null)
            {
                frame.blend.TimeInBlend += (deltaTime >= 0) ? deltaTime : frame.blend.Duration;
                if (frame.blend.IsComplete)
                {
                    // No more blend
                    frame.blend.CamA = null;
                    frame.blend.BlendCurve = null;
                    frame.blend.Duration = 0;
                    frame.blend.TimeInBlend = 0;
                }
            }
        }

        /// <summary>
        /// Used internally to compute the currrent blend, taking into account
        /// the in-game camera and all the active overrides.  Caller may optionally
        /// exclude n topmost overrides.
        /// </summary>
        /// <param name="outputBlend">Receives the nested blend</param>
        /// <param name="numTopLayersToExclude">Optionaly exclude the last number 
        /// of overrides from the blend</param>
        public void ComputeCurrentBlend(
            ref ACEBlend outputBlend, int numTopLayersToExclude)
        {
            // Resolve the current working frame states in the stack
            int lastActive = 0;
            int topLayer = Mathf.Max(1, mFrameStack.Count - numTopLayersToExclude);
            for (int i = 0; i < topLayer; ++i)
            {
                BrainFrame frame = mFrameStack[i];
                if (i == 0 || frame.Active)
                {
                    frame.workingBlend.CamA = frame.blend.CamA;
                    frame.workingBlend.CamB = frame.blend.CamB;
                    frame.workingBlend.BlendCurve = frame.blend.BlendCurve;
                    frame.workingBlend.Duration = frame.blend.Duration;
                    frame.workingBlend.TimeInBlend = frame.blend.TimeInBlend;
                    if (i > 0 && !frame.blend.IsComplete)
                    {
                        if (frame.workingBlend.CamA == null)
                        {
                            if (mFrameStack[lastActive].blend.IsComplete)
                                frame.workingBlend.CamA = mFrameStack[lastActive].blend.CamB;
                            else
                            {
                                frame.workingBlendSource.Blend = mFrameStack[lastActive].workingBlend;
                                frame.workingBlend.CamA = frame.workingBlendSource;
                            }
                        }
                        else if (frame.workingBlend.CamB == null)
                        {
                            if (mFrameStack[lastActive].blend.IsComplete)
                                frame.workingBlend.CamB = mFrameStack[lastActive].blend.CamB;
                            else
                            {
                                frame.workingBlendSource.Blend = mFrameStack[lastActive].workingBlend;
                                frame.workingBlend.CamB = frame.workingBlendSource;
                            }
                        }
                    }
                    lastActive = i;
                }
            }
            var workingBlend = mFrameStack[lastActive].workingBlend;
            outputBlend.CamA = workingBlend.CamA;
            outputBlend.CamB = workingBlend.CamB;
            outputBlend.BlendCurve = workingBlend.BlendCurve;
            outputBlend.Duration = workingBlend.Duration;
            outputBlend.TimeInBlend = workingBlend.TimeInBlend;
        }

        /// <summary>
        /// True if the ICamera the current active camera,
        /// or part of a current blend, either directly or indirectly because its parents are live.
        /// </summary>
        /// <param name="vcam">The camera to test whether it is live</param>
        /// <param name="dominantChildOnly">If truw, will only return true if this vcam is the dominat live child</param>
        /// <returns>True if the camera is live (directly or indirectly)
        /// or part of a blend in progress.</returns>
        public bool IsLive(ICamera vcam, bool dominantChildOnly = false)
        {
            if (SoloCamera == vcam)
                return true;
            if (mCurrentLiveCameras.Uses(vcam))
                return true;

            ICamera parent = vcam.ParentCamera;
            while (parent != null && parent.IsLiveChild(vcam, dominantChildOnly))
            {
                if (SoloCamera == parent || mCurrentLiveCameras.Uses(parent))
                    return true;
                vcam = parent;
                parent = vcam.ParentCamera;
            }
            return false;
        }

        /// <summary>
        /// The current state applied to the unity camera (may be the result of a blend)
        /// </summary>
        public CameraState CurrentCameraState { get; private set; }

        /// <summary>
        /// Get the highest-priority Enabled ICamera
        /// that is visible to my camera.  Culling Mask is used to test visibility.
        /// </summary>
        private ICamera TopCameraFromPriorityQueue()
        {
            ACECore core = ACECore.Instance;
            Camera outputCamera = OutputCamera;
            int mask = outputCamera == null ? ~0 : outputCamera.cullingMask;
            int numCameras = core.VirtualCameraCount;
            for (int i = 0; i < numCameras; ++i)
            {
                var cam = core.GetVirtualCamera(i);
                GameObject go = cam != null ? cam.gameObject : null;
                if (go != null && (mask & (1 << go.layer)) != 0)
                    return cam;
            }
            return null;
        }

        /// <summary>
        /// Create a blend curve for blending from one ICamera to another.
        /// If there is a specific blend defined for these cameras it will be used, otherwise
        /// a default blend will be created, which could be a cut.
        /// </summary>
        private CinemachineBlendDefinition LookupBlend(
            ICamera fromKey, ICamera toKey)
        {
            // Get the blend curve that's most appropriate for these cameras
            CinemachineBlendDefinition blend = m_DefaultBlend;
            if (m_CustomBlends != null)
            {
                string fromCameraName = (fromKey != null) ? fromKey.Name : string.Empty;
                string toCameraName = (toKey != null) ? toKey.Name : string.Empty;
                blend = m_CustomBlends.GetBlendForVirtualCameras(
                        fromCameraName, toCameraName, blend);
            }
            if (ACECore.GetBlendOverride != null)
                blend = ACECore.GetBlendOverride(fromKey, toKey, blend, this);
            return blend;
        }

        /// <summary> Apply a cref="CameraState"/> to the game object</summary>
        private void PushStateToUnityCamera(CameraState state)
        {
            CurrentCameraState = state;
            if ((state.BlendHint & CameraState.BlendHintValue.NoPosition) == 0)
                transform.position = state.FinalPosition;
            if ((state.BlendHint & CameraState.BlendHintValue.NoOrientation) == 0)
                transform.rotation = state.FinalOrientation;
            if ((state.BlendHint & CameraState.BlendHintValue.NoLens) == 0)
            {
                Camera cam = OutputCamera;
                if (cam != null)
                {
                    cam.nearClipPlane = state.Lens.NearClipPlane;
                    cam.farClipPlane = state.Lens.FarClipPlane;
                    cam.fieldOfView = state.Lens.FieldOfView;
                    if (cam.orthographic)
                        cam.orthographicSize = state.Lens.OrthographicSize;
#if UNITY_2018_2_OR_NEWER
                    else
                    {
                        cam.usePhysicalProperties = state.Lens.IsPhysicalCamera;
                        cam.lensShift = state.Lens.LensShift;
                    }
    #if CINEMACHINE_HDRP
                    if (state.Lens.IsPhysicalCamera)
                    {
#if UNITY_2019_2_OR_NEWER
                        cam.TryGetComponent<HDAdditionalCameraData>(out var hda);
#else
                        var hda = cam.GetComponent<HDAdditionalCameraData>();
#endif
                        if (hda != null)
                        {
                            hda.physicalParameters.iso = state.Lens.Iso;
                            hda.physicalParameters.shutterSpeed = state.Lens.ShutterSpeed;
                            hda.physicalParameters.aperture = state.Lens.Aperture;
                            hda.physicalParameters.bladeCount = state.Lens.BladeCount;
                            hda.physicalParameters.curvature = state.Lens.Curvature;
                            hda.physicalParameters.barrelClipping = state.Lens.BarrelClipping;
                            hda.physicalParameters.anamorphism = state.Lens.Anamorphism;
                        }
                    }
    #endif
#endif
                }
            }
            if (ACECore.CameraUpdatedEvent != null)
                ACECore.CameraUpdatedEvent.Invoke(this);
        }
    }
}
