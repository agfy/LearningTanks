using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
//using Tanks.Data;
using Tanks.CameraControl;
using Tanks.Shells;
using Tanks.Explosions;
using Random = UnityEngine.Random;


public class Shooting : MonoBehaviour
{
	// Unique playerID used to identify this tank.
	[SerializeField]
	public int m_PlayerNumber = 1;

	// Prefab of the default shell.
	[SerializeField]
	protected Rigidbody m_Shell;

	// A child of the tank where the shells are spawned.
	[SerializeField]
	protected Transform m_FireTransform;

	// A child of the tank that is oriented towards fire direction
	[SerializeField]
	protected Transform m_TurretTransform;

	// A child of the tank that displays the current launch force.
	[SerializeField]
	protected Slider m_AimSlider;

	// The transform that contains the aim slider
	[SerializeField]
	protected Transform m_AimSliderParent;

	[SerializeField]
	protected float m_LookDirTickInterval;

	[SerializeField]
	protected float m_LookDirInterpolate;

	[SerializeField]
	protected float m_ShootShakeMinMagnitude;

	[SerializeField]
	protected float m_ShootShakeMaxMagnitude;

	[SerializeField]
	protected float m_ShootShakeDuration;

	[SerializeField]
	protected ExplosionSettings m_FiringExplosion;

	[SerializeField]
	protected float m_ChargeShakeMagnitude;

	[SerializeField]
	protected float m_ChargeShakeNoiseScale;

	[SerializeField]
	protected float m_FireRecoilMagnitude = 0.25f;

	[SerializeField]
	protected float m_FireRecoilSpeed = 4f;

	[SerializeField]
	protected AnimationCurve m_FireRecoilCurve;
	public GameObject myAcademyObj;
	TankAcademy myAcademy;

	private Vector3 m_DefaultTurretPos;
	private Vector2 m_RecoilDirection;
	private float m_RecoilTime;

	//Variables to set and keep track of a minimum safety distance within which the tank will not fire a shell.
	[SerializeField]
	protected float m_MinimumSafetyRange = 4f;
	private float m_SqrMinimumSafetyRange;
	private float m_SqrTargetRange = 0f;

	// The high angle for shots
	[SerializeField]
	//protected float m_MaxLaunchAngle = 70f;
	public float m_MaxLaunchForce = 45f;
	
	// The long angle for shots
	[SerializeField]
	//protected float m_MinLaunchAngle = 20f;
	public float m_MinLaunchForce = 15f;
	
	// How long the shell can charge for before it is fired at max force.
	[SerializeField]
	protected float m_MaxChargeTime = 0.75f;
	
	//my shit
	protected float m_LaunchAngle = 30f;

	//The rate of fire for this tank.
	[SerializeField]
	protected float m_RefireRate;

	// The force that will be given to the shell when the fire button is released.
	//private float m_CurrentLaunchAngle;
	public float m_CurrentLaunchForce;

	// How fast the launch force increases, based on the max charge time.
	private float m_ChargeSpeed;

	// Whether or not the shell has been launched with this button press.
	private bool m_Fired;

	// The turret's facing direction in degrees
	private float m_TurretHeading;

	// Client-side interpolation for any but the local player
	private float m_ClientTurretHeading;
	private float m_ClientTurretHeadingVel;

	//The point that we want to fire to.
	private Vector3 m_TargetFirePosition;

	//Whether the input manager has flagged firing, and whether this was the case last tick.
	private bool m_FireInput;
	private bool m_WasFireInput;

	//Internal tracking for reload time.
	private float m_ReloadTime;

	//Last time the look update was ticked.
	private float m_LastLookUpdate;

	//Public field allowing external scripts to block the ability to fire.
	public bool canShoot
	{
		get;
		set;
	}

	//Update the turret's orientation based on a set vector from the input manager.
	public void SetLookDirection(Vector2 target)
	{
		// Subtract from 90 to correct for Atan2 being anti-clockwise from right
		m_TurretHeading = 90 - Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;

		//If we've ticked for a rotation update (to avoid continual traffic), fire the server command to set rotation and update the update tick timer.
		if (Time.realtimeSinceStartup - m_LastLookUpdate >= m_LookDirTickInterval)
		{
			CmdSetLook(m_TurretHeading);

			m_LastLookUpdate = Time.realtimeSinceStartup;
		}

		//Set the local turret transform to our new value.
		m_TurretTransform.rotation = Quaternion.AngleAxis(m_TurretHeading, Vector3.up);
	}

