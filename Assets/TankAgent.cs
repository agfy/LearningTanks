using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

[RequireComponent(typeof(Shooting))]
[RequireComponent(typeof(Movement))]

public class TankAgent : Agent
{
    protected Shooting m_Shooting;
    protected Movement m_Movement;
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
        m_Movement = GetComponent<Movement>();
        Monitor.verticalOffset = 1f;
        myArea = area.GetComponent<TankArea>();
        rayPer = GetComponent<RayPerception>();
        myAcademy = myAcademyObj.GetComponent<TankAcademy>();
        agentRB = GetComponent<Rigidbody>();
    }

    public override void CollectObservations()
    {
        float rayDistance = 50f;
        float[] rayAngles = { 20f, 45f, 70f, 90f, 110f, 135f, 160f, 200f, 225f, 250f, 270f, 290f, 315f, 340f };
        string[] detectableObjects = { "Crate", "Agent" };
        AddVectorObs(rayPer.Perceive(rayDistance, rayAngles, detectableObjects, 0f, 0f));
        Vector3 localVelocity = transform.InverseTransformDirection(agentRB.velocity);
        AddVectorObs(localVelocity.x);
        AddVectorObs(localVelocity.z);
    }

    public void MoveAgent(float[] act)
    {
        if (brain.brainParameters.vectorActionSpaceType == SpaceType.continuous)
        {
            if (Mathf.Clamp(act[1], 0f, 1f) > 0.5f)
            {
                float movementAndle = RayPerception.DegreeToRadian((Mathf.Clamp(act[0], -1f, 1f) + 1f) * 180f);
                m_Movement.SetDesiredMovementDirection(new Vector2(Mathf.Cos(movementAndle), Mathf.Sin(movementAndle)));
            }
            else
            {
                m_Movement.SetDesiredMovementDirection(new Vector2(0, 0));
            }
            
            float shootAndle = RayPerception.DegreeToRadian((Mathf.Clamp(act[2], -1f, 1f) + 1f) * 180f);
            m_Shooting.SetDesiredFirePosition(new Vector3(10f * Mathf.Cos(shootAndle) + agentRB.position.x, 0, 10f * Mathf.Sin(shootAndle) + agentRB.position.z));

            if (Mathf.Clamp(act[3], 0f, 1f) > 0.5f)
            {
                m_Shooting.SetFireIsHeld(true);
            }
            else
            {
                m_Shooting.SetFireIsHeld(false);
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
        int index = 0;
        
        bool foundFreePoint = false;
        while (!foundFreePoint)
        {
            index = Random.Range(0, myArea.spawnPoints.Count);
            
            var hitColliders = Physics.OverlapSphere(myArea.spawnPoints[index], 2);//2 is purely chosen arbitrarly
            if (hitColliders.Length <= 0)
            {
                foundFreePoint = true;
            }
        }

        transform.position = new Vector3(myArea.spawnPoints[index].x, 0f, myArea.spawnPoints[index].y);// + area.transform.position;
        transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Crate"))
        {
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
