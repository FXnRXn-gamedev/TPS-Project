using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace FXnRXn
{
	public class PlayerMovementController : MonoBehaviour
	{
		public static PlayerMovementController instance { get; private set; }
		
		#region Enum
		private enum AnimationState
		{
			Base,
			Locomotion,
			Jump,
			Fall,
			Crouch
		}
		
		private enum MovementState
		{
			Idle,
			Walk,
			Run,
			Sprint
		}
		
		#endregion

		#region Animation Variable Hashes

		

		#endregion

		#region Player Settings Variables

		[Header("Refference :")] 
		[Space(10)] 
		[SerializeField]private CameraController					cameraController;
		[SerializeField] private InputHandler						inputHandler;
		[SerializeField] private Animator							playerAnim;
		[SerializeField] private CharacterController				controller;

		[Header("Game Settings :")]
		[Space(10)]
		[SerializeField] private bool								alwaysStrafe				= true;
		[SerializeField] private float								walkSpeed					= 1.4f;
		[SerializeField] private float								runSpeed					= 2.5f;
		[SerializeField] private float								sprintSpeed					= 7f;
		[SerializeField] private float								speedChangeDamping			= 10f;
		[SerializeField] private float								rotationSmoothing			= 10f;
		[SerializeField] private float								cameraRotationOffset;

		[Space(10)]
		[SerializeField] private float								buttonHoldThreshold			= 0.15f;
		[SerializeField] private float								shuffleDirectionX;
		[SerializeField] private float								shuffleDirectionZ;
		
		[Header("Grounded Angle")]
		[SerializeField] private Transform							rearRayPos;
		[SerializeField] private Transform							frontRayPos;
		[SerializeField] private LayerMask							groundLayerMask;
		[SerializeField] private float								inclineAngle;
		[SerializeField] private float								groundedOffset				= -0.14f;
		
		[Header("Player In-Air")]
		[SerializeField] private float								jumpForce					= 10f;
		[SerializeField] private float								gravityMultiplier			= 2f;
		[SerializeField] private float								fallingDuration;
		
		[Header("Player Strafing")]
		[SerializeField] private float								forwardStrafeMinThreshold	= -55.0f;
		[SerializeField] private float								forwardStrafeMaxThreshold	= 125.0f;
		[SerializeField] private float								forwardStrafe				= 1f;


		private AnimationState										currentState				= AnimationState.Base;
		private Vector3												previousRotation;
		private bool												isAiming;
		private bool												isCrouching;
		private bool												isGrounded					= true;
		private bool												isSliding;
		private bool												isSprinting;
		private bool												isStarting;
		private bool												isStopped					= true;
		private bool												isStrafing;
		private bool												isTurningInPlace;
		private bool												isWalking;
		private bool												movementInputHeld;
		private bool												movementInputPressed;
		private bool												movementInputTapped;
		private float												currentMaxSpeed;
		private Vector3												moveDirection;
		private Vector3												velocity;
		private float												speed2D;
		private float												newDirectionDifferenceAngle;
		private MovementState										currentGait;
		private float												strafeAngle;
		private float												strafeDirectionX;
		private float												strafeDirectionZ;
		
		
		private const float											STRAFE_DIRECTION_DAMP_TIME	= 20f;
		private const float											ANIMATION_DAMP_TIME			= 5f;
		private float												targetMaxSpeed;
		private Vector3												targetVelocity;
		private Vector3												cameraForward;
		
		#endregion
		
		//--------------------------------------------------------------------------------------------------------------
		private void Awake()
		{
			if (instance == null) instance = this;
			
		}

		private void Start()
		{
			Init();

			isStrafing = alwaysStrafe;
			SwitchState(AnimationState.Locomotion);
		}

		private void Init()
		{
			if (inputHandler == null)inputHandler = InputHandler.instance;
			if (playerAnim == null) playerAnim = GetComponentInChildren<Animator>();
			if (controller == null) controller = GetComponent<CharacterController>();
			if(cameraController == null) cameraController = CameraController.instance;
		}


		#region STATE

			#region State Change

			private void SwitchState(AnimationState _newState)
			{
				ExitCurrentState();
				EnterState(_newState);
			}

			private void EnterState(AnimationState _stateToEnter)
			{
				currentState = _stateToEnter;
				switch (currentState)
				{
					case AnimationState.Base:
						EnterBaseState();
						break;
					case AnimationState.Locomotion:
						EnterLocomotionState();
						break;
					case AnimationState.Jump:
						break;
					case AnimationState.Fall:
						break;
					case AnimationState.Crouch:
						break;
					
				}
			}

			private void ExitCurrentState()
			{
				switch (currentState)
				{
					case AnimationState.Locomotion:
						ExitLocomotionState();
						break;
					case AnimationState.Jump:
						break;
					case AnimationState.Crouch:
						break;
					
				}
			}
			#endregion

			#region Updates

			private void Update()
			{
				switch (currentState)
				{
					case AnimationState.Locomotion:
						UpdateLocomotionState();
						break;
					case AnimationState.Jump:
						break;
					case AnimationState.Fall:
						break;
					case AnimationState.Crouch:
						break;
				}
				
			}


			private void UpdateAnimatorController()
			{
				
			}

			#endregion

		#endregion

		#region Base State

			#region Setup

			private void EnterBaseState()
			{
				previousRotation = transform.forward;
			}

			private void CalculateInput()
			{
				//-- Find out what state movement button like it tapped, pressed or Held
				if (inputHandler.GetMovementInputDetected())
				{
					if (inputHandler.movementInputDuration == 0)
					{
						movementInputTapped = true;
					}
					else if (inputHandler.movementInputDuration > 0 && inputHandler.movementInputDuration < buttonHoldThreshold)
					{
						movementInputTapped = false;
						movementInputPressed = true;
						movementInputHeld = false;
					}
					else
					{
						movementInputTapped = false;
						movementInputPressed = false;
						movementInputHeld = true;
					}

					inputHandler.movementInputDuration += Time.deltaTime;
				}
				else
				{
					inputHandler.movementInputDuration = 0;
					movementInputTapped = false;
					movementInputPressed = false;
					movementInputHeld = false;
				}

				moveDirection = (cameraController.GetCameraForwardZeroedYNormalised() * inputHandler.GetMovementInput().y) +
				                (cameraController.GetCameraRightZeroedYNormalised() * inputHandler.GetMovementInput().x);
				
			}

			#endregion

			#region Movement

			private void Move()
			{
				controller.Move(velocity * Time.deltaTime);
			}

			private void ApplyGravity()
			{
				if (velocity.y > Physics.gravity.y)
				{
					velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
				}
			}

			private void CalculateMoveDirection()
			{
				CalculateInput();
				if (!isGrounded)
				{
					targetMaxSpeed = currentMaxSpeed;
				}
				else if (isCrouching)
				{
					targetMaxSpeed = walkSpeed;
				}
				else if (isSprinting)
				{
					targetMaxSpeed = sprintSpeed;
				}
				else if (isWalking)
				{
					targetMaxSpeed = walkSpeed;
				}
				else
				{
					targetMaxSpeed = runSpeed;
				}

				currentMaxSpeed = Mathf.Lerp(currentMaxSpeed, targetMaxSpeed, ANIMATION_DAMP_TIME * Time.deltaTime);

				targetVelocity.x = moveDirection.x * currentMaxSpeed;
				targetVelocity.z = moveDirection.z * currentMaxSpeed;

				velocity.z = Mathf.Lerp(velocity.z, targetVelocity.z, speedChangeDamping * Time.deltaTime);
				velocity.x = Mathf.Lerp(velocity.x, targetVelocity.x, speedChangeDamping * Time.deltaTime);

				speed2D = new Vector3(velocity.x, 0f, velocity.z).magnitude;
				speed2D = Mathf.Round(speed2D * 1000f) / 1000f;

				Vector3 playerForwardVector = transform.forward;

				newDirectionDifferenceAngle = playerForwardVector != moveDirection
					? Vector3.SignedAngle(playerForwardVector, moveDirection, Vector3.up)
					: 0f;

				CalculateMovementState();
			}
			
			//-- Idle = 0, Walk = 1, Run = 2, Sprint = 3
			private void CalculateMovementState()
			{
				float runThreshold = (walkSpeed + runSpeed) / 2;
				float sprintThreshold = (runSpeed + sprintSpeed) / 2;

				if (speed2D < 0.01)
				{
					currentGait = MovementState.Idle;
				}
				else if (speed2D < runThreshold)
				{
					currentGait = MovementState.Walk;
				}
				else if (speed2D < sprintThreshold)
				{
					currentGait = MovementState.Run;
				}
				else
				{
					currentGait = MovementState.Sprint;
				}
			}

			private void FaceMoveDirection()
			{
				Vector3 characterForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
				Vector3 characterRight = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
				Vector3 directionForward = new Vector3(moveDirection.x, 0f, moveDirection.z).normalized;

				cameraForward = cameraController.GetCameraForwardZeroedYNormalised();
				Quaternion strafingTargetRotation = Quaternion.LookRotation(cameraForward);

				strafeAngle = characterForward != directionForward
					? Vector3.SignedAngle(characterForward, directionForward, Vector3.up)
					: 0f;

				isTurningInPlace = false;

				if (isStrafing)
				{
					if (moveDirection.magnitude > 0.01)
					{
						if (cameraForward != Vector3.zero)
						{
							shuffleDirectionZ = Vector3.Dot(characterForward, directionForward);
							shuffleDirectionX = Vector3.Dot(characterRight, directionForward);
							
							UpdateStrafeDirection(Vector3.Dot(characterForward, directionForward), Vector3.Dot(characterRight, directionForward));

							cameraRotationOffset =
								Mathf.Lerp(cameraRotationOffset, 0f, rotationSmoothing * Time.deltaTime);
							float targetValue = strafeAngle > forwardStrafeMinThreshold &&
							                    strafeAngle < forwardStrafeMaxThreshold
								? 1f
								: 0f;
							
							if (Mathf.Abs(forwardStrafe - targetValue) <= 0.001f)
							{
								forwardStrafe = targetValue;
							}
							else
							{
								float t = Mathf.Clamp01(STRAFE_DIRECTION_DAMP_TIME * Time.deltaTime);
								forwardStrafe = Mathf.SmoothStep(forwardStrafe, targetValue, t);
							}

						}
						transform.rotation = Quaternion.Slerp(transform.rotation, strafingTargetRotation, rotationSmoothing * Time.deltaTime);
					}
					else
					{
						UpdateStrafeDirection(1f, 0f);

						float t = 20 * Time.deltaTime;
						float newOffset = characterForward != cameraForward
							? Vector3.SignedAngle(characterForward, cameraForward, Vector3.up)
							: 0f;
						
						cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, newOffset, t);
						if (Mathf.Abs(cameraRotationOffset) > 10)
						{
							isTurningInPlace = true;
						}
					}
				}
				else
				{
					UpdateStrafeDirection(1f, 0f);

					cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, 0f, rotationSmoothing * Time.deltaTime);
					shuffleDirectionZ = 1;
					shuffleDirectionX = 0;

					Vector3 faceDirection = new Vector3(velocity.x, 0f, velocity.z);
					if(faceDirection == Vector3.zero) return;
					
					transform.rotation = Quaternion.Slerp(transform.rotation,  Quaternion.LookRotation(faceDirection), rotationSmoothing * Time.deltaTime);
				}

			}
			
			//-- Check If Player Stopped
			private void CheckIfStopped()
			{
				isStopped = moveDirection.magnitude == 0 && speed2D < 0.5f;
			}
			
			//-- Check If Player Start Moving
			private void CheckIfStarting()
			{
				
			}

			private void UpdateStrafeDirection(float TargetZ, float TargetX)
			{
				strafeDirectionZ = Mathf.Lerp(strafeDirectionZ, TargetZ, ANIMATION_DAMP_TIME * Time.deltaTime);
				strafeDirectionX = Mathf.Lerp(strafeDirectionX, TargetX, ANIMATION_DAMP_TIME * Time.deltaTime);
				strafeDirectionZ = Mathf.Round(strafeDirectionZ * 1000f) / 1000f;
				strafeDirectionX = Mathf.Round(strafeDirectionX * 1000f) / 1000f;
			}
			
			
			#endregion

			#region Ground Check

			private void GroundedCheck()
			{
				Vector3 spherePosition = new Vector3(controller.transform.position.x, controller.transform.position.x - groundedOffset, controller.transform.position.z);

				isGrounded = Physics.CheckSphere(spherePosition, controller.radius, groundLayerMask,
					QueryTriggerInteraction.Ignore);
				
				if (isGrounded)
				{
					GroundInclineCheck();
				}
			}

			private void GroundInclineCheck()
			{
				
			}

			private void CeilingHeightCheck()
			{
				
			}
			#endregion

		#endregion


		#region Locomotion State

		private void EnterLocomotionState()
		{
			
		}

		private void UpdateLocomotionState()
		{
			GroundedCheck();
			// if (!isGrounded)
			// {
			// 	SwitchState(AnimationState.Fall);
			// }
			// if (isCrouching)
			// {
			// 	SwitchState(AnimationState.Crouch);
			// }
			
			// CheckEnableTurns();
			// CheckEnableLean();
			// CalculateRotationalAdditives(_enableLean, _enableHeadTurn, _enableBodyTurn);
			Debug.Log("Locomotion Update");
			CalculateMoveDirection();
			CheckIfStarting();
			CheckIfStopped();
			FaceMoveDirection();
			Move();
			UpdateAnimatorController();
			
		}

		private void ExitLocomotionState()
		{
			
		}
		
		private void LocomotionToJumpState()
		{
			SwitchState(AnimationState.Jump);
		}

		#endregion


		#region Gizmo

		

		#endregion
		
	}
}

