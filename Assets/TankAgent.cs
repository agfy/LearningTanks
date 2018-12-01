using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

[RequireComponent(typeof(Shooting))]
[RequireComponent(typeof(Movement))]

public class TankAgent : Agent
{
    protected Shooting m_Shooting;
    //protected Movement m_Movement;
    protected LocalMovement m_Movement;
    public GameObject myAcademyObj;
    TankAcademy myAcademy;
    public GameObject area;
    TankArea myArea;
    Rigidbody agentRB;
    int tanks;
    RayPerception rayPer;

    public override void InitializeAgent()
    {
        base.InitializeAgent();
        m_Shooting = GetComponent<Shooting>();
        //m_Movement = GetComponent<Movement>();
        m_Movement = GetComponent<LocalMovement>();
        Monitor.verticalOffset = 1f;
        myArea = area.GetComponent<TankArea>();
        rayPer = GetComponent<RayPerception>();
        myAcademy = myAcademyObj.GetComponent<TankAcademy>();
        agentRB = GetComponent<Rigidbody>();
    }

    public override void CollectObservations()
    {
        float rayDistance = 75f;
        //float[] rayAngles = { 20f, 90f, 160f, 45f, 135f, 70f, 110f };
        float[] rayAngles = { 10f, 20f, 30f, 40f, 50f, 60f, 70f, 80f, 90f, 100f, 110f, 120f, 130f, 140f, 150f, 160f, 170f };
        //float[] rayAngles = { 20f, 45f, 70f, 90f, 110f, 135f, 160f, 200f, 225f, 250f, 270f, 290f, 315f, 340f };
        //float[] rayAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f};
        string[] detectableObjects = { "Crate", "Agent" };
        AddVectorObs(rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f));
        Vector3 localVelocity = transform.InverseTransformDirection(agentRB.velocity);
        AddVectorObs(localVelocity.x);
        AddVectorObs(localVelocity.z);
        AddVectorObs(Mathf.Lerp(m_Shooting.m_MinLaunchForce, m_Shooting.m_MinLaunchForce, m_Shooting.m_CurrentLaunchForce));
    }

    public void MoveAgent(float[] act)
    {
        if (brain.brainParameters.vectorActionSpaceType == SpaceType.continuous)
        {
            //if (Mathf.Clamp(act[1], 0f, 1f) > 0.5f)
            //{
                //float movementAndle = RayPerception.DegreeToRadian((Mathf.Clamp(act[0], -1f, 1f) + 1f) * 180f);
                //m_Movement.SetDesiredMovementDirection(new Vector2(Mathf.Cos(movementAndle), Mathf.Sin(movementAndle)));
            //}
            //else
            //{
            //    m_Movement.SetDesiredMovementDirection(new Vector2(0, 0));
            //}
            
            m_Movement.SetMovementInputValue(Mathf.Clamp(act[0], -1f, 1f));
            m_Movement.SetTurnInputValue(Mathf.Clamp(act[1], -1f, 1f));
            
            Vector3 endPosition = transform.TransformDirection(RayPerception.PolarToCartesian(20f, Mathf.Clamp(act[2], 0f, 1f) * 180f));
            endPosition.y = 0;
            //float shootAndle = RayPerception.DegreeToRadian((Mathf.Clamp(act[2], -1f, 1f) + 1f) * 180f);
            //m_Shooting.SetDesiredFirePosition(new Vector3(10f * Mathf.Cos(shootAndle) + agentRB.position.x, 0, 10f * Mathf.Sin(shootAndle) + agentRB.position.z));
            m_Shooting.SetDesiredFirePosition(endPosition + agentRB.position);
            
            if (Mathf.Clamp(act[3], 0f, 1f) > 0.5f)
            {
                m_Shooting.SetFireIsHeld(true);
            }
            else
            {
                m_Shooting.SetFireIsHeld(false);
            }
        }
        else
        {
            switch ((int)act[0])
            {
                case 1:
                    m_Movement.SetMovementInputValue(1);
                    break;
                case 2:
                    m_Movement.SetMovementInputValue(-1);
                    break;
                case 3:
                    m_Movement.SetTurnInputValue(1);
                    break;
                case 4:
                    m_Movement.SetTurnInputValue(-1);
                    break;
                case 5:
                    m_Shooting.SetDesiredFirePosition(transform.TransformDirection(RayPerception.PolarToCartesian(20f, 0f)) + agentRB.position);
                    break;
                case 6:
                    m_Shooting.SetDesiredFirePosition(transform.TransformDirection(RayPerception.PolarToCartesian(20f, 90f)) + agentRB.position);
                    break;
                case 7:
                    m_Shooting.SetDesiredFirePosition(transform.TransformDirection(RayPerception.PolarToCartesian(20f, 180f)) + agentRB.position);
                    break; 
                case 8:
                    m_Shooting.SetFireIsHeld(true);
                    break;
                case 9:
                    m_Shooting.SetFireIsHeld(false);
                    break;
            }
        }
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        MoveAgent(vectorAction);
    }

    public override void AgentReset()
    {
        agentRB.velocity = Vector3.zero;
        
        bool foundFreePoint = false;
        Vector3 spawnPoint = new Vector3(0, myArea.transform.position.y, 0);
        while (!foundFreePoint)
        {
            spawnPoint.x = myArea.transform.position.x + Random.Range(-45, 45);
            spawnPoint.z = myArea.transform.position.z + Random.Range(-45, 45);
            var hitColliders = Physics.OverlapSphere(spawnPoint, 2);
            if (hitColliders.Length > 1)
            {
                continue;
            }
                
            foundFreePoint = true;
        }

        transform.position = spawnPoint;
        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Crate"))
        {
            Debug.LogFormat("Added -1 to player {0}", m_Shooting.m_PlayerNumber);
            AddReward(-1f);
            if (false)
            {
                myAcademy.totalScore -= 1;
            }
        }
    }

    public override void AgentOnDone()
    {

    }
}
