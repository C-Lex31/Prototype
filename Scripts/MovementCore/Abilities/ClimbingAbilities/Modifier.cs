using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Added by PeskyDev001 (uid 65)
    public abstract class Modifier : MonoBehaviour
    {
        protected ThirdPersonSystem m_System;

        public virtual void Initialize(ThirdPersonSystem system)
        {
            m_System = system;
        }
        
        public virtual void UpdateModifier() { }

        public virtual void FixedUpdateModifier() { }
    }
