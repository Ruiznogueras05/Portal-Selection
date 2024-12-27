using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;

namespace cs5678_2024sp.p_project.g02
{

    /// <summary>
    /// PortalSelection manages the selection and teleportation of the portal based on user input and feedback from
    /// the PortalSelectionFeedback class. When the user performs a pinch gesture, the portal is teleported to
    /// the location where the line renderer (raycast) from PortalSelectionFeedback hit a valid surface.
    /// </summary>
    public class PortalSelection : MonoBehaviour
    {
        /// <summary>
        /// Reference to the left pinch gesture input action.
        /// </summary>
        [SerializeField] private InputActionReference m_leftPinchActionReference;
        
        /// <summary>
        /// Reference to the right pinch gesture input action.
        /// </summary>
        [SerializeField] private InputActionReference m_rightPinchActionReference;
        
        /// <summary>
        /// The GameObject that represents the marker for portal placement.
        /// </summary>
        [SerializeField] private GameObject m_portalObject; 
        
        /// <summary>
        /// Prefab for displaying the portal preview before placement.
        /// </summary>
        [SerializeField] private GameObject m_portalPreviewPrefab;
        
        /// <summary>
        /// Transform of the user's right hand to track its rotation.
        /// </summary>
        [SerializeField] private Transform m_rightHandTransform;
        
        /// <summary>
        /// Reference to the Portals system managing multiple portals.
        /// </summary>
        [SerializeField] Portals m_Portals;
        
        /// <summary>
        /// Feedback system for portal interactions.
        /// </summary>
        [SerializeField] private PortalsFeedback m_PortalsFeedback;
        
        /// <summary>
        /// Reference to the PortalSelectionFeedback script that handles visual feedback for portal placement.
        /// </summary>
        private PortalSelectionFeedback m_selectionFeedback; 
        
        /// <summary>
        /// Initial state of the portal.
        /// </summary>
        private PortalState m_currentState = PortalState.PortalOff; 
        
        /// <summary>
        /// Instance of the portal preview object.
        /// </summary>
        private GameObject m_portalPreviewInstance;
        
        /// <summary>
        /// Initial rotation of the portal preview when the right pinch starts.
        /// </summary>
        private Quaternion m_initialPortalRotation;
        
        /// <summary>
        /// Stores the initial rotation of the right hand when the right pinch starts.
        /// </summary>
        private Quaternion m_initialRightHandRotation; 
        
        public InputActionReference leftPinchActionReference { get => m_leftPinchActionReference; set => m_leftPinchActionReference = value; }
        public InputActionReference rightPinchActionReference { get => m_rightPinchActionReference; set => m_rightPinchActionReference = value; }
        public GameObject portalObject { get => m_portalObject; set => m_portalObject = value; }
        public GameObject portalPreviewPrefab{ get => m_portalPreviewPrefab; set => m_portalPreviewPrefab = value; }
        public Transform rightHandTransform { get => m_rightHandTransform; set => m_rightHandTransform = value; }
        public Portals Portals { get => m_Portals; set => m_Portals = value; }
        public PortalsFeedback PortalsFeedback { get => m_PortalsFeedback; set => m_PortalsFeedback = value; }

        
        /// <summary>
        /// Defines the state of the portal, whether it is active or not.
        /// </summary>
        public enum PortalState
        {
            /// <summary>
            /// Indicates that the portal is inactive and ready to be placed.
            /// </summary>
            PortalOff,  
            /// <summary>
            /// Indicates that the portal is active and not available for repositioning.
            /// </summary>
            PortalOn    
        }
        
        private void Awake()
        {
            // Retrieve the PortalSelectionFeedback component from the same GameObject.
            m_selectionFeedback = GetComponent<PortalSelectionFeedback>();
           
            if (m_selectionFeedback == null)
            {
                Debug.LogError("PortalSelectionFeedback component not found on the GameObject.");
            }
            m_PortalsFeedback.finishedPortalSetup += OnPortalSetup;
            if (m_portalPreviewPrefab != null)
            {
                m_portalPreviewInstance = Instantiate(m_portalPreviewPrefab, Vector3.zero, Quaternion.identity);
                m_portalPreviewInstance.SetActive(false); // Initially inactive
            }
        }
        
        /// <summary>
        /// Deactivates the portal object when the setup is complete.
        /// </summary>
        private void OnPortalSetup()
        {
            m_portalObject.SetActive(false);
        }

