using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode;

namespace FXnRXn
{
	[RequireComponent(typeof(CharacterController), typeof(NetworkObject))]
	public class PlayerMovementController : NetworkBehaviour
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
		private readonly int movementInputTappedHash = Animator.StringToHash("MovementInputTapped");
        private readonly int movementInputPressedHash = Animator.StringToHash("MovementInputPressed");
        private readonly int movementInputHeldHash = Animator.StringToHash("MovementInputHeld");
        private readonly int shuffleDirectionXHash = Animator.StringToHash("ShuffleDirectionX");
        private readonly int shuffleDirectionZHash = Animator.StringToHash("ShuffleDirectionZ");

        private readonly int moveSpeedHash = Animator.StringToHash("MoveSpeed");
        private readonly int currentGaitHash = Animator.StringToHash("CurrentGait");

        private readonly int isJumpingAnimHash = Animator.StringToHash("IsJumping");
        private readonly int fallingDurationHash = Animator.StringToHash("FallingDuration");

        private readonly int inclineAngleHash = Animator.StringToHash("InclineAngle");

        private readonly int strafeDirectionXHash = Animator.StringToHash("StrafeDirectionX");
        private readonly int strafeDirectionZHash = Animator.StringToHash("StrafeDirectionZ");

        private readonly int forwardStrafeHash = Animator.StringToHash("ForwardStrafe");
        private readonly int cameraRotationOffsetHash = Animator.StringToHash("CameraRotationOffset");
        private readonly int isStrafingHash = Animator.StringToHash("IsStrafing");
        private readonly int isTurningInPlaceHash = Animator.StringToHash("IsTurningInPlace");

        private readonly int isCrouchingHash = Animator.StringToHash("IsCrouching");

        private readonly int isWalkingHash = Animator.StringToHash("IsWalking");
        private readonly int isStoppedHash = Animator.StringToHash("IsStopped");
        private readonly int isStartingHash = Animator.StringToHash("IsStarting");

        private readonly int isGroundedHash = Animator.StringToHash("IsGrounded");

        private readonly int leanValueHash = Animator.StringToHash("LeanValue");
        private readonly int headLookXHash = Animator.StringToHash("HeadLookX");
        private readonly int headLookYHash = Animator.StringToHash("HeadLookY");

        private readonly int bodyLookXHash = Animator.StringToHash("BodyLookX");
        private readonly int bodyLookYHash = Animator.StringToHash("BodyLookY");

        private readonly int locomotionStartDirectionHash = Animator.StringToHash("LocomotionStartDirection");
		

		#endregion

		#region Player Settings Variables

		[Header("Refference :")] 
		[Space(10)] 
		[SerializeField]private CameraController					cameraController;
		[SerializeField] private InputHandler						inputHandler;
		[SerializeField] private Animator							playerAnim;
		[SerializeField] private CharacterController				controller;
		[SerializeField] private Transform							playerTarget;
		[SerializeField] private Transform							lockOnTarget;

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
		[SerializeField] private float								jumpForce					= .1f;
		[SerializeField] private float								gravityMultiplier			= 2f;
		[SerializeField] private float								fallingDuration;
		
		[Header("Player Strafing")]
		[SerializeField] private float								forwardStrafeMinThreshold	= -55.0f;
		[SerializeField] private float								forwardStrafeMaxThreshold	= 125.0f;
		[SerializeField] private float								forwardStrafe				= 1f;
		
		[Header("Player Head Look")]
		[SerializeField] private bool								enableHeadTurn				= true;
		[SerializeField] private float								headLookDelay;
		[SerializeField] private float								headLookX;
		[SerializeField] private float								headLookY;
		[SerializeField] private AnimationCurve						headLookXCurve;
		
		[Header("Player Body Look")]
		[SerializeField] private bool								enableBodyTurn				= true;
		[SerializeField] private float								bodyLookDelay;
		[SerializeField] private float								bodyLookX;
		[SerializeField] private float								bodyLookY;
		[SerializeField] private AnimationCurve						bodyLookXCurve;
		
		[Header("Player Lean")]
		[SerializeField] private bool								enableLean					= true;
		[SerializeField] private float								leanDelay;
		[SerializeField] private float								leanValue;
		[SerializeField] private AnimationCurve						leanCurve;
		[SerializeField] private float								leansHeadLooksDelay;
		[SerializeField] private bool								animationClipEnd;


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
		private Vector3												currentRotation				= new Vector3(0f, 0f, 0f);
		private Vector3												moveDirection;
		private Vector3												velocity;
		private float												speed2D;
		private float												newDirectionDifferenceAngle;
		private MovementState										currentGait;
		private float												strafeAngle;
		private float												strafeDirectionX;
		private float												strafeDirectionZ;
		private float												locomotionStartTimer;
		private float												locomotionStartDirection;
		
		
		private const float											STRAFE_DIRECTION_DAMP_TIME	= 20f;
		private const float											ANIMATION_DAMP_TIME			= 5f;
		private float												targetMaxSpeed;
		private float												fallStartTime;
		private Vector3												targetVelocity;
		private Vector3												cameraForward;
		private float												rotationRate;
		private float												initialLeanValue;
		private float												initialTurnValue;
		
		
		
