using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace FXnRXn
{
	[RequireComponent(typeof(NetworkObject))]
	public class GameManager : NetworkBehaviour
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

