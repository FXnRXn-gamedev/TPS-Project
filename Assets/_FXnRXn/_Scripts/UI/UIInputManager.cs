using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace FXnRXn
{
	[RequireComponent(typeof(NetworkObject))]
	public class UIInputManager : NetworkBehaviour
	{
		#region Variables

		[Header("Player Action Button :")] 
		[Space(10)] 
		[SerializeField] private Button							jumpButton;

		#endregion

		public override void OnNetworkSpawn()
		{
			
		}

		private void Start()
		{
			if (jumpButton != null)
			{
				jumpButton?.onClick.RemoveAllListeners();
				jumpButton?.onClick.AddListener(() =>
				{
					InputHandler.instance.OnJump();
				});
			}
			
		}
	}
}