	//Calculate the firing position, and perform turret rotation logic to match the new orientation.
	public void SetDesiredFirePosition(Vector3 target)
	{
		m_TargetFirePosition = target;

		// Reorient turret
		Vector3 toAimPos = m_TargetFirePosition - transform.position;
		// Subtract from 90 to correct for Atan2 being anti-clockwise from right
		m_TurretHeading = 90 - Mathf.Atan2(toAimPos.z, toAimPos.x) * Mathf.Rad2Deg;

		if (Time.realtimeSinceStartup - m_LastLookUpdate >= m_LookDirTickInterval)
		{
			CmdSetLook(m_TurretHeading);

			m_LastLookUpdate = Time.realtimeSinceStartup;
		}
		m_TurretTransform.rotation = Quaternion.AngleAxis(m_TurretHeading, Vector3.up);

		//Determine square distance to desired target for range checks later.
		m_SqrTargetRange = toAimPos.sqrMagnitude;
	}

	//Set by input manager to indicate that fire input has been made.
	public void SetFireIsHeld(bool fireHeld)
	{
		m_FireInput = fireHeld;
	}

	private void Awake()
	{
		Init();
		m_LastLookUpdate = Time.realtimeSinceStartup;
		myAcademy = myAcademyObj.GetComponent<TankAcademy>();
	}

	private void Start()
	{
		// The rate that the launch force charges up is the range of possible forces by the max charge time.
		//m_ChargeSpeed = (m_MaxLaunchAngle - m_MinLaunchAngle) / m_MaxChargeTime;
		m_ChargeSpeed = (m_MaxLaunchForce - m_MinLaunchForce) / m_MaxChargeTime;

		//The square of our minimum firing point safety range, for efficient distance comparison.
		m_SqrMinimumSafetyRange = Mathf.Pow(m_MinimumSafetyRange, 2f);
	}

	private void OnDisable()
	{
		SetDefaults();
	}

	private void Update()
	{
/*
		if (!m_Initialized)
		{
			return;
		}

		if (!hasAuthority)
		{
			// Remote players interpolate their facing direction
			if (m_TurretTransform != null)
			{
				m_ClientTurretHeading = Mathf.SmoothDampAngle(m_ClientTurretHeading, m_TurretHeading, ref m_ClientTurretHeadingVel, m_LookDirInterpolate);
				m_TurretTransform.rotation = Quaternion.AngleAxis(m_ClientTurretHeading, Vector3.up);
			}
			return;
		}
*/
		// Reload time
		if (m_ReloadTime > 0)
		{
			m_ReloadTime -= Time.deltaTime;
		}
		
		//If the fire button has been released with the target point inside the tank's safety radius, we fire the hooter instead of continuing with fire logic.
/*
		if (m_FireInput && !m_WasFireInput && InSafetyRange())
		{

		}
*/
		// Otherwise, if the min angle has been exceeded and the shell hasn't yet been launched...
		//else if (m_CurrentLaunchAngle <= m_MinLaunchAngle && !m_Fired)
		//else if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
		if (m_CurrentLaunchForce >= m_MaxLaunchForce && !m_Fired)
		{
			// ... use the max force and launch the shell.
			//m_CurrentLaunchAngle = m_MinLaunchAngle;
			m_CurrentLaunchForce = m_MaxLaunchForce;
			Fire();
		}
		// Otherwise, if the fire button has just started being pressed...
		else if (m_FireInput && !m_WasFireInput && CanFire())
		{
			// ... reset the fired flag and reset the launch force.
			m_Fired = false;

			//m_CurrentLaunchAngle = m_MaxLaunchAngle;
			m_CurrentLaunchForce = m_MinLaunchForce;
		}
		// Otherwise, if the fire button is being held and the shell hasn't been launched yet...
		else if (m_FireInput && !m_Fired)
		{
			// Increment the launch force and update the slider.
			//m_CurrentLaunchAngle -= m_ChargeSpeed * Time.deltaTime;
			m_CurrentLaunchForce += m_ChargeSpeed * Time.deltaTime;
		}
		// Otherwise, if the fire button is released and the shell hasn't been launched yet...
		else if (!m_FireInput && m_WasFireInput && !m_Fired)
		{
			// ... launch the shell.
			Fire();
		}

		m_WasFireInput = m_FireInput;

		UpdateAimSlider();

		// Turret shake
		//float shakeMagnitude = Mathf.Lerp(0, m_ChargeShakeMagnitude, Mathf.InverseLerp(m_MaxLaunchAngle, m_MinLaunchAngle, m_CurrentLaunchAngle));
		float shakeMagnitude = Mathf.Lerp(0, m_ChargeShakeMagnitude, Mathf.Lerp(m_MaxLaunchForce, m_MinLaunchForce, m_CurrentLaunchForce));
		Vector2 shakeOffset = Vector2.zero;

		if (shakeMagnitude > 0)
		{
			shakeOffset.x = (Mathf.PerlinNoise((Time.realtimeSinceStartup + 0) * m_ChargeShakeNoiseScale, Time.smoothDeltaTime) * 2 - 1) * shakeMagnitude;
			shakeOffset.y = (Mathf.PerlinNoise((Time.realtimeSinceStartup + 100) * m_ChargeShakeNoiseScale, Time.smoothDeltaTime) * 2 - 1) * shakeMagnitude;
		}

		if (m_RecoilTime > 0)
		{
			m_RecoilTime = Mathf.Clamp01(m_RecoilTime - Time.deltaTime * m_FireRecoilSpeed);
			float recoilPoint = m_FireRecoilCurve.Evaluate(1 - m_RecoilTime);

			shakeOffset += m_RecoilDirection * recoilPoint * m_FireRecoilMagnitude;
		}

		m_TurretTransform.localPosition = m_DefaultTurretPos + new Vector3(shakeOffset.x, 0, shakeOffset.y);
	}
		
