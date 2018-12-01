using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankArea : MonoBehaviour
{
	private void Awake()
	{
		
	}

	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{

	}

	public void ResetBananaArea(GameObject[] agents)
	{
		foreach (GameObject agent in agents)
		{
			
			/*
			if (agent.transform.parent == gameObject.transform)
			{
				agent.transform.position = new Vector3(Random.Range(-range, range), 2f,
					                           Random.Range(-range, range))
				                           + transform.position;
				agent.transform.rotation = Quaternion.Euler(new Vector3(0f, Random.Range(0, 360)));
			}
			*/
		}
	}

	public void ResetArea(GameObject[] agents)
	{
		foreach (GameObject agent in agents)
		{
			agent.GetComponent<Health>().SetTankActive(true);
		}
	}
}
