using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    public Player m_player;
    public enum eState : int
    {
        kIdle,
        kHopStart,
        kHop,
        kCaught,
        kNumStates
    }

    private Color[] stateColors = new Color[(int)eState.kNumStates]
   {
        new Color(255, 0,   0),
        new Color(0,   255, 0),
        new Color(0,   0,   255),
        new Color(255, 255, 255)
   };

    private struct Intersection 
    {
        public bool isIntersection;
        public float constant;
        public float offset;

        public Intersection(bool isIntersection, float constant, float offset) : this()
        {
            this.isIntersection = isIntersection;
            this.constant = constant;
            this.offset = offset;
        }
    };

    //External tunables.
    public float m_fHopTime = 0.2f;
    public float m_fHopSpeed = 6.5f;
    public float m_fScaredDistance = 3.0f;
    public int m_nMaxMoveAttempts = 50;
    public float m_fBorderOffset = 0.5f;
    public float m_fCorneredRandomness = 0.25f;

    //Internal variables.
    public eState m_nState;
    public float m_fHopStart;
    public Vector3 m_vHopStartPos;
    public Vector3 m_vHopEndPos;
    public Vector2 m_vScreenSize;

    void Start()
    {
        //Setup the initial state and get the player GO.
        m_nState = eState.kIdle;
        m_player = GameObject.FindFirstObjectByType(typeof(Player)) as Player;
        m_vScreenSize = Camera.main.ScreenToWorldPoint(new Vector2(Screen.width, Screen.height));

        //Offset to keep target more visible
        m_vScreenSize.x -= m_fBorderOffset;
        m_vScreenSize.y -= m_fBorderOffset;
    }

    bool IsValidHop(Vector3 hopEndPos)
    {
        if (hopEndPos.x > m_vScreenSize.x || hopEndPos.y > m_vScreenSize.y 
        || hopEndPos.x < -m_vScreenSize.x || hopEndPos.y < -m_vScreenSize.y)
        {
            return false;
        }
        else
            return true;
    }

    Intersection CalculateIntersectionOffset(float position, float screenSize)
    {
        float constant = 0f;
        bool closeToWall = false;

        if (position < (-screenSize + (m_fHopSpeed * m_fHopTime)))
        {
            constant = -screenSize;
            closeToWall = true;
        }
        else if (position > (screenSize - (m_fHopSpeed * m_fHopTime)))
        {
            constant = screenSize;
            closeToWall = true;
        }

        if (closeToWall)
        {
            return new Intersection(closeToWall, constant, Mathf.Sqrt(Mathf.Pow(m_fHopSpeed * m_fHopTime, 2) - Mathf.Pow(position - constant, 2)));
        }

        return new Intersection(false, 0f, 0f);
    }

    //Returns angle between two vectors with range [180, 540]
    float GetAngle(Vector3 source, Vector3 target)
    {
        Vector2 direction = (target - source).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        //Add 360 to create positive only angles
        return angle + 360f;
    }

    void DetermineHop()
    {
        m_vHopStartPos = transform.position;
        m_fHopStart = Time.time;

        //Try to move directly opposite to the player
        Vector3 playerDirection = (transform.position - m_player.transform.position).normalized;
        m_vHopEndPos = playerDirection * m_fHopSpeed * m_fHopTime + m_vHopStartPos;

        //Verify movement
        if (IsValidHop(m_vHopEndPos))
        {
            return;
        }
        
        m_vHopEndPos = transform.position;

        //If not possible, then determine what intersections are made with the walls and our hop radius
        //Intersections on the x axis are: (c, y_0 +- sqrt(r^2 - (x_0 - c)^2))
        //Intersections on the y axis are: (x_0 +- sqrt(r^2 - (y_0 - c)^2), c)
        Vector3[] intersections = new Vector3[4];
        
        Intersection xAxisOffset = CalculateIntersectionOffset(transform.position.x, m_vScreenSize.x);
        Intersection yAxisOffset = CalculateIntersectionOffset(transform.position.y, m_vScreenSize.y);

        if (xAxisOffset.isIntersection)
        {
            intersections[0] = new Vector3(xAxisOffset.constant, transform.position.y + xAxisOffset.offset, 0f);
            intersections[1] = new Vector3(xAxisOffset.constant, transform.position.y - xAxisOffset.offset, 0f);
        }

        if (yAxisOffset.isIntersection)
        {
            intersections[2] = new Vector3(transform.position.x + yAxisOffset.offset, yAxisOffset.constant, 0f);
            intersections[3] = new Vector3(transform.position.x - yAxisOffset.offset, yAxisOffset.constant, 0f);
        }

        //From there, find the furtherest intersection from player, thats valid, to hop towards
        Vector3 chosen = Vector3.zero;
        float maxDistance = 0f;

        foreach (Vector3 vector in intersections)
        {
            if (vector == Vector3.zero || !IsValidHop(vector))
            {
                continue;
            }

            float distance = Vector3.Distance(vector, m_player.transform.position);

            if (distance > maxDistance)
            {
                maxDistance = distance;
                chosen = vector;
            }
        }

        //Shouldn't be possible, at minimum there will always be 2 valid positions
        if (chosen == Vector3.zero)
        {
            Debug.LogError("Couldn't find valid path!");
        }

        m_vHopEndPos = chosen;

        //Add some randomness by interpolating between the angle of chosen vector and the angle of center
        float angle = GetAngle(m_vHopStartPos, m_vHopEndPos);
        float zeroAngle = GetAngle(m_vHopStartPos, Vector3.zero);
        angle = Mathf.Lerp(angle, zeroAngle, m_fCorneredRandomness);
        
        //Incorporate randomly chosen direction
        float x = Mathf.Cos(angle * Mathf.Deg2Rad);
        float y = Mathf.Sin(angle * Mathf.Deg2Rad);
        chosen = new Vector3(x, y, 0f);

        //Add magnitude of chosen hop
        Vector3 movement = m_vHopEndPos - m_vHopStartPos;
        chosen *= movement.magnitude;

        //Add start position to make this an end position
        chosen += m_vHopStartPos;

        //Not all random directions will be valid due to the range of angles wanted in each unique position
        //If not valid, fall back to desired path
        if (!IsValidHop(chosen))
        {
            return;
        }

        m_vHopEndPos = chosen;
    }

    void FixedUpdate()
    {
        GetComponent<Renderer>().material.color = stateColors[(int)m_nState];

        switch(m_nState)
        {
        case eState.kIdle:
            //Get distance from player and begin hop if necessary
            if (Vector3.Distance(transform.position, m_player.transform.position) <= m_fScaredDistance)
            {
                m_nState = eState.kHopStart;
            }

            break;
        case eState.kHopStart:
            //Determine where to hop
            DetermineHop();

            //Face in hop direction
            Vector2 direction = (m_vHopEndPos - m_vHopStartPos).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle - 90f));

            m_nState = eState.kHop;
            break;
        case eState.kHop:
            //Perform hop
            float percentCompleted = (Time.time - m_fHopStart) / m_fHopTime;
            transform.position = Vector3.Lerp(m_vHopStartPos, m_vHopEndPos, percentCompleted);

            if (percentCompleted >= 1.0f)
            {
                m_nState = eState.kIdle;
            }

            break;
        case eState.kCaught:
            //Attached to player, nothing to do
            break;
        default:
            break;
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        // Check if this is the player (in this situation it should be!)
        if (collision.gameObject == GameObject.Find("Player"))
        {
            // If the player is diving, it's a catch!
            if (m_player.IsDiving())
            {
                m_nState = eState.kCaught;
                transform.parent = m_player.transform;
                transform.localPosition = new Vector3(0.3f, 0.3f, 0.0f);
            }
        }
    }
}