	// Can shoot if the refire time is depleted and shooting has not been overridden externally.
	private bool CanFire()
	{
		return (m_ReloadTime <= 0 && canShoot);
	}
		
	// Returns whether the current targeting point is within the tank's no-fire safety range.
	private bool InSafetyRange()
	{
		return (m_SqrTargetRange <= m_SqrMinimumSafetyRange);
	}

	public static Vector3 CalculateFireVector(Shell shellToFire, Vector3 targetFirePosition, Vector3 firePosition, 
		float currentLaunchForce, float launchAngle)
	{
		Vector3 target = targetFirePosition;
		target.y = firePosition.y;
		Vector3 toTarget = target - firePosition;
		//float targetDistance = toTarget.magnitude;
		//float shootingAngle = launchAngle;
		//float grav = Mathf.Abs(Physics.gravity.y);
		//grav *= shellToFire != null ? shellToFire.speedModifier : 1;
		//float relativeY = firePosition.y - targetFirePosition.y;

		//float theta = Mathf.Deg2Rad * shootingAngle;
		//float cosTheta = Mathf.Cos(theta);
		//float num = targetDistance * Mathf.Sqrt(grav) * Mathf.Sqrt(1 / cosTheta);
		//float denom = Mathf.Sqrt(2 * targetDistance * Mathf.Sin(theta) + 2 * relativeY * cosTheta);
		//float v = num / denom;
		Vector3 aimVector = toTarget;// / targetDistance;
		
		aimVector /= aimVector.magnitude;
		aimVector.y = 0.3f;
		
		//aimVector.y = 0;
		/*
		Vector3 rotAxis = Vector3.Cross(aimVector, Vector3.up);
		Quaternion rotation = Quaternion.AngleAxis(shootingAngle, rotAxis);
*/
		//aimVector = rotation * aimVector.normalized;

		return aimVector * currentLaunchForce;
	}

