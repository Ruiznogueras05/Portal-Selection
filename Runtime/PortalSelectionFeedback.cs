using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace cs5678_2024sp.p_project.g02
{
    
    /// <summary>
    /// This PortalSelectionFeedback is responsible for providing visual feedback for the portal placement process.
    /// It creates a raycast from a given origin and changes the line renderer's appearance
    /// based on whether a suitable surface for portal placement has been hit.
    /// </summary>
    public class PortalSelectionFeedback : MonoBehaviour
    {
        /// <summary>
        /// The origin point from which the raycast is emitted.
        /// </summary>
        public Transform rayOrigin;
        
        /// <summary>
        /// The default maximum length of the ray when no surface is hit.
        /// </summary>
        public float rayLength = 10.0f; 
        
        /// <summary>
        /// Layer mask to filter the raycast for valid portal surfaces.
        /// </summary>
        public LayerMask raycastLayerMask;
       
        /// <summary>
        /// Layer mask to filter the raycast for invalid portal surfaces.
        /// </summary>
        public LayerMask negativeRaycastLayerMask; 
        
        /// <summary>
        /// The line renderer component used to visualize the raycast.
        /// </summary>
        private LineRenderer lineRenderer; 
        
        /// <summary>
        /// Material that represents an invalid surface
        /// </summary>
        public Material defaultMaterial;
       
        /// <summary>
        /// Material that represents a valid surface
        /// </summary>
        public Material activeMaterial;  
        
        /// <summary>
        /// Property to get the status of the raycast hit 
        /// </summary>
        public bool IsHit { get; private set; }
       
        /// <summary>
        /// Property to get the position of the raycast hit
        /// </summary>
        public Vector3 HitPoint { get; private set; }
        
        /// <summary>
        /// Property to get the normal of the raycast hit
        /// </summary>
        public Vector3 HitNormal { get; private set; }
        
        /// <summary>
        /// Indicates whether the last raycast hit a surface valid for portal placement.
        /// </summary>
        public bool IsValidSurface { get; private set; }

        /// <summary>
        /// Indicates whether the raycast length and direction are currently fixed.
        /// </summary>
        public bool IsRaycastFixed { get; private set; } = false;
        
        /// <summary>
        /// Controls whether raycasting is active
        /// </summary>
        private bool m_enableRayCasting = true; 
        
        void Start()
        {
            // Initialize the line renderer and validate the ray origin.
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) { Debug.LogError("LineRenderer component not found on the GameObject."); }
            if (rayOrigin == null) { Debug.LogError("Ray origin not assigned in the inspector."); }
            else
            {
                // Initialize the line renderer material to the default
                lineRenderer.material = defaultMaterial;
                // Set the width of the line renderer
                lineRenderer.startWidth = 0.02f;
                lineRenderer.endWidth = 0.02f; 
            }
        }

        void Update()
        {
            // Guard clause to exit early if the line renderer or ray origin is not set.
            if (!m_enableRayCasting || lineRenderer == null || rayOrigin == null) return;

            if (!IsRaycastFixed)
            {
                UpdateRaycast();
            }
            else
            {
                // Maintain the fixed point and update the line renderer using the stored fixed rotation
                UpdateFixedRaycast();
            }
            UpdateLineRenderer();
            
        }
        
        /// <summary>
        /// Performs a raycast from the origin point and updates hit detection variables.
        /// </summary>
        private void UpdateRaycast()
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            RaycastHit hitInfo;
            bool hitOccurred = Physics.Raycast(ray, out hitInfo, Mathf.Infinity, raycastLayerMask | negativeRaycastLayerMask);
            IsHit = hitOccurred;
            HitPoint = hitOccurred ? hitInfo.point : rayOrigin.position + rayOrigin.forward * rayLength;
            HitNormal = hitOccurred ? hitInfo.normal : Vector3.up;
            IsValidSurface = hitOccurred && ((1 << hitInfo.collider.gameObject.layer) & raycastLayerMask) != 0;
            
            if (hitOccurred)
            {
                rayLength = Vector3.Distance(rayOrigin.position, HitPoint);
            }
        }
        
        /// <summary>
        /// Maintains a fixed raycast hit point distance while updating its direction to the current forward direction of the ray origin.
        /// </summary>
        private void UpdateFixedRaycast()
        {
            // Keep the hit point at a fixed distance but update the direction
            HitPoint = rayOrigin.position + rayOrigin.forward * rayLength;
            HitNormal = rayOrigin.forward;
        }
        
        /// <summary>
        /// Updates the line renderer to reflect the current raycast state.
        /// </summary>
        private void UpdateLineRenderer()
        {
            lineRenderer.SetPosition(0, rayOrigin.position);
            lineRenderer.SetPosition(1, HitPoint);
            lineRenderer.material = IsValidSurface && IsHit ? activeMaterial : defaultMaterial;
        }
        
        /// <summary>
        /// Enables or disables the raycasting functionality
        /// </summary>
        public void SetRayCastingActive(bool isActive)
        {
            m_enableRayCasting = isActive;
            lineRenderer.enabled = isActive;
        }
        
        /// <summary>
        /// Activates the fixed raycast mode, locking the current raycast properties.
        /// </summary>
        public void FixRaycastLength()
        {
            IsRaycastFixed = true;
        }
        
        /// <summary>
        /// Deactivates the fixed raycast mode, allowing the raycast properties to update.
        /// </summary>
        public void ReleaseRaycastLength()
        {
            IsRaycastFixed = false;
        }
    }
}