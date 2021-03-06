﻿using UnityEngine;
using System.Collections;

[ RequireComponent ( typeof( Rigidbody ) ) ]
public class InteractableItemBase : MonoBehaviour
{
    [SerializeField] private bool m_attachable = true;
    [SerializeField] private bool m_dynamic = true;

    protected bool m_attached = false;
    protected InteractableHand m_handObject;
    protected SteamVR_Controller.Device m_device;

    protected Rigidbody m_rigidbody;

    protected Vector3 m_previousPosition;
    protected Quaternion m_previousRotation;
    protected Vector3 m_previousForward;

    protected Transform m_transform;

    protected Vector3 storedVelocity;

    protected float m_hookedSensitivity = 0.5f;

    protected float m_minHookedTensionChangeTime = 0.3f;
    protected float m_maxHookedTensionChangeTime = 2.0f;

    protected float m_minHookedTension = 0.1f;
    protected float m_maxHookedTension = 1.5f;

    public bool Attachable
    {
        get { return m_attachable; }
    }

    public float GetHookedSensitivity()
    {
        return m_hookedSensitivity;
    }

    public float GetRandomFishTension()
    {
        return Random.Range( m_minHookedTension, m_maxHookedTension );
    }

    public float GetRandomFishTensionChangeTime()
    {
        return Random.Range( m_minHookedTensionChangeTime, m_maxHookedTensionChangeTime );
    }

    public SteamVR_Controller.Device Device
    {
        get { return m_device; }
    }

    public bool OnMouth { get; set; }

    protected virtual void Awake()
    {
        m_transform = transform;
        m_rigidbody = GetComponent<Rigidbody>();
        if(m_rigidbody != null)
        {
            m_rigidbody.isKinematic = !m_dynamic;
            m_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    protected virtual void Update()
    {

    }

    protected virtual void LateUpdate()
    {
        m_previousPosition = m_transform.position;
        m_previousRotation = m_transform.rotation;
        m_previousForward = m_transform.forward;
    }

    public virtual bool Attach(Transform parent, InteractableHand hand, int deviceIndex, bool worldPositionStays)
    {
        if (m_attached) return false;

        m_attached = true;

#if !UNITY_EDITOR_OSX
        m_device = SteamVR_Controller.Input(deviceIndex);

        m_transform.SetParent(parent, worldPositionStays);
        m_handObject = hand;

        if(!worldPositionStays)
        {
            m_transform.localPosition = Vector3.zero;
            m_transform.localRotation = Quaternion.identity;
        }

        if (m_rigidbody != null) m_rigidbody.isKinematic = true;
#endif

        return true;
    }

    public virtual void Detach()
    {
        m_attached = false;
        m_device = null;
        m_handObject = null;
        m_transform.SetParent(null, true);

        if (m_rigidbody != null)
        {
            m_rigidbody.isKinematic = !m_dynamic;

            Vector3 deltaPos = m_transform.position - m_previousPosition;
            Vector3 newVel = deltaPos / Time.deltaTime;

            m_rigidbody.AddForce(newVel, ForceMode.VelocityChange);

            /*
            Quaternion deltaRot = Quaternion.Inverse(m_transform.rotation) * m_previousRotation;
            Vector3 newTorque = deltaRot.eulerAngles / Time.deltaTime;

            m_rigidbody.AddTorque(newTorque, ForceMode.VelocityChange);
            */

            Vector3 x = Vector3.Cross(m_previousForward, m_transform.forward);
            float theta = Mathf.Asin(x.magnitude);
            Vector3 w = x.normalized * theta / Time.fixedDeltaTime;

            Quaternion q = transform.rotation * m_rigidbody.inertiaTensorRotation;
            Vector3 newTorque = q * Vector3.Scale(m_rigidbody.inertiaTensor, (Quaternion.Inverse(q) * w));

            m_rigidbody.AddTorque(newTorque, ForceMode.Impulse);
        }
    }

    public bool IsAttached()
    {
        return m_attached;
    }
}
