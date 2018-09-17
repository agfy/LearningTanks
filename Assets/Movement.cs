using UnityEngine;
using UnityEngine.Networking;
using System;
using Random = UnityEngine.Random;
using Tanks.CameraControl;

//This class is responsible for the movement of the tank and related animation/audio.
public class Movement : MonoBehaviour
{
	//Enum to define how the tank is moving towards its desired direction.
	public enum MovementMode
	{
		Forward = 1,
		Backward = -1
	}

	// How fast the tank moves forward and back. We sync this stat from server to prevent local cheatery.
	private float m_Speed = 12f;

	public float speed
	{
		get
		{
			return m_Speed;
		}
	}

	// How fast the tank turns in degrees per second. We sync this stat from server to prevent local cheatery.
	private float m_TurnSpeed = 180f;

	// Reference used to move the tank.
	private Rigidbody m_Rigidbody;

	public Rigidbody Rigidbody
	{
		get
		{
			return m_Rigidbody;
		}
	}

	//The direction that the player wants to move in.
	private Vector2 m_DesiredDirection;

	//The tank's position last tick.
	private Vector3 m_LastPosition;

	private MovementMode m_CurrentMovementMode;

	public MovementMode currentMovementMode
	{
		get
		{
			return m_CurrentMovementMode;
		}
	}

	//Whether the tank was undergoing movement input last tick.
	private bool m_HadMovementInput;

	//The final velocity of the tank.
	public Vector3 velocity
	{
		get;
		protected set;
	}

	//Whether the tank is moving.
	public bool isMoving
	{
		get
		{
			return m_DesiredDirection.sqrMagnitude > 0.01f;
		}
	}

	//Called by the active tank input manager to set the movement direction of the tank.
	public void SetDesiredMovementDirection(Vector2 moveDir)
	{
		m_DesiredDirection = moveDir;
		m_HadMovementInput = true;

		if (m_DesiredDirection.sqrMagnitude > 1)
		{
			m_DesiredDirection.Normalize();
		}
	}

	private void Awake()
	{
		SetDefaults();
		//Get our rigidbody, and init originalconstraints for enable/disable code.
		LazyLoadRigidBody();
		m_OriginalConstrains = m_Rigidbody.constraints;

		m_CurrentMovementMode = MovementMode.Forward;
	}

	private void LazyLoadRigidBody()
	{
		if (m_Rigidbody != null)
		{
			return;
		}

		m_Rigidbody = GetComponent<Rigidbody>();
	}


	private void Start()
	{
		m_LastPosition = transform.position;
	}

	private void Update()
	{
		if (!m_HadMovementInput || !isMoving)
		{
			m_DesiredDirection = Vector2.zero;
		}

		m_HadMovementInput = false;	
	}

	private void FixedUpdate()
	{
		velocity = transform.position - m_LastPosition;
		m_LastPosition = transform.position;

		// Adjust the rigidbody's position and orientation in FixedUpdate.
		if (isMoving)
		{
			Turn();
			Move();
		}
	}


	private void Move()
	{
		float moveDistance = m_DesiredDirection.magnitude * m_Speed * Time.deltaTime;

		// Create a movement vector based on the input, speed and the time between frames, in the direction the tank is facing.
		Vector3 movement = m_CurrentMovementMode == MovementMode.Backward ? -transform.forward : transform.forward;
		movement *= moveDistance;

		// Apply this movement to the rigidbody's position.
		// Also immediately move our transform so that attached joints update this frame
		m_Rigidbody.position = m_Rigidbody.position + movement;
		transform.position = m_Rigidbody.position;
	}


	private void Turn()
	{
		// Determine turn direction
		float desiredAngle = 90 - Mathf.Atan2(m_DesiredDirection.y, m_DesiredDirection.x) * Mathf.Rad2Deg;

		// Check whether it's shorter to move backwards here
		Vector2 facing = new Vector2(transform.forward.x, transform.forward.z);
		float facingDot = Vector2.Dot(facing, m_DesiredDirection);

		// Only change if the desired direction is a significant change over our current one
		if (m_CurrentMovementMode == MovementMode.Forward &&
			facingDot < -0.5)
		{
			m_CurrentMovementMode = MovementMode.Backward;
		}
		if (m_CurrentMovementMode == MovementMode.Backward &&
			facingDot > 0.5)
		{
			m_CurrentMovementMode = MovementMode.Forward;
		}
		// currentMovementMode =  >= 0 ? MovementMode.Forward : MovementMode.Backward;

		if (m_CurrentMovementMode == MovementMode.Backward)
		{
			desiredAngle += 180;
		}

		// Determine the number of degrees to be turned based on the input, speed and time between frames.
		float turn = m_TurnSpeed * Time.deltaTime;

		// Make this into a rotation in the y axis.
		Quaternion desiredRotation = Quaternion.Euler(0f, desiredAngle, 0f);

		// Approach that direction
		// Also immediately turn our transform so that attached joints update this frame
		m_Rigidbody.rotation = Quaternion.RotateTowards(m_Rigidbody.rotation, desiredRotation, turn);
		transform.rotation = m_Rigidbody.rotation;
	}


	// This function is called at the start of each round to make sure each tank is set up correctly.
	public void SetDefaults()
	{
		enabled = true;
		LazyLoadRigidBody();

		m_Rigidbody.velocity = Vector3.zero;
		m_Rigidbody.angularVelocity = Vector3.zero;

		m_DesiredDirection = Vector2.zero;
		m_CurrentMovementMode = MovementMode.Forward;
	}

	//We freeze the rigibody when the control is disabled to avoid the tank drifting!
	protected RigidbodyConstraints m_OriginalConstrains;

	//On disable, lock our rigidbody in position.
	void OnDisable()
	{
		m_OriginalConstrains = m_Rigidbody.constraints;
		m_Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
	}

	//On enable, restore our rigidbody's range of movement.
	void OnEnable()
	{
		//m_Rigidbody.constraints = m_OriginalConstrains;
	}
}
