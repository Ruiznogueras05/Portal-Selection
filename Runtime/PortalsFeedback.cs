using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace cs5678_2024sp.p_project.g02
{
    /// <summary>
    /// This component is responsible for providing feedback for the portals of the Portal Selection interaction technique.
    /// This includes rendering the surface of each portal by positioning a camera behind the view of the opposite portal.
    /// </summary>
    public class PortalsFeedback : MonoBehaviour
    {
        [SerializeField] private Camera m_PortalCameraA;
        [SerializeField] private Camera m_PortalCameraB;
        [SerializeField] private Camera m_MainCamera;
        [SerializeField] private Material m_CameraMaterialA;
        [SerializeField] private Material m_CameraMaterialB;

        [SerializeField] private Material m_RecursionMaterial;

        private GameObject m_PortalA;
        private GameObject m_PortalB;

        private Renderer m_PortalRendererA;
        private Renderer m_PortalRendererB;

        private Portals m_Portals;
        private PortalCollisions m_PortalCollisionsA;
        private PortalCollisions m_PortalCollisionsB;

        private Dictionary<XRGrabInteractable, GameObject> m_InteractableClones;
        
        private MaterialPropertyBlock m_TintPropertyBlock;
        
        Color m_TintColor = Color.yellow;
        
        /// <summary>
        /// The event used to notify other scripts that portals have finished being set up.
        /// </summary>
        public event Action finishedPortalSetup;

        /// <summary>
        /// The camera used to render the perspective of portal A, oriented based on the position of the main camera.
        /// </summary>
        public Camera portalCameraA
        {
            get { return m_PortalCameraA; }
        }
        
        /// <summary>
        /// The camera used to render the perspective of portal B, oriented based on the position of the main camera.
        /// </summary>
        public Camera portalCameraB
        {
            get { return m_PortalCameraB; }
        }
        
        /// <summary>
        /// The main camera of the scene.
        /// </summary>
        public Camera mainCamera
        {
            get { return m_MainCamera; }
        }
        
        /// <summary>
        /// The material to which portal camera A is rendered.
        /// </summary>
        public Material cameraMaterialA
        {
            get { return m_CameraMaterialA; }
        }
        
        /// <summary>
        /// The material to which portal camera B is rendered.
        /// </summary>
        public Material cameraMaterialB
        {
            get { return m_CameraMaterialB; }
        }
        
        /// <summary>
        /// The material used when a portal is in the field of view of another portal.
        /// </summary>
        public Material recursionMaterial
        {
            get { return m_RecursionMaterial; }
        }
        

        private void Start()
        {
            m_Portals = GetComponent<Portals>();

            m_PortalA = m_Portals.portalA;
            m_PortalB = m_Portals.portalB;

            m_PortalCollisionsA = m_Portals.portalCollisionsA;
            m_PortalCollisionsB = m_Portals.portalCollisionsB;

            m_PortalCollisionsA.collisionStarted += OnCollisionStartedA;
            m_PortalCollisionsA.collisionEnded += OnCollisionEndedA;
            
            m_PortalCollisionsB.collisionStarted += OnCollisionStartedB;
            m_PortalCollisionsB.collisionEnded += OnCollisionEndedB;

            m_PortalRendererA = m_PortalA.GetComponent<Renderer>();
            m_PortalRendererB = m_PortalB.GetComponent<Renderer>();

            if (m_PortalCameraA.targetTexture != null)
            {
                m_PortalCameraA.targetTexture.Release();
            }

            int width = XRSettings.eyeTextureWidth;
            int height = XRSettings.eyeTextureHeight;
            if (width < 1)
            {
                width = Screen.width;
                height = Screen.height;
            }
            m_PortalCameraA.targetTexture = new RenderTexture(width, height, 24);
            m_CameraMaterialA.mainTexture = m_PortalCameraA.targetTexture;

            if (m_PortalCameraB.targetTexture != null)
            {
                m_PortalCameraB.targetTexture.Release();
            }

            m_PortalCameraB.targetTexture = new RenderTexture(width, height, 24);
            m_CameraMaterialB.mainTexture = m_PortalCameraB.targetTexture;

            Camera.onPreRender += OnPortalUpdate;
            
            finishedPortalSetup?.Invoke();
            
            m_TintPropertyBlock = new MaterialPropertyBlock();

            m_InteractableClones = new Dictionary<XRGrabInteractable, GameObject>();
        }

        private void OnPortalUpdate(Camera cam)
        {
            if (cam == m_MainCamera)
            {
                if (m_PortalA.activeSelf && m_PortalB.activeSelf)
                {
                    // Set portal materials to blank recursion material to avoid recursion rendering
                    m_PortalRendererA.material = m_RecursionMaterial;
                    m_PortalRendererB.material = m_RecursionMaterial;

                    UpdatePortalCamera(m_PortalA, m_PortalB, m_PortalCameraB);
                    UpdatePortalCamera(m_PortalB, m_PortalA, m_PortalCameraA);

                    m_PortalRendererA.material = m_CameraMaterialB;
                    m_PortalRendererB.material = m_CameraMaterialA;
                }
                
                else
                {
                    m_PortalRendererA.material = m_RecursionMaterial;
                    m_PortalRendererB.material = m_RecursionMaterial;
                }
            }
        }

        private void UpdatePortalCamera(GameObject inPortal, GameObject outPortal, Camera outCamera)
        {
            Transform inTransform = inPortal.transform;
            Transform outTransform = outPortal.transform;

            // Position the camera behind the other portal.
            Vector3 relativePos = inTransform.InverseTransformPoint(m_MainCamera.transform.position);
            relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
            outCamera.transform.position = outTransform.TransformPoint(relativePos);

            // Rotate the camera to look through the other portal.
            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * m_MainCamera.transform.rotation;
            relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
            outCamera.transform.rotation = outTransform.rotation * relativeRot;

            // Set the camera's oblique view frustum.
            Plane p = new Plane(-outTransform.forward, outTransform.position);
            Vector4 clipPlane = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
            Vector4 clipPlaneCameraSpace =
                Matrix4x4.Transpose(Matrix4x4.Inverse(outCamera.worldToCameraMatrix)) * clipPlane;

            var newMatrix = m_MainCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
            outCamera.projectionMatrix = newMatrix;

            outCamera.Render();
        }
        
        private struct ShaderPropertyLookup
        {
            public static readonly int emissionColor = Shader.PropertyToID("_EmissionColor");
        }

        private void SetCloneTint(bool on, Renderer renderer)
        {
            var emissionColor = on ? m_TintColor * Mathf.LinearToGammaSpace(1f) : Color.black;
            
            if (on)
                renderer.material.EnableKeyword("_EMISSION");
            else
                renderer.material.DisableKeyword("_EMISSION");
            
            renderer.GetPropertyBlock(m_TintPropertyBlock);
            m_TintPropertyBlock.SetColor(ShaderPropertyLookup.emissionColor, emissionColor);
            renderer.SetPropertyBlock(m_TintPropertyBlock);
        }

        private void Update()
        {
            bool portalsActive = m_PortalA.activeSelf && m_PortalB.activeSelf;
            foreach (GameObject clone in m_InteractableClones.Values)
            {
                clone.SetActive(portalsActive);
            }
            if (portalsActive)
            {
                foreach (GameObject portalObject in m_PortalCollisionsA.portalObjects)
                {
                    if (portalObject)
                    {
                        XRGrabInteractable interactable = portalObject.GetComponent<XRGrabInteractable>();
                        if (interactable)
                        {
                            if (m_InteractableClones.ContainsKey(interactable))
                            {
                                GameObject clone = m_InteractableClones[interactable];
                                clone.transform.position = interactable.transform.position;
                                clone.transform.rotation = interactable.transform.rotation;

                                m_Portals.Warp(m_PortalA.transform, m_PortalB.transform, clone.transform);

                                SetCloneTint(interactable.isHovered || interactable.isSelected,
                                    clone.GetComponent<Renderer>());
                            }
                        }
                    }
                }

                foreach (GameObject portalObject in m_PortalCollisionsB.portalObjects)
                {
                    if (portalObject)
                    {
                        XRGrabInteractable interactable = portalObject.GetComponent<XRGrabInteractable>();
                        if (interactable)
                        {
                            if (m_InteractableClones.ContainsKey(interactable))
                            {
                                GameObject clone = m_InteractableClones[interactable];
                                clone.transform.position = interactable.transform.position;
                                clone.transform.rotation = interactable.transform.rotation;

                                m_Portals.Warp(m_PortalB.transform, m_PortalA.transform, clone.transform);

                                SetCloneTint(interactable.isHovered || interactable.isSelected,
                                    clone.GetComponent<Renderer>());
                            }
                        }
                    }
                }
            }
        }

        private void OnCollisionStartedA(GameObject portalObject)
        {
            bool isClone = m_InteractableClones.ContainsValue(portalObject);
            bool isInteractable = portalObject.GetComponent<XRGrabInteractable>() != null;
            if (isClone || isInteractable)
            {
                Debug.Log("feedback col start!");
                Material objMaterial = portalObject.GetComponent<Renderer>().material;
                objMaterial.SetVector("sliceCentre", m_PortalA.transform.position);
                objMaterial.SetVector("sliceNormal", m_PortalA.transform.forward);
                objMaterial.SetFloat("sliceOffsetDst", 0);

                if (isInteractable)
                {
                    GameObject newClone = Instantiate(portalObject);
                    XRGrabInteractable gi = newClone.GetComponent<XRGrabInteractable>();
                    if (gi != null)
                    {
                        Destroy(gi);
                    }
                    m_InteractableClones.Add(portalObject.GetComponent<XRGrabInteractable>(), newClone);
                }
            }
        }
        
        private void OnCollisionEndedA(GameObject portalObject)
        { 
            XRGrabInteractable interactable = portalObject.GetComponent<XRGrabInteractable>();
            if (interactable != null)
            {
                Debug.Log("feedback col end!");
                Material objMaterial = portalObject.GetComponent<Renderer>().material;
                objMaterial.SetVector("sliceNormal", Vector3.zero);

                GameObject clone = m_InteractableClones[interactable];
                m_InteractableClones.Remove(interactable);
                Destroy(clone);
            }
        }
        
        private void OnCollisionStartedB(GameObject portalObject)
        {
            bool isClone = m_InteractableClones.ContainsValue(portalObject);
            bool isInteractable = portalObject.GetComponent<XRGrabInteractable>() != null;
            if (isClone || isInteractable)
            {
                Debug.Log("feedback col start!");
                Material objMaterial = portalObject.GetComponent<Renderer>().material;
                objMaterial.SetVector("sliceCentre", m_PortalB.transform.position);
                objMaterial.SetVector("sliceNormal", m_PortalB.transform.forward);
                objMaterial.SetFloat("sliceOffsetDst", 0);

                if (isInteractable)
                {
                    GameObject newClone = Instantiate(portalObject);
                    XRGrabInteractable gi = newClone.GetComponent<XRGrabInteractable>();
                    if (gi != null)
                    {
                        Destroy(gi);
                    }
                    m_InteractableClones.Add(portalObject.GetComponent<XRGrabInteractable>(), newClone);
                }
            }
        }
        
        private void OnCollisionEndedB(GameObject portalObject)
        {
            XRGrabInteractable interactable = portalObject.GetComponent<XRGrabInteractable>();
            if (interactable != null)
            {
                Debug.Log("feedback col end!");
                Material objMaterial = portalObject.GetComponent<Renderer>().material;
                objMaterial.SetVector("sliceNormal", Vector3.zero);

                GameObject clone = m_InteractableClones[interactable];
                m_InteractableClones.Remove(interactable);
                Destroy(clone);
            }
        }
    }
}