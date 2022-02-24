using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimManager : MonoBehaviour
{
    private Animator m_Animator;
    private int m_ParameterID;

     [Tooltip("Name of the animation multiplier parameter of the animator")] [SerializeField] private string m_AnimationMultiplierParameter = "Animation Multiplier";
    [Tooltip("Default transition duration between animations")] [SerializeField] private float m_TransitionDuration = 0.1f;
    public static int BaseLayerIndex { get { return 0; } } // Index of base layer

    void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }


    /// <summary>
    /// Check if animator is playing a state
    /// </summary>
    /// <param name="state">state name</param>
    /// <param name="layer">layer</param>
    /// <returns>true: is playing the state; false: is not playing the state</returns>
    public bool IsPlayingState(string state, int layer)
    {
        return m_Animator.GetCurrentAnimatorStateInfo(layer).IsName(state);
    }

    /// <summary>
    /// Set a new state in animator with default parameters
    /// </summary>
    /// <param name="newState">Name of the new state</param>
    public void SetAnimatorState(string newState)
    {
        SetAnimatorState(newState, m_TransitionDuration, BaseLayerIndex);
    }

    public void SetAnimatorState(string newState, float transitionDuration, int layer)
    {
        m_Animator.CrossFadeInFixedTime(newState, transitionDuration, layer);
    }
    public bool HasFinishedAnimation(string state)
    {
        return HasFinishedAnimation(state, BaseLayerIndex);
    }
    /// <summary>
    /// Set animation multiplier parameter
    /// </summary>
    /// <param name="value">New animation multiplier value</param>
    /// <param name="dampTime">Damp time: higher values results in smoother change</param>
    public void SetAnimationMultiplierParameter(float value, float dampTime)
    {
        m_Animator.SetFloat(m_AnimationMultiplierParameter, value, dampTime, Time.fixedDeltaTime);
    }


    /// <summary>
    /// Get animation multiplier parameter
    /// </summary>
    public float GetAnimationMultiplierParameter()
    {
        return m_Animator.GetFloat(m_AnimationMultiplierParameter);
    }

    public bool HasFinishedAnimation(string state, int layer=0, bool includeLoop = false)
    {
        if (m_Animator.GetCurrentAnimatorStateInfo(layer).IsName(state))
        {
            // All looped animation should be threat with no end
            if (m_Animator.GetCurrentAnimatorStateInfo(layer).loop && !includeLoop)
                return false;

            if (GetNormalizedTime(layer, includeLoop) >= 0.9f)
                return true;
        }

        return false;
    }
    public float GetNormalizedTime(int layer = 0, bool loop = false)
    {
        if (loop)
            return m_Animator.GetCurrentAnimatorStateInfo(layer).normalizedTime % 1;

        return m_Animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
    }

    public void SetHorizontalParameter(string ParameterName, float value, float dampTime)
    {
        m_ParameterID = Animator.StringToHash(ParameterName);
        m_Animator.SetFloat(m_ParameterID, value, dampTime, Time.deltaTime);
    }
    public void PerformBoolEvent(string ParameterName, bool value)
    {
        m_Animator.SetBool(ParameterName, value);
    }
    public float GetFloatParameter(string ParameterName)
    {
        return m_Animator.GetFloat(ParameterName);
    }
    public void SetFloatParameter(string ParameterName, float value ,float dampTime=0.0f)
    {
        int m_LocalParameterID = Animator.StringToHash(ParameterName);
        m_Animator.SetFloat(m_LocalParameterID, value,dampTime,Time.deltaTime);
    }
    public void TriggerState(string ParameterName)
    {
        m_Animator.SetTrigger(ParameterName);
    }

}
