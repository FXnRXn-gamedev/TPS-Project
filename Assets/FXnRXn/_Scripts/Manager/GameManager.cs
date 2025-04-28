using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FXnRXn
{
	public class GameManager : MonoBehaviour
	{
		[Header("GAME SETTING : ")] 
		[Range(0, 300)][SerializeField] private int						targetFrameRate = 300;
		
		void Start()
		{
			
			Application.targetFrameRate = targetFrameRate;
			QualitySettings.vSyncCount = 0;
		}
		
	}
}

