using System;
using Unity.Netcode;
using UnityEngine;

namespace FXnRXn
{
	[RequireComponent(typeof(NetworkObject))]
	public class InputHandler : NetworkBehaviour
	{
		public static InputHandler instance { get; private set; }
		
		private bool _isRunning;

		public bool IsRunning
		{
			get => _isRunning;
			set => _isRunning = value;
		}
		
		
		[Header("REFFERENCE :")] 
		[SerializeField] private Joystick						inputMoveJoystick;
		[SerializeField] private Joystick						inputLookJoystick;

		[Header("SETTINGS :")] 
		[SerializeField] private Vector2						moveComposite;
		[SerializeField] private Vector2						lookInput;
		[SerializeField] private bool							movementInputDetected;
		public float											movementInputDuration;
		
		
		
		public Action											onJumpPerformed;
		public Action											onAimActivated;
		public Action											onAimDeactivated;

		//--------------------------------------------------------------------------------------------------------------
		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
		}

		private void Update()
		{
			SetupInputControllerMovement();
			SetupInputCameraMovement();
		}


		private void SetupInputControllerMovement()
		{
			if (inputMoveJoystick == null) return;
			

			if (new Vector2(inputMoveJoystick.Horizontal, inputMoveJoystick.Vertical).magnitude > 0)
			{
				movementInputDetected = true;
				moveComposite.x = inputMoveJoystick.Horizontal;
				moveComposite.y = inputMoveJoystick.Vertical;
			}
			else
			{
				movementInputDetected = false;
				moveComposite = Vector2.zero;
			}
		}

		private void SetupInputCameraMovement()
		{
			if (inputLookJoystick == null) return;
			
			lookInput = inputLookJoystick.Direction;

		}
		
		
		public void OnJump()
		{
			onJumpPerformed?.Invoke();
		}


		public Vector2 GetMovementInput() => moveComposite;
		public Vector2 GetLookInput() => lookInput;
		public bool GetMovementInputDetected() => movementInputDetected;
	}
}