		#endregion
		
		//--------------------------------------------------------------------------------------------------------------
		private void Awake()
		{
			if (instance == null) instance = this;
		}

		private void Start()
		{
			isStrafing = alwaysStrafe;
			
			
		}

		public override void OnNetworkSpawn()
		{
			if(!IsOwner) return;
			
			if(cameraController == null) cameraController = CameraController.instance;
			if (inputHandler == null) inputHandler = InputHandler.instance;
			if (playerAnim == null) playerAnim = GetComponentInChildren<Animator>();
			if (controller == null) controller = GetComponent<CharacterController>();

			
			
			cameraController.Init(transform, playerTarget, lockOnTarget);
			SwitchState(AnimationState.Locomotion);
			DeactivateSprint();
		}
		


		#region STATE

			#region Walking State
			private void ToggleWalk()
			{
				EnableWalk(!isWalking);
			}
			private void EnableWalk(bool enable)
			{
				isWalking = enable && isGrounded && !isSprinting;
			}

			#endregion
		
			#region Sprinting State

			public void ActivateSprint()
			{
				if (!isCrouching)
				{
					EnableWalk(false);
					isSprinting = true;
					isStrafing = false;
				}
			}
			public void DeactivateSprint()
			{
				isSprinting = false;

				if (alwaysStrafe || isAiming)
				{
					isStrafing = true;
				}
			}

