using System;
using UnityEngine;

namespace FXnRXn
{
	public class InputHandler : MonoBehaviour
	{
		public static InputHandler instance { get; private set; }
		
		
		[Header("REFFERENCE :")] 
		[SerializeField] private Joystick						inputJoystick;
		[SerializeField] private FixedTouchField				touchField;

		[Header("SETTINGS :")] 
		[SerializeField] private Vector2						moveComposite;
		[SerializeField] private Vector2						lookInput;
		[SerializeField] private bool							movementInputDetected;
		public float											movementInputDuration;

		//--------------------------------------------------------------------------------------------------------------
		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			
			if (inputJoystick == null)
			{
				inputJoystick = FindFirstObjectByType<Joystick>();
			}

			if (touchField == null)
			{
				touchField = FindFirstObjectByType<FixedTouchField>();
			}
		}

		private void Update()
		{
			SetupControllerMovement();
			SetupCameraMovement();
		}


		private void SetupControllerMovement()
		{
			if (inputJoystick == null) return;
			

			if (new Vector2(inputJoystick.Horizontal, inputJoystick.Vertical).magnitude > 0)
			{
				movementInputDetected = true;
				moveComposite.x = inputJoystick.Horizontal;
				moveComposite.y = inputJoystick.Vertical;
			}
			else
			{
				movementInputDetected = false;
				moveComposite = Vector2.zero;
			}
		}

		private void SetupCameraMovement()
		{
			if (touchField == null) return;
			
			lookInput = touchField.TouchDist;

		}


		public Vector2 GetMovementInput() => moveComposite;
		public Vector2 GetLookInput() => lookInput;
		public bool GetMovementInputDetected() => movementInputDetected;
	}
}

