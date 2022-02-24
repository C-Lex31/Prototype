using UnityEngine;
using System.Collections.Generic;

namespace Ace
{
    internal static class Documentation 
    {
        /// <summary>This must be used like
        /// [HelpURL(Documentation.BaseURL + "api/some-page.html")]
        /// or
        /// [HelpURL(Documentation.BaseURL + "manual/some-page.html")]
        /// It cannot support String.Format nor string interpolation</summary>
        public const string BaseURL = "https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/";
    }

    /// <summary>A singleton that manages complete lists of ACEBrain and,
    /// Cinemachine Virtual Cameras, and the priority queue.  Provides
    /// services to keeping track of whether Cinemachine Virtual Cameras have
    /// been updated each frame.</summary>
    public sealed class ACECore
    {
        /// <summary>Data version string.  Used to upgrade from legacy projects</summary>
        public static readonly int kStreamingVersion = 20170927;

        /// <summary>Human-readable Cinemachine Version</summary>
        public static readonly string kVersionString = "2.6.11";

        /// <summary>
        /// Stages in the Cinemachine Component pipeline, used for
        /// UI organization>.  This enum defines the pipeline order.
        /// </summary>
        public enum Stage
        {
            /// <summary>Second stage: position the camera in space</summary>
            Body,

            /// <summary>Third stage: orient the camera to point at the target</summary>
            Aim,

            /// <summary>Final pipeline stage: apply noise (this is done separately, in the
            /// Correction channel of the CameraState)</summary>
            Noise,

            /// <summary>Post-correction stage.  This is invoked on all virtual camera
            /// types, after the pipeline is complete</summary>
            Finalize
        };

        private static ACECore sInstance = null;

        /// <summary>Get the singleton instance</summary>
        public static ACECore Instance
        {
            get
            {
                if (sInstance == null)
                    sInstance = new ACECore();
                return sInstance;
            }
        }

        /// <summary>
        /// If true, show hidden Cinemachine objects, to make manual script mapping possible.
        /// </summary>
        public static bool sShowHiddenObjects = false;

        /// <summary>Delegate for overriding Unity's default input system.  Returns the value
        /// of the named axis.</summary>
        public delegate float AxisInputDelegate(string axisName);

        /// <summary>Delegate for overriding Unity's default input system.
        /// If you set this, then your delegate will be called instead of
        /// System.Input.GetAxis(axisName) whenever in-game user input is needed.</summary>
        public static AxisInputDelegate GetInputAxis = UnityEngine.Input.GetAxis;

        /// <summary>
        /// If non-negative, cinemachine will update with this uniform delta time.
        /// Usage is for timelines in manual update mode.
        /// </summary>
        public static float UniformDeltaTimeOverride = -1;

        /// <summary>
        /// Replacement for Time.deltaTime, taking UniformDeltaTimeOverride into account.
        /// </summary>
        public static float DeltaTime => UniformDeltaTimeOverride >= 0 ? UniformDeltaTimeOverride : Time.deltaTime;

        /// <summary>
        /// If non-negative, cinemachine willuse this value whenever it wants current game time.
        /// Usage is for master timelines in manual update mode, for deterministic behaviour.
        /// </summary>
        public static float CurrentTimeOverride = -1;

        /// <summary>
        /// Replacement for Time.time, taking CurrentTimeTimeOverride into account.
        /// </summary>
        public static float CurrentTime => CurrentTimeOverride >= 0 ? CurrentTimeOverride : Time.time;

        /// <summary>
        /// Delegate for overriding a blend that is about to be applied to a transition.
        /// A handler can either return the default blend, or a new blend specific to
        /// current conditions.
        /// </summary>
        /// <param name="fromVcam">The outgoing virtual camera</param>
        /// <param name="toVcam">Yhe incoming virtual camera</param>
        /// <param name="defaultBlend">The blend that would normally be applied</param>
        /// <param name="owner">The context in which the blend is taking place.
        /// Can be a ACEBrain, or CinemachineStateDrivenCamera, or other manager
        /// object that can initiate a blend</param>
        /// <returns>The blend definition to use for this transition.</returns>
        public delegate CinemachineBlendDefinition GetBlendOverrideDelegate(
            ICamera fromVcam, ICamera toVcam,
            CinemachineBlendDefinition defaultBlend,
            MonoBehaviour owner);

        /// <summary>
        /// Delegate for overriding a blend that is about to be applied to a transition.
        /// A handler can either return the default blend, or a new blend specific to
        /// current conditions.
        /// </summary>
        public static GetBlendOverrideDelegate GetBlendOverride;

        /// <summary>This event will fire after a brain updates its Camera</summary>
        public static ACEBrain.BrainEvent CameraUpdatedEvent = new ACEBrain.BrainEvent();

