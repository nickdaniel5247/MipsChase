using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //External tunables.
    static public float m_fMaxSpeed = 5.0f;
    public float m_fSlowSpeed = m_fMaxSpeed * 0.66f;
    public float m_fIncSpeed = 10f;
    public float m_fMagnitudeFast = 0.6f;
    public float m_fMagnitudeSlow = 0.06f;
    public float m_fFastRotateSpeed = 0.2f;
    public float m_fFastRotateMax = 10.0f;
    public float m_fDiveTime = 0.3f;
    public float m_fDiveRecoveryTime = 0.5f;
    public float m_fDiveDistance = 3.0f;
    public float m_fMouseDeadZone = 0.25f;

    //Internal variables.
    public Vector3 m_vDiveStartPos;
    public Vector3 m_vDiveEndPos;
    public float m_fAngle;
    public float m_fSpeed;
    public float m_fTargetSpeed;
    public float m_fTargetAngle;
    public eState m_nState;
    public float m_fDiveStartTime;

    public enum eState : int
    {
        kMoveSlow,
        kMoveFast,
        kDiving,
        kRecovering,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
    {
        new Color(0,     0,   0),
        new Color(255, 255, 255),
        new Color(0,     0, 255),
        new Color(0,   255,   0),
    };

    public bool IsDiving()
    {
        return (m_nState == eState.kDiving);
    }

    void CheckForDive()
    {
        if (Input.GetMouseButton(0) && (m_nState != eState.kDiving && m_nState != eState.kRecovering))
        {
            //Start the dive operation
            m_nState = eState.kDiving;
            m_fSpeed = 0.0f;

            //Store starting parameters.
            m_vDiveStartPos = transform.position;
            m_vDiveEndPos = m_vDiveStartPos - (transform.up * m_fDiveDistance);
            m_fDiveStartTime = Time.time;
        }
    }

    void Start()
    {
        //Initialize variables.
        m_fAngle = 0;
        m_fSpeed = 0;
        m_nState = eState.kMoveSlow;
    }

    void UpdateDirectionAndSpeed()
    {
        //Get relative positions between the mouse and player
        Vector3 vScreenPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));
        Vector2 vOffset = new Vector2(transform.position.x - vScreenPos.x, transform.position.y - vScreenPos.y);

        //Find the target angle being requested, if mouse isn't in deadzone. Deadzone helps with fast glitching rotations.
        if (vOffset.magnitude >= m_fMouseDeadZone)
            m_fTargetAngle = Mathf.Atan2(vOffset.y, vOffset.x) * Mathf.Rad2Deg;

        //Calculate how far away from the player the mouse is.
        float fMouseMagnitude = vOffset.magnitude / vScreenSize.magnitude;

        //Based on distance, calculate the speed the player is requesting.
        if (fMouseMagnitude > m_fMagnitudeFast)
        {
            m_fTargetSpeed = m_fMaxSpeed;
        }
        else if (fMouseMagnitude > m_fMagnitudeSlow)
        {
            m_fTargetSpeed = m_fSlowSpeed;
        }
        else
        {
            m_fTargetSpeed = 0.0f;
        }
    }

    void MovePlayer()
    {
        //m_fAngle is offset by 90 due to m_fTargetAngle's calculation
        transform.rotation = Quaternion.Euler(Vector3.forward * (m_fAngle - 90f));

        //Prefab has player pointing downwards from the up direction so -transform.up is used
        transform.Translate(-transform.up * m_fSpeed * Time.fixedDeltaTime, Space.World);
    }

    void Update()
    {
        CheckForDive();
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];

        switch(m_nState)
        {
        case eState.kMoveSlow:
            //Listen for dive or just move slowly in mouse direction until speed has surpassed slow speed
            UpdateDirectionAndSpeed();

            //Speed is slowly gained and angle is instant
            m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fTargetSpeed, m_fIncSpeed * Time.fixedDeltaTime);
            m_fAngle = m_fTargetAngle;

            MovePlayer();

            //Check if we passed slow threshold
            if (m_fSpeed > m_fSlowSpeed)
            {
                m_nState = eState.kMoveFast;
            }

            break;
        case eState.kMoveFast:
            //Move fast until direction is outside of fast range, then slow down and switch to kMoveSlow
            if (Mathf.Abs(m_fTargetAngle - m_fAngle) > m_fFastRotateMax)
            {
                m_fSpeed = Mathf.MoveTowards(m_fSpeed, m_fSlowSpeed, m_fIncSpeed * Time.fixedDeltaTime);

                if (Mathf.Approximately(m_fSpeed, m_fSlowSpeed) || m_fSpeed < m_fSlowSpeed)
                {
                    // Without this speed reduction, player can instantly go back into fast mode
                    // This is done to replicate the behavior in the example video
                    m_fSpeed = m_fSlowSpeed / 2;
                    m_nState = eState.kMoveSlow;
                    break;
                }
            }
            else
            {
                UpdateDirectionAndSpeed();
                m_fSpeed = m_fMaxSpeed;
                m_fAngle = Mathf.MoveTowards(m_fAngle, m_fTargetAngle, m_fFastRotateSpeed * Time.fixedDeltaTime);
            }

            MovePlayer();
            break;
        case eState.kDiving:
            //Perform dive
            float percentCompleted = (Time.time - m_fDiveStartTime) / m_fDiveTime;
            transform.position = Vector3.Lerp(m_vDiveStartPos, m_vDiveEndPos, percentCompleted);

            if (percentCompleted >= 1.0f)
            {
                m_nState = eState.kRecovering;
            }

            break;
        case eState.kRecovering:
            //Wait for recovery time to pass then swtich back to slow movement
            if (Time.time >= (m_fDiveStartTime + m_fDiveTime + m_fDiveRecoveryTime))
            {
                m_nState = eState.kMoveSlow;
            }
            
            break;
        default:
            break;
        }
    }
}
