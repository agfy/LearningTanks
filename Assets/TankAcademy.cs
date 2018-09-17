using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TankAcademy : Academy
{
	[HideInInspector]
	public GameObject[] agents;
	[HideInInspector]
	public BananaArea[] listArea;

	public int totalScore;
	public Text scoreText;
	public override void AcademyReset()
	{
		agents = GameObject.FindGameObjectsWithTag("Agent");
		listArea = FindObjectsOfType<BananaArea>();
		foreach (BananaArea ba in listArea)
		{
			ba.ResetBananaArea(agents);
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
	}
}