        /// <summary>This event will fire after a brain updates its Camera</summary>
        public static ACEBrain.BrainEvent CameraCutEvent = new ACEBrain.BrainEvent();

        /// <summary>List of all active ACEBrains.</summary>
        private List<ACEBrain> mActiveBrains = new List<ACEBrain>();

        /// <summary>Access the array of active ACEBrains in the scene</summary>
        public int BrainCount { get { return mActiveBrains.Count; } }

        /// <summary>Access the array of active ACEBrains in the scene
        /// without gebnerating garbage</summary>
        /// <param name="index">Index of the brain to access, range 0-BrainCount</param>
        /// <returns>The brain at the specified index</returns>
        public ACEBrain GetActiveBrain(int index)
        {
            return mActiveBrains[index];
        }

        /// <summary>Called when a ACEBrain is enabled.</summary>
        internal void AddActiveBrain(ACEBrain brain)
        {
            // First remove it, just in case it's being added twice
            RemoveActiveBrain(brain);
            mActiveBrains.Insert(0, brain);
        }

        /// <summary>Called when a ACEBrain is disabled.</summary>
        internal void RemoveActiveBrain(ACEBrain brain)
        {
            mActiveBrains.Remove(brain);
        }

        /// <summary>List of all active ICameras.</summary>
        private List<VirtualCamBase> mActiveCameras = new List<VirtualCamBase>();

        /// <summary>
        /// List of all active Cinemachine Virtual Cameras for all brains.
        /// This list is kept sorted by priority.
        /// </summary>
        public int VirtualCameraCount { get { return mActiveCameras.Count; } }

        /// <summary>Access the array of active ICamera in the scene
        /// without gebnerating garbage</summary>
        /// <param name="index">Index of the camera to access, range 0-VirtualCameraCount</param>
        /// <returns>The virtual camera at the specified index</returns>
        public VirtualCamBase GetVirtualCamera(int index)
        {
            return mActiveCameras[index];
        }

        /// <summary>Called when a Cinemachine Virtual Camera is enabled.</summary>
        internal void AddActiveCamera(VirtualCamBase vcam)
        {
            // Bring it to the top of the list
            RemoveActiveCamera(vcam);

            // Keep list sorted by priority
            int insertIndex;
            for (insertIndex = 0; insertIndex < mActiveCameras.Count; ++insertIndex)
                if (vcam.Priority >= mActiveCameras[insertIndex].Priority)
                    break;

            mActiveCameras.Insert(insertIndex, vcam);
        }

        /// <summary>Called when a Cinemachine Virtual Camera is disabled.</summary>
        internal void RemoveActiveCamera(VirtualCamBase vcam)
        {
            mActiveCameras.Remove(vcam);
        }

        /// <summary>Called when a Cinemachine Virtual Camera is destroyed.</summary>
        internal void CameraDestroyed(VirtualCamBase vcam)
        {
            if (mActiveCameras.Contains(vcam))
                mActiveCameras.Remove(vcam);
            if (mUpdateStatus != null && mUpdateStatus.ContainsKey(vcam))
                mUpdateStatus.Remove(vcam);
        }

        // Registry of all vcams that are present, active or not
        private List<List<VirtualCamBase>> mAllCameras
            = new List<List<VirtualCamBase>>();

        /// <summary>Called when a vcam is enabled.</summary>
        internal void CameraEnabled(VirtualCamBase vcam)
        {
            int parentLevel = 0;
            for (ICamera p = vcam.ParentCamera; p != null; p = p.ParentCamera)
                ++parentLevel;
            while (mAllCameras.Count <= parentLevel)
                mAllCameras.Add(new List<VirtualCamBase>());
            mAllCameras[parentLevel].Add(vcam);
        }

        /// <summary>Called when a vcam is disabled.</summary>
        internal void CameraDisabled(VirtualCamBase vcam)
        {
            for (int i = 0; i < mAllCameras.Count; ++i)
                mAllCameras[i].Remove(vcam);
            if (mRoundRobinVcamLastFrame == vcam)
                mRoundRobinVcamLastFrame = null;
        }

        VirtualCamBase mRoundRobinVcamLastFrame = null;

        static float mLastUpdateTime;
        static int FixedFrameCount { get; set; } // Current fixed frame count

