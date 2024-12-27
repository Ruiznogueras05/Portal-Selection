using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace cs5678_2024sp.p_project.g02
{

    /// <summary>
    /// This component implements the logic for the portals present in the Portal Selection interaction technique as
    /// presented in our paper. This script is responsible for check the objects currently colliding with either of the
    /// portals, and warping them to the other portal if they cross through a portal.
    /// </summary>
    public class Portals : MonoBehaviour
    {
        [SerializeField] private GameObject m_PortalA;
        [SerializeField] private GameObject m_PortalB;
        [SerializeField] private GameObject m_RightPalm;

        private PortalCollisions m_PortalCollisionsA;
        private PortalCollisions m_PortalCollisionsB;

        private Transform m_RightControllerOffset;

        private bool m_HandInPortal;
        

        /// <summary>
        /// The first of the two portal GameObjects, appearing with an orange outline and anchored near the user.
        /// </summary>
        public GameObject portalA
        {
            get { return m_PortalA; }
            set { m_PortalA = value; }
        }

        /// <summary>
        /// The first of the two portal GameObjects, appearing with a blue outline and placeable by the user.
        /// </summary>
        public GameObject portalB
        {
            get { return m_PortalB; }
            set { m_PortalB = value; }
        }
        
        /// <summary>
        /// The part of the right hand used to detect collisions and as a midpoint of the hand for teleportation.
        /// </summary>
        public GameObject rightPalm
        {
            get { return m_RightPalm; }
        }
        
        /// <summary>
        /// The component responsible for tracking objects currently colliding with portal A.
        /// </summary>
        public PortalCollisions portalCollisionsA
        {
            get { return m_PortalCollisionsA; }
        }
        
        /// <summary>
        /// The component responsible for tracking objects currently colliding with portal B.
        /// </summary>
        public PortalCollisions portalCollisionsB
        {
            get { return m_PortalCollisionsB; }
        }
        
        /// <summary>
        /// Boolean flag used to indicate that the player's hand is currently in the portal.
        /// </summary>
        public bool handInPortal
        {
            get { return m_HandInPortal; }
        }

        // Start is called before the first frame update
        void Awake()
        {
            m_PortalCollisionsA = m_PortalA.GetComponent<PortalCollisions>();
            m_PortalCollisionsB = m_PortalB.GetComponent<PortalCollisions>();

            m_RightControllerOffset = m_RightPalm.transform.parent.parent.parent.parent;

            m_HandInPortal = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (m_PortalA.activeSelf && m_PortalB.activeSelf)
            {
                if (m_PortalCollisionsA)
                {
                    foreach (GameObject portalObject in m_PortalCollisionsA.portalObjects)
                    {

                        if (portalObject == m_RightPalm)
                        {
                            Vector3 objPos = m_PortalA.transform.InverseTransformPoint(portalObject.transform.position);
                            if (objPos.z > 0 && !m_HandInPortal)
                            {
                                Warp(m_PortalA.transform, m_PortalB.transform, m_RightControllerOffset);
                                m_HandInPortal = true;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("No Portal Collisons A component");
                }

                if (m_PortalCollisionsB)
                {
                    foreach (GameObject portalObject in m_PortalCollisionsB.portalObjects)
                    {
                        if (portalObject == m_RightPalm)
                        {
                            Vector3 objPos = m_PortalB.transform.InverseTransformPoint(portalObject.transform.position);
                            if (objPos.z > 0 && m_HandInPortal)
                            {
                                Warp(m_PortalB.transform, m_PortalA.transform, m_RightControllerOffset.transform);
                                m_HandInPortal = false;
                            }
                        }
                    }
                }
                else
                {
                    Debug.Log("No Portal Collisons B component");
                }
            }
        }
        
        /// <summary>
        /// Instantaneously transports the warpObject through the portal inPortal to the portal outPortal.
        /// </summary>
        public void Warp(Transform inPortal, Transform outPortal, Transform warpObject)
        {
            Vector3 relativePos = inPortal.InverseTransformPoint(warpObject.position);
            relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
            warpObject.position = outPortal.TransformPoint(relativePos);

            // Update rotation of object.
            Quaternion relativeRot = Quaternion.Inverse(inPortal.rotation) * warpObject.rotation;
            relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
            warpObject.rotation = outPortal.rotation * relativeRot;
        }
    }
}
