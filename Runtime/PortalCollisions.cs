using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace cs5678_2024sp.p_project.g02
{
    /// <summary>
    /// This component handles collisions for the individual portals. Collided objects are added to the portalObjects
    /// property of the portal, which is then accessed by the Portals logic component.
    /// </summary>
    public class PortalCollisions : MonoBehaviour
    {
        private List<GameObject> m_PortalObjects;

        /// <summary>
        /// A list of GameObjects currently colliding with the portal.
        /// </summary>
        public List<GameObject> portalObjects
        {
            get { return m_PortalObjects; }
        }
        
        public event Action<GameObject> collisionStarted;
        public event Action<GameObject> collisionEnded;

        private void Start()
        {
            m_PortalObjects = new List<GameObject>();
        }

        private void OnTriggerEnter(Collider other)
        {
            var obj = other.gameObject;
            if (obj != null)
            {
                m_PortalObjects.Add(obj);
            }
            
            collisionStarted?.Invoke(obj);

            Debug.Log("Trigger Enter!");
            Debug.Log(m_PortalObjects);
        }

        private void OnTriggerExit(Collider other)
        {
            var obj = other.gameObject;

            if (m_PortalObjects.Contains(obj))
            {
                m_PortalObjects.Remove(obj);
            }
            
            collisionEnded?.Invoke(obj);

            Debug.Log("Trigger Exit!");
            Debug.Log(m_PortalObjects);
        }
    }
}