        /// <summary>Update all the active vcams in the scene, in the correct dependency order.</summary>
        internal void UpdateAllActiveVirtualCameras(int layerMask, Vector3 worldUp, float deltaTime)
        {
            // Setup for roundRobin standby updating
            var filter = CurrentUpdateFilter;
            bool canUpdateStandby = (filter != UpdateFilter.SmartFixed); // never in smart fixed
            VirtualCamBase currentRoundRobin = mRoundRobinVcamLastFrame;

            // Update the fixed frame count
            float now = ACECore.CurrentTime;
            if (now != mLastUpdateTime)
            {
                mLastUpdateTime = now;
                if ((filter & ~UpdateFilter.Smart) == UpdateFilter.Fixed)
                    ++FixedFrameCount;
            }

            // Update the leaf-most cameras first
            for (int i = mAllCameras.Count-1; i >= 0; --i)
            {
                var sublist = mAllCameras[i];
                for (int j = sublist.Count - 1; j >= 0; --j)
                {
                    var vcam = sublist[j];
                    if (canUpdateStandby && vcam == mRoundRobinVcamLastFrame)
                        currentRoundRobin = null; // update the next roundrobin candidate
                    if (vcam == null)
                    {
                        sublist.RemoveAt(j);
                        continue; // deleted
                    }
                    if (vcam.m_StandbyUpdate == VirtualCamBase.StandbyUpdateMode.Always
                        || IsLive(vcam))
                    {
                        // Skip this vcam if it's not on the layer mask
                        if (((1 << vcam.gameObject.layer) & layerMask) != 0)
                            UpdateVirtualCamera(vcam, worldUp, deltaTime);
                    }
                    else if (currentRoundRobin == null
                        && mRoundRobinVcamLastFrame != vcam
                        && canUpdateStandby
                        && vcam.m_StandbyUpdate != VirtualCamBase.StandbyUpdateMode.Never
                        && vcam.isActiveAndEnabled)
                    {
                        // Do the round-robin update
                        CurrentUpdateFilter &= ~UpdateFilter.Smart; // force it
                        UpdateVirtualCamera(vcam, worldUp, deltaTime);
                        CurrentUpdateFilter = filter;
                        currentRoundRobin = vcam;
                    }
                }
            }

            // Did we manage to update a roundrobin?
            if (canUpdateStandby)
            {
                if (currentRoundRobin == mRoundRobinVcamLastFrame)
                    currentRoundRobin = null; // take the first candidate
                mRoundRobinVcamLastFrame = currentRoundRobin;
            }
        }

        /// <summary>
        /// Update a single Cinemachine Virtual Camera if and only if it
        /// hasn't already been updated this frame.  Always update vcams via this method.
        /// Calling this more than once per frame for the same camera will have no effect.
        /// </summary>
        internal void UpdateVirtualCamera(
            VirtualCamBase vcam, Vector3 worldUp, float deltaTime)
        {
            if (vcam == null)
                return;

            bool isSmartUpdate = (CurrentUpdateFilter & UpdateFilter.Smart) == UpdateFilter.Smart;
            UpdateTracker.UpdateClock updateClock
                = (UpdateTracker.UpdateClock)(CurrentUpdateFilter & ~UpdateFilter.Smart);

            // If we're in smart update mode and the target moved, then we must examine
            // how the target has been moving recently in order to figure out whether to
            // update now
            if (isSmartUpdate)
            {
                Transform updateTarget = GetUpdateTarget(vcam);
                if (updateTarget == null)
                    return;   // vcam deleted
                if (UpdateTracker.GetPreferredUpdate(updateTarget) != updateClock)
                    return;   // wrong clock
            }

            // Have we already been updated this frame?
            if (mUpdateStatus == null)
                mUpdateStatus = new Dictionary<VirtualCamBase, UpdateStatus>();
            if (!mUpdateStatus.TryGetValue(vcam, out UpdateStatus status))
            {
                status = new UpdateStatus
                {
                    lastUpdateDeltaTime = -2,
                    lastUpdateMode = UpdateTracker.UpdateClock.Late,
                    lastUpdateFrame = Time.frameCount + 2, // so that frameDelta ends up negative
                    lastUpdateFixedFrame = FixedFrameCount + 2
                };
                mUpdateStatus.Add(vcam, status);
            }
            int frameDelta = (updateClock == UpdateTracker.UpdateClock.Late)
                ? Time.frameCount - status.lastUpdateFrame
                : FixedFrameCount - status.lastUpdateFixedFrame;
            if (deltaTime >= 0)
            {
                if (frameDelta == 0 && status.lastUpdateMode == updateClock
                        && status.lastUpdateDeltaTime == deltaTime)
                    return; // already updated
                if (frameDelta > 0)
                    deltaTime *= frameDelta; // try to catch up if multiple frames
            }

//Debug.Log((vcam.ParentCamera == null ? "" : vcam.ParentCamera.Name + ".") + vcam.Name + ": frame " + Time.frameCount + "/" + status.lastUpdateFixedFrame + ", " + CurrentUpdateFilter + ", deltaTime = " + deltaTime);
            vcam.InternalUpdateCameraState(worldUp, deltaTime);
            status.lastUpdateFrame = Time.frameCount;
            status.lastUpdateFixedFrame = FixedFrameCount;
            status.lastUpdateMode = updateClock;
            status.lastUpdateDeltaTime = deltaTime;
        }

