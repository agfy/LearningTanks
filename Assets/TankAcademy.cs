using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TankAcademy : Academy
{
	[HideInInspector]
	public GameObject[] agents;
	[HideInInspector]
	public TankArea[] listArea;

	public int totalScore;
	public Text scoreText;
	public override void AcademyReset()
	{
		listArea = FindObjectsOfType<TankArea>();
		foreach (TankArea tankArea in listArea)
		{
			tankArea.ResetArea(agents);
		}

		totalScore = 0;
	}

	void ClearObjects(GameObject[] objects)
	{
		/*
		foreach (GameObject bana in objects)
		{
			Destroy(bana);
		}
		*/
	}

	public override void AcademyStep()
	{
		scoreText.text = string.Format(@"Score: {0}", totalScore);
	}
	
	public override void InitializeAcademy()
	{
		//Physics.gravity *= gravityMultiplier;
		agents = GameObject.FindGameObjectsWithTag("Agent");
	}

	public void AddRewardToPlayer(int playerId, float points)
	{
		foreach (var agent in agents)
		{
			if (agent.GetComponent<Shooting>().m_PlayerNumber == playerId)
			{
				Debug.LogFormat("Added {0} to player {1}", points, playerId);
				agent.GetComponent<TankAgent>().AddReward(points);
			}
		}
	}
}