			#endregion

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
						EnterJumpState();
						break;
					case AnimationState.Fall:
						EnterFallState();
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
						ExitJumpState();
						break;
					case AnimationState.Crouch:
						break;
					
				}
			}
			#endregion

			#region Updates

			private void Update()
			{
				if(!IsOwner) return;
				
				
				SyncMoveServerRpc(controller.transform.position, controller.transform.rotation);
				
				switch (currentState)
				{
					case AnimationState.Locomotion:
						UpdateLocomotionState();
						break;
					case AnimationState.Jump:
						UpdateJumpState();
						break;
					case AnimationState.Fall:
						UpdateFallState();
						break;
					case AnimationState.Crouch:
						break;
				}
				
			}


			private void UpdateAnimatorController()
			{
				playerAnim.SetFloat(leanValueHash, leanValue);
				playerAnim.SetFloat(headLookXHash, headLookX);
				playerAnim.SetFloat(headLookYHash, headLookY);
				playerAnim.SetFloat(bodyLookXHash, bodyLookX);
				playerAnim.SetFloat(bodyLookYHash, bodyLookY);

				playerAnim.SetFloat(isStrafingHash, isStrafing ? 1.0f : 0.0f);

				playerAnim.SetFloat(inclineAngleHash, inclineAngle);

				playerAnim.SetFloat(moveSpeedHash, speed2D);
				playerAnim.SetInteger(currentGaitHash, (int) currentGait);

				playerAnim.SetFloat(strafeDirectionXHash, strafeDirectionX);
				playerAnim.SetFloat(strafeDirectionZHash, strafeDirectionZ);
				playerAnim.SetFloat(forwardStrafeHash, forwardStrafe);
				playerAnim.SetFloat(cameraRotationOffsetHash, cameraRotationOffset);

				playerAnim.SetBool(movementInputHeldHash, movementInputHeld);
				playerAnim.SetBool(movementInputPressedHash, movementInputPressed);
				playerAnim.SetBool(movementInputTappedHash, movementInputTapped);
				playerAnim.SetFloat(shuffleDirectionXHash, shuffleDirectionX);
				playerAnim.SetFloat(shuffleDirectionZHash, shuffleDirectionZ);

				playerAnim.SetBool(isTurningInPlaceHash, isTurningInPlace);
				playerAnim.SetBool(isCrouchingHash, isCrouching);

				playerAnim.SetFloat(fallingDurationHash, fallingDuration);
				playerAnim.SetBool(isGroundedHash, isGrounded);

				playerAnim.SetBool(isWalkingHash, isWalking);
				playerAnim.SetBool(isStoppedHash, isStopped);

				playerAnim.SetFloat(locomotionStartDirectionHash, locomotionStartDirection);
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

							cameraRotationOffset = Mathf.Lerp(cameraRotationOffset, 0f, rotationSmoothing * Time.deltaTime);
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
						//transform.rotation = Quaternion.Slerp(transform.rotation, strafingTargetRotation, rotationSmoothing * Time.deltaTime);
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
					
					transform.rotation = Quaternion.Slerp(transform.rotation, strafingTargetRotation, rotationSmoothing * Time.deltaTime);
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
				locomotionStartTimer = VariableOverrideDelayTimer(locomotionStartTimer);

				bool isStartingCheck = false;

				if (locomotionStartTimer <= 0.0f)
				{
					if (moveDirection.magnitude > 0.01 && speed2D < 1 && !isStrafing)
					{
						isStartingCheck = true;
					}

					if (isStartingCheck)
					{
						if (!isStarting)
						{
							locomotionStartDirection = newDirectionDifferenceAngle;
							playerAnim.SetFloat(locomotionStartDirectionHash, locomotionStartDirection);
						}
						float delayTime = 0.2f;
						leanDelay = delayTime;
						headLookDelay = delayTime;
						bodyLookDelay = delayTime;

						locomotionStartTimer = delayTime;
					}
				}
				else
				{
					isStartingCheck = true;
				}

				isStarting = isStartingCheck;
				playerAnim.SetBool(isStartingHash, isStarting);
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
				Vector3 spherePosition = new Vector3(controller.transform.position.x, controller.transform.position.y - groundedOffset, controller.transform.position.z);

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

			#region Check

			private void CheckEnableTurns()
			{
				headLookDelay = VariableOverrideDelayTimer(headLookDelay);
				enableHeadTurn = headLookDelay == 0.0f && !isStarting;
				bodyLookDelay = VariableOverrideDelayTimer(bodyLookDelay);
				enableBodyTurn = bodyLookDelay == 0.0f && !(isStarting || isTurningInPlace);
			}
			
			private void CheckEnableLean()
			{
				leanDelay = VariableOverrideDelayTimer(leanDelay);
				enableLean = leanDelay == 0.0f && !(isStarting || isTurningInPlace);
			}
			#endregion

			#region Lean and Offsets

			private void CalculateRotationalAdditives(bool _leansActivated, bool _headLookActivated,
				bool _bodyLookActivated)
			{
				if (_headLookActivated || _leansActivated || _bodyLookActivated)
				{
					currentRotation = transform.forward;

					rotationRate = currentRotation != previousRotation
						? Vector3.SignedAngle(currentRotation, previousRotation, Vector3.up) / Time.deltaTime * -1f
						: 0f;
					
				}
				// Lean
				initialLeanValue = _leansActivated ? rotationRate : 0f;
				float leanSmoothness = 5;
				float maxLeanRotationRate = 275.0f;

				float referenceValue = speed2D / sprintSpeed;
				leanValue = CalculateSmoothedValue(leanValue, initialLeanValue, maxLeanRotationRate, leanSmoothness,
					leanCurve, referenceValue, true);
				
				// Head Turn
				float headTurnSmoothness = 5f;

				if (_headLookActivated && isTurningInPlace)
				{
					initialTurnValue = cameraRotationOffset;
					headLookX = Mathf.Lerp(headLookX, initialTurnValue / 200, headTurnSmoothness * Time.deltaTime);
				}
				else
				{
					initialTurnValue = _headLookActivated ? rotationRate : 0f;
					headLookX = CalculateSmoothedValue(headLookX, initialTurnValue, maxLeanRotationRate, 
						headTurnSmoothness, headLookXCurve, headLookX, false);
				}
				
				// Body Turn
				float bodyTurnSmoothness = 5f;

				initialTurnValue = _bodyLookActivated ? rotationRate : 0f;
				
				bodyLookX = CalculateSmoothedValue(
					bodyLookX,
					initialTurnValue,
					maxLeanRotationRate,
					bodyTurnSmoothness,
					bodyLookXCurve,
					bodyLookX,
					false
				);

				float cameraTilt = cameraController.GetCameraTiltX();
				cameraTilt = (cameraTilt > 180f ? cameraTilt - 360f : cameraTilt) / -180;
				cameraTilt = Mathf.Clamp(cameraTilt, -0.1f, 1.0f);
				headLookX = cameraTilt;
				bodyLookX = cameraTilt;

				previousRotation = currentRotation;
			}

			private float CalculateSmoothedValue(
				float mainVariable,
				float newValue,
				float maxRateChange,
				float smoothness,
				AnimationCurve referenceCurve,
				float referenceValue,
				bool isMultiplier
			)
			{
				float changeVariable = newValue / maxRateChange;

				changeVariable = Mathf.Clamp(changeVariable, -1.0f, 1.0f);

				if (isMultiplier)
				{
					float multiplier = referenceCurve.Evaluate(referenceValue);
					changeVariable *= multiplier;
				}
				else
				{
					changeVariable = referenceCurve.Evaluate(changeVariable);
				}

				if (!changeVariable.Equals(mainVariable))
				{
					changeVariable = Mathf.Lerp(mainVariable, changeVariable, smoothness * Time.deltaTime);
				}

				return changeVariable;
			}

			private float VariableOverrideDelayTimer(float _timeVariable)
			{
				if (_timeVariable > 0.0f)
				{
					_timeVariable -= Time.deltaTime;
					_timeVariable = Mathf.Clamp(_timeVariable, 0.0f, 1.0f);
				}
				else
				{
					_timeVariable = 0.0f;
				}

				return _timeVariable;
			}

			#endregion

			#region Falling

			private void ResetFallingDuration()
			{
				fallStartTime = Time.time;
				fallingDuration = 0f;
			}

			private void UpdateFallingDuration()
			{
				fallingDuration = Time.time - fallStartTime;
			}
			

			#endregion

		#endregion


		#region Locomotion State

		private void EnterLocomotionState()
		{
			inputHandler.onJumpPerformed += LocomotionToJumpState;
		}

		private void UpdateLocomotionState()
		{
			GroundedCheck();
			ApplyGravity();
			// if (!isGrounded)
			// {
			// 	SwitchState(AnimationState.Fall);
			// }
			// if (isCrouching)
			// {
			// 	SwitchState(AnimationState.Crouch);
			// }
			
			CheckEnableTurns();
			CheckEnableLean();
			CalculateRotationalAdditives(enableLean, enableHeadTurn, enableBodyTurn);
			
			CalculateMoveDirection();
			CheckIfStarting();
			CheckIfStopped();
			FaceMoveDirection();
			Move();
			UpdateAnimatorController();
			
			
			
			

		}

		private void ExitLocomotionState()
		{
			inputHandler.onJumpPerformed -= LocomotionToJumpState;
		}
		
		private void LocomotionToJumpState()
		{
			SwitchState(AnimationState.Jump);
		}

		#endregion

		#region Jump State

		private void EnterJumpState()
		{
			playerAnim.SetBool(isJumpingAnimHash, true);
			isSliding = false;
			velocity = new Vector3(velocity.x, jumpForce, velocity.z);
		}

		private void UpdateJumpState()
		{
			ApplyGravity();
			if (velocity.y <= 0f)
			{
				playerAnim.SetBool(isJumpingAnimHash, false);
				SwitchState(AnimationState.Fall);
			}
			GroundedCheck();
			
			CalculateRotationalAdditives(false, enableHeadTurn, enableBodyTurn);
			CalculateMoveDirection();
			FaceMoveDirection();
			Move();
			UpdateAnimatorController();
			
			
		}

		private void ExitJumpState()
		{
			playerAnim.SetBool(isJumpingAnimHash, false);
		}

		#endregion

		#region Fall State

		private void EnterFallState()
		{
			ResetFallingDuration();
			velocity.y = 0f;
			
			//DeactivateCrouch();
			isSliding = false;
		}
		
		private void UpdateFallState()
		{
			GroundedCheck();
			CalculateRotationalAdditives(false, enableHeadTurn, enableBodyTurn);
			
			CalculateMoveDirection();
			FaceMoveDirection();
			
			ApplyGravity();
			Move();
			UpdateAnimatorController();

			if (controller.isGrounded)
			{
				SwitchState(AnimationState.Locomotion);
			}
			
			UpdateFallingDuration();
		}
		
		#endregion


		#region Server RPC
		[ServerRpc]
		private void SyncMoveServerRpc(Vector3 pos, Quaternion rot)
		{
			//transform.SetPositionAndRotation(pos, rot);
			transform.position = Vector3.Lerp(transform.position, pos, 0.1f);
			transform.rotation = Quaternion.Lerp(transform.rotation, rot, 0.1f);
			SyncMoveClientRpc(controller.transform.position, controller.transform.rotation);
		}
		
		#endregion
		
		#region Client RPC
		[ClientRpc]
		private void SyncMoveClientRpc(Vector3 pos, Quaternion rot)
		{
			//transform.SetPositionAndRotation(pos, rot);
			transform.position = Vector3.Lerp(transform.position, pos, 0.1f);
			transform.rotation = Quaternion.Lerp(transform.rotation, rot, 0.1f);
		}
		
		#endregion
		
	}
}

