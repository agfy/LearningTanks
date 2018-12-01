using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalMovement : MonoBehaviour {
    public float m_Speed = 6f;                 // How fast the tank moves forward and back.
    public float m_TurnSpeed = 90f;            // How fast the tank turns in degrees per second.
   
    //private string m_MovementAxisName;          // The name of the input axis for moving forward and back.
    //private string m_TurnAxisName;              // The name of the input axis for turning.
    private Rigidbody m_Rigidbody;              // Reference used to move the tank.
    private float m_MovementInputValue;         // The current value of the movement input.
    private float m_TurnInputValue;             // The current value of the turn input.

    private void Awake ()
    {
        m_Rigidbody = GetComponent<Rigidbody> ();
    }

    public void SetMovementInputValue(float value)
    {
        m_MovementInputValue = value;
    }

    public void SetTurnInputValue(float value)
    {
        m_TurnInputValue = value;
    }

    private void FixedUpdate ()
    {
        // Adjust the rigidbodies position and orientation in FixedUpdate.
        Move ();
        Turn ();
    }


    private void Move ()
    {
        // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
        Vector3 movement = transform.forward * m_MovementInputValue * m_Speed * Time.deltaTime;

        // Apply this movement to the rigidbody's position.
        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }


    private void Turn ()
    {
        // Determine the number of degrees to be turned based on the input, speed and time between frames.
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

        // Make this into a rotation in the y axis.
        Quaternion turnRotation = Quaternion.Euler (0f, turn, 0f);

        // Apply this rotation to the rigidbody's rotation.
        m_Rigidbody.MoveRotation (m_Rigidbody.rotation * turnRotation);
    }
}
