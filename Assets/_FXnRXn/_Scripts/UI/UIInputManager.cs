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


		private bool localAim;

		#endregion

		public override void OnNetworkSpawn()
		{
			localAim = false;
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


		public void AimButtonPressed()
		{
			localAim = !localAim;
			SetAimButtonAction(localAim);

		}

		private void SetAimButtonAction(bool enable)
		{
			if (enable)
			{
				InputHandler.instance.onAimActivated?.Invoke();
			}
			else
			{
				InputHandler.instance.onAimDeactivated?.Invoke();
			}
		}
	}
}

