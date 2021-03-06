using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ace;
//Developed by C-Lex31 (uid 31)
//Contact cpplexicon@gmail.com
public abstract class PivotBasedCamRig : AbstractTargetFollower
{

    // This script is designed to be placed on the root object of a camera rig,
    // comprising 3 gameobjects, each parented to the next:

    // 	Camera Rig
    // 		Pivot
    // 			Camera
    protected Transform m_Cam; // the transform of the camera
    protected Transform m_Pivot; // the point at which the camera pivots around
    protected Vector3 m_LastTargetPosition;
    protected virtual void Awake()
    {
        // find the camera in the object hierarchy
        m_Cam = GetComponentInChildren<ACEVirtualCamera>().transform;
        m_Pivot = m_Cam.parent;
    }

}