        class UpdateStatus
        {
            public int lastUpdateFrame;
            public int lastUpdateFixedFrame;
            public UpdateTracker.UpdateClock lastUpdateMode;
            public float lastUpdateDeltaTime;
        }
        Dictionary<VirtualCamBase, UpdateStatus> mUpdateStatus;

        [RuntimeInitializeOnLoadMethod]
        static void InitializeModule()
        {
            ACECore.Instance.mUpdateStatus = new Dictionary<VirtualCamBase, UpdateStatus>();
        }

        /// <summary>Internal use only</summary>
        internal enum UpdateFilter
        {
            Fixed = UpdateTracker.UpdateClock.Fixed,
            Late = UpdateTracker.UpdateClock.Late,
            Smart = 8, // meant to be or'ed with the others
            SmartFixed = Smart | Fixed,
            SmartLate = Smart | Late
        }
        internal UpdateFilter CurrentUpdateFilter { get; set; }

        private static Transform GetUpdateTarget(VirtualCamBase vcam)
        {
            if (vcam == null || vcam.gameObject == null)
                return null;
            Transform target = vcam.LookAt;
            if (target != null)
                return target;
            target = vcam.Follow;
            if (target != null)
                return target;
            // If no target, use the vcam itself
            return vcam.transform;
        }

        /// <summary>Internal use only - inspector</summary>
        public UpdateTracker.UpdateClock GetVcamUpdateStatus(VirtualCamBase vcam)
        {
            UpdateStatus status;
            if (mUpdateStatus == null || !mUpdateStatus.TryGetValue(vcam, out status))
                return UpdateTracker.UpdateClock.Late;
            return status.lastUpdateMode;
        }

        /// <summary>
        /// Is this virtual camera currently actively controlling any Camera?
        /// </summary>
        public bool IsLive(ICamera vcam)
        {
            if (vcam != null)
            {
                for (int i = 0; i < BrainCount; ++i)
                {
                    ACEBrain b = GetActiveBrain(i);
                    if (b != null && b.IsLive(vcam))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Signal that the virtual has been activated.
        /// If the camera is live, then all ACEBrains that are showing it will
        /// send an activation event.
        /// </summary>
        public void GenerateCameraActivationEvent(ICamera vcam, ICamera vcamFrom)
        {
            if (vcam != null)
            {
                for (int i = 0; i < BrainCount; ++i)
                {
                    ACEBrain b = GetActiveBrain(i);
                    if (b != null && b.IsLive(vcam))
                        b.m_CameraActivatedEvent.Invoke(vcam, vcamFrom);
                }
            }
        }

        /// <summary>
        /// Signal that the virtual camera's content is discontinuous WRT the previous frame.
        /// If the camera is live, then all ACEBrains that are showing it will send a cut event.
        /// </summary>
        public void GenerateCameraCutEvent(ICamera vcam)
        {
            if (vcam != null)
            {
                for (int i = 0; i < BrainCount; ++i)
                {
                    ACEBrain b = GetActiveBrain(i);
                    if (b != null && b.IsLive(vcam))
                    {
                        if (b.m_CameraCutEvent != null)
                            b.m_CameraCutEvent.Invoke(b);
                        if (CameraCutEvent != null)
                            CameraCutEvent.Invoke(b);
                    }
                }
            }
        }

        /// <summary>
        /// Try to find a ACEBrain to associate with a
        /// Cinemachine Virtual Camera.  The first ACEBrain
        /// in which this Cinemachine Virtual Camera is live will be used.
        /// If none, then the first active ACEBrain with the correct
        /// layer filter will be used.
        /// Brains with OutputCamera == null will not be returned.
        /// Final result may be null.
        /// </summary>
        /// <param name="vcam">Virtual camera whose potential brain we need.</param>
        /// <returns>First ACEBrain found that might be
        /// appropriate for this vcam, or null</returns>
        public ACEBrain FindPotentialTargetBrain(VirtualCamBase vcam)
        {
            if (vcam != null)
            {
                int numBrains = BrainCount;
                for (int i = 0; i < numBrains; ++i)
                {
                    ACEBrain b = GetActiveBrain(i);
                    if (b != null && b.OutputCamera != null && b.IsLive(vcam))
                        return b;
                }
                int layer = 1 << vcam.gameObject.layer;
                for (int i = 0; i < numBrains; ++i)
                {
                    ACEBrain b = GetActiveBrain(i);
                    if (b != null && b.OutputCamera != null && (b.OutputCamera.cullingMask & layer) != 0)
                        return b;
                }
            }
            return null;
        }
    }
}