        /// <summary>
        /// Enables the input actions on component enable.
        /// </summary>
        private void OnEnable()
        {
            
            m_leftPinchActionReference.action.Enable(); // Enable the input action for the pinch gesture.
            m_rightPinchActionReference.action.Enable();
            m_leftPinchActionReference.action.performed += OnLeftPinchPerformed; // Subscribe to the event that is triggered when the left pinch gesture is performed.
            m_rightPinchActionReference.action.started += OnRightPinchStart; // Triggered when pinch starts
            m_rightPinchActionReference.action.canceled += OnRightPinchEnd;   // Triggered when pinch ends
        }

        /// <summary>
        /// Disables the input actions and unsubscribes from events on component disable.
        /// </summary>
        private void OnDisable()
        {
            m_leftPinchActionReference.action.Disable();
            m_rightPinchActionReference.action.Disable();
            m_leftPinchActionReference.action.performed -= OnLeftPinchPerformed; // Unsubscribe from the left pinch gesture event upon disabling the script.
            m_rightPinchActionReference.action.started -= OnRightPinchStart;
            m_rightPinchActionReference.action.canceled -= OnRightPinchEnd;
        }
        
        private void Update()
        {
            if (m_selectionFeedback.IsHit && m_selectionFeedback.IsValidSurface)
            {
                m_portalPreviewInstance.transform.position = m_selectionFeedback.HitPoint + m_selectionFeedback.HitNormal * 0.05f;
                if (m_selectionFeedback.IsRaycastFixed)
                {
                    // Apply the rotation difference to the initial portal rotation
                    Quaternion currentRotation = m_rightHandTransform.rotation;
                    Quaternion rotationDelta = Quaternion.Inverse(m_initialRightHandRotation) * currentRotation;
                    
                    float rotationMultiplier = 10f;
                    rotationDelta = Quaternion.Slerp(Quaternion.identity, rotationDelta, rotationMultiplier);

                    m_portalPreviewInstance.transform.rotation = m_initialPortalRotation * rotationDelta;
                }
                else
                {
                    m_portalPreviewInstance.transform.rotation = Quaternion.LookRotation(m_selectionFeedback.HitNormal);
                }
                m_portalPreviewInstance.SetActive(m_currentState == PortalState.PortalOff);
            }
            else
            {
                m_portalPreviewInstance.SetActive(false);
            }
        }

        /// <summary>
        /// Toggles the portal's state based on user interaction. It places or removes the portal based on the current state and user gestures.
        /// </summary>
        public void OnLeftPinchPerformed(InputAction.CallbackContext context)
        {
            if (m_selectionFeedback.IsHit)
            {
                TogglePortal();
            }
        }
        
        /// <summary>
        /// Handles the right pinch gesture to fix the portal's position in 3D space.
        /// </summary>
        public void OnRightPinchStart(InputAction.CallbackContext context)
        {
            if (m_selectionFeedback.IsHit)
            {
                m_initialRightHandRotation = m_rightHandTransform.rotation;
                m_initialPortalRotation = m_portalPreviewInstance.transform.rotation;
                m_selectionFeedback.FixRaycastLength();
               
            }
        }
        
        /// <summary>
        /// Handles the right pinch gesture to release the portal's position in 3D space.
        /// </summary>
        public void OnRightPinchEnd(InputAction.CallbackContext context)
        {
            m_selectionFeedback.ReleaseRaycastLength();  // Stop adjusting the rotation
        }

        /// <summary>
        /// Changes the portal's state between active and inactive.
        /// </summary>
        public void TogglePortal()
        {
            if (m_Portals.handInPortal == false)
            {
                if (m_currentState == PortalState.PortalOff && m_selectionFeedback.IsValidSurface)
                {
                    PlacePortal();
                }
                else if (m_currentState == PortalState.PortalOn)
                {
                    RemovePortal();
                }
            }
        }

        /// <summary>
        /// Places the portal at the calculated position and orientation, and activates it.
        /// </summary>
        public void PlacePortal()
        {
            Vector3 offsetPosition = m_selectionFeedback.HitPoint + m_selectionFeedback.HitNormal * 0.05f;
            m_portalObject.transform.position = offsetPosition;
            // Use the last known rotation from the preview
            m_portalObject.transform.rotation = m_portalPreviewInstance.transform.rotation;
            m_portalObject.transform.Rotate(0, 180, 0);
            m_portalObject.SetActive(true);
            m_selectionFeedback.SetRayCastingActive(false);
            m_currentState = PortalState.PortalOn;
            m_portalPreviewInstance.SetActive(false);
        }
        
        /// <summary>
        /// Deactivates the portal and makes it available for repositioning.
        /// </summary>
        public void RemovePortal()
        {
            m_portalObject.SetActive(false);
            m_selectionFeedback.SetRayCastingActive(true);
            m_currentState = PortalState.PortalOff;
        }
        
    }
}