	private void Fire()
	{
		// Set the fired flag so only Fire is only called once.
		m_Fired = true;

		//Determine which shell we should fire.
		Shell shellToFire = GetShellType().GetComponent<Shell>();

		//Determine our firing solution based on our target location and power.
		
		Vector3 fireVector = CalculateFireVector(shellToFire, m_TargetFirePosition, m_FireTransform.position, m_CurrentLaunchForce, m_LaunchAngle);
		//Vector3 direction = m_TargetFirePosition - m_FireTransform.position;
		//Vector3 fireVector = m_CurrentLaunchForce * direction / direction.magnitude;
		
		// Get a random seed to associate with projectile on all clients.
		// This is specifically used for the cluster bomb and any debris spawns, to ensure that their
		// random velocities are identical
		int randSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

		// Immediately fire shell on client - this provides players with the necessary feedback they want
		FireVisualClientShell(fireVector, m_FireTransform.position, randSeed);
		myAcademy.AddRewardToPlayer(m_PlayerNumber, 1f);

		// Reset the launch force.  This is a precaution in case of missing button events.
		//m_CurrentLaunchAngle = m_MaxLaunchAngle;
		m_CurrentLaunchForce = m_MinLaunchForce;

		m_ReloadTime = m_RefireRate;

		// Small screenshake on client
		if (ScreenShakeController.s_InstanceExists)
		{
			ScreenShakeController shaker = ScreenShakeController.s_Instance;

			//float chargeAmount = Mathf.InverseLerp(m_MaxLaunchAngle, m_MinLaunchAngle, m_CurrentLaunchAngle);
			float chargeAmount = Mathf.InverseLerp(m_MaxLaunchForce, m_MinLaunchForce, m_CurrentLaunchForce);
			float magnitude = Mathf.Lerp(m_ShootShakeMinMagnitude, m_ShootShakeMaxMagnitude, chargeAmount);
			// Scale magnitude 
			shaker.DoShake(m_TargetFirePosition, magnitude, m_ShootShakeDuration);
		}

		m_RecoilTime = 1;
		Vector3 localVector = transform.InverseTransformVector(fireVector);
		m_RecoilDirection = new Vector2(-localVector.x, -localVector.z);
	}

	//Server-side command to propogate turret facing to all clients.
	private void CmdSetLook(float turretHeading)
	{
		m_TurretHeading = turretHeading;
	}

	//This method takes care of all the aesthetic elements of firing - instantiating a shell prefab and playing all visual and audio effects.
	private Shell FireVisualClientShell(Vector3 shotVector, Vector3 position, int randSeed)
	{
		// Create explosion for muzzle flash
		if (Explosion.s_InstanceExists)
		{
			Explosion.s_Instance.SpawnExplosion(position, shotVector, null, m_PlayerNumber, m_FiringExplosion, true);
		}

		// Create an instance of the shell and store a reference to its rigidbody.
		Rigidbody shellInstance = Instantiate<Rigidbody>(GetShellType());

		// Set the shell's velocity and position
		shellInstance.transform.position = position;
		shellInstance.velocity = shotVector;

		Shell shell = shellInstance.GetComponent<Shell>();
		shell.Setup(m_PlayerNumber, null, randSeed);

		//Ensure that the shell does not collide with this tank, which fired it.
		Physics.IgnoreCollision(shell.GetComponent<Collider>(), GetComponentInChildren<Collider>(), true);

		return shell;
	}

	private Rigidbody GetShellType()
	{
		//We return the standard shell populated in this controller by default.
		Rigidbody shellType = m_Shell;

		return shellType;
	}

	public void Init()
	{
		enabled = false;
		canShoot = false;

		// Reparent aim slider
		m_AimSliderParent.SetParent(m_TurretTransform, false);
		m_DefaultTurretPos = m_TurretTransform.localPosition;

		SetDefaults();
	}

	//Updates the fill value of the aim charge arrow graphic.
	private void UpdateAimSlider()
	{
		//float aimValue = m_Fired ? m_MaxLaunchAngle : m_CurrentLaunchAngle;
		//m_AimSlider.value = m_MaxLaunchAngle - aimValue + m_MinLaunchAngle;

		m_AimSlider.value = m_CurrentLaunchForce;
	}

	// This is used by the game manager to reset the tank.
	public void SetDefaults()
	{
		enabled = true;
		canShoot = true;
		//m_CurrentLaunchAngle = m_MaxLaunchAngle;
		m_CurrentLaunchForce = m_MinLaunchForce;
		UpdateAimSlider();
		m_FireInput = m_WasFireInput = false;
		m_Fired = false;
	}
}
