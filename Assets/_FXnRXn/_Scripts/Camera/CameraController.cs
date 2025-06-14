using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace FXnRXn
{
	[RequireComponent(typeof(NetworkObject))]
	public class CameraController : NetworkBehaviour
	{
		public static CameraController							instance { get; private set; }
		private const int										LAG_DELTA_TIME_ADJUSTMENT = 20;
		
		
		[Header("-------------		Refference :		-------------")]
		[Space(10)]
		[SerializeField] private GameObject						mainCharacter;
		[SerializeField] private Camera							mainCamera;
		
		
		[Header("-------------		Settings :		-------------")]
		[Space(10)]
		[SerializeField] private Transform						playerTarget;
		[SerializeField] private Transform						lockOnTarget;

		[Space(10)]
		[SerializeField] private bool							invertCamera;
		[SerializeField] private bool							hideCursor;
		[SerializeField] private bool							isLockedOn;
		[SerializeField] private float							mouseSensitivity			= 5f;
		[SerializeField] private float							cameraDistance				= 5f;
		[SerializeField] private float							cameraHeightOffset;
		[SerializeField] private float							cameraHorizontalOffset;
		
		[SerializeField] private float							cameraTiltOffset;
		[SerializeField] private Vector2						cameraTiltBounds			= new Vector2(-10f, 45f);
		[SerializeField] private float							positionalCameraLag			= 1f;
		[SerializeField] private float							rotationalCameraLag			= 1f;
		
		[Header("-------------		ADS Settings :		-------------")]
		[Space(10)]
		[SerializeField] private List<WeaponADSConfigSO>		weaponADSProfiles;
		[SerializeField] private float							adsTransitionSpeed			= 10f;
		
		
		private Dictionary<WeaponType, WeaponADSConfigSO>		adsProfileDict = new Dictionary<WeaponType, WeaponADSConfigSO>();
		private bool											isAimingDownSights			= false;
		private Vector3											currentAdsPositionOffset	= Vector3.zero;
		private Vector3											currentAdsRotationOffset	= Vector3.zero;
		private float											currentAdsSensitivityMultiplier = 1f;
		private float											cameraInversion;
		private InputHandler									inputReader;
		private float											lastAngleX;
		private float											lastAngleY;
		private Vector3											lastPosition;
		private float											newAngleX;
		private float											newAngleY;
		private Vector3											newPosition;
		private float											rotationX;
		private float											rotationY;
		private Transform										gameCamera;
		
		
		
		//--------------------------------------------------------------------------------------------------------------
		private void Awake()
		{
			if (instance == null) instance = this;
			if (mainCamera == null)
			{
				mainCamera = GetComponentInChildren<Camera>();
				gameCamera = transform.GetChild(0);
			}
			
			if (weaponADSProfiles == null)
			{
				var configADSSo = new WeaponADSConfigSO();
				weaponADSProfiles.Add(configADSSo);
			}
		}

		public override void OnNetworkSpawn()
		{
			foreach (var adsConfig in weaponADSProfiles)
			{
				adsProfileDict.Add(adsConfig.weaponType, adsConfig);
			}
			
			if (IsClient)
			{
				mainCamera.gameObject.SetActive(true);
				transform.gameObject.SetActive(true);
			}
			else
			{
				mainCamera.gameObject.SetActive(false);
				transform.gameObject.SetActive(false);
			}
			
			if (inputReader == null)inputReader = InputHandler.instance;

			if (inputReader != null)
			{
				InputHandler.instance.onAimActivated += EnableAim;
				InputHandler.instance.onAimDeactivated += DisableAim;
			}
		}

		public override void OnNetworkDespawn()
		{
			if (inputReader != null)
			{
				InputHandler.instance.onAimActivated -= EnableAim;
				InputHandler.instance.onAimDeactivated -= DisableAim;
			}
		}
		
		
		public void Init(Transform _player, Transform _target, Transform _lockOn)
		{
			if (mainCharacter == null) mainCharacter = _player.gameObject;
			
			playerTarget = _target;
			lockOnTarget = _lockOn;

			if (hideCursor)
			{
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;
			}

			cameraInversion = invertCamera ? 1 : -1;
			
			transform.position = playerTarget.position;
			transform.rotation = playerTarget.rotation;
			lastPosition = transform.position;
			
			gameCamera.localPosition = new Vector3(cameraHorizontalOffset, cameraHeightOffset, cameraDistance * -1);
			gameCamera.localEulerAngles = new Vector3(cameraTiltOffset, 0f, 0f);
		}


		private void LateUpdate()
		{
			if(inputReader == null || playerTarget == null || lockOnTarget == null) return;
			
			float positionalFollowSpeed = 1 / (positionalCameraLag / LAG_DELTA_TIME_ADJUSTMENT);
			float rotationalFollowSpeed = 1 / (rotationalCameraLag / LAG_DELTA_TIME_ADJUSTMENT);

			rotationX = inputReader.GetLookInput().y * cameraInversion * mouseSensitivity;
			rotationY = inputReader.GetLookInput().x * mouseSensitivity;

			newAngleX += rotationX;
			newAngleX = Mathf.Clamp(newAngleX, cameraTiltBounds.x, cameraTiltBounds.y);
			newAngleX = Mathf.Lerp(lastAngleX, newAngleX, rotationalFollowSpeed * Time.deltaTime);

			if (isLockedOn)
			{
				Vector3 aimVector = lockOnTarget.position - playerTarget.position;
				Quaternion targetRotation = Quaternion.LookRotation(aimVector);
				targetRotation = Quaternion.Lerp(transform.rotation , targetRotation, rotationalFollowSpeed * Time.deltaTime);
				newAngleY = targetRotation.eulerAngles.y;
			}
			else
			{
				newAngleY += rotationY;
				newAngleY = Mathf.Lerp(lastAngleY, newAngleY, rotationalFollowSpeed * Time.deltaTime);
			}

			newPosition = playerTarget.position;
			newPosition = Vector3.Lerp(lastPosition, newPosition, positionalFollowSpeed * Time.deltaTime);

			transform.position = newPosition;
			transform.eulerAngles = new Vector3(newAngleX, newAngleY, 0);

			// gameCamera.localPosition = new Vector3(cameraHorizontalOffset, cameraHeightOffset, cameraDistance * -1);
			// gameCamera.localEulerAngles = new Vector3(cameraTiltOffset, 0f, 0f);
			
			// Calculate target camera local position
			Vector3 targetLocalPosition = new Vector3(cameraHorizontalOffset, cameraHeightOffset, cameraDistance * -1);
			// Calculate target camera local Euler angles
			Vector3 targetLocalEulerAngles = new Vector3(cameraTiltOffset, 0f, 0f);
			
			// If aiming down sights, add the ADS offsets
			if (isAimingDownSights)
			{
				targetLocalPosition += currentAdsPositionOffset;
				targetLocalEulerAngles += currentAdsRotationOffset;
			}
			
			// Smoothly interpolate the gameCamera's local position towards the target
			gameCamera.localPosition = Vector3.Lerp(gameCamera.localPosition, targetLocalPosition, adsTransitionSpeed * Time.deltaTime);
			// Smoothly interpolate the gameCamera's local Euler angles towards the target
			gameCamera.localEulerAngles = Vector3.Lerp(gameCamera.localEulerAngles, targetLocalEulerAngles, adsTransitionSpeed * Time.deltaTime);


			lastPosition = newPosition;
			lastAngleX = newAngleX;
			lastAngleY = newAngleY;

		}



		public void LockOn(bool _enable, Transform _newLockOnTarget)
		{
			isLockedOn = _enable;
			if (_newLockOnTarget != null)
			{
				lockOnTarget = _newLockOnTarget;
			}
		}
		
		#region ADS
		public void StartAimDownSights(WeaponType weaponType)
		{
			if (adsProfileDict.TryGetValue(weaponType, out WeaponADSConfigSO ads))
			{
				if (ads != null)
				{
					currentAdsPositionOffset = ads.adsCameraOffset;
					currentAdsRotationOffset = ads.rotationOffset;
					currentAdsSensitivityMultiplier = ads.adsSensitivityMultiplier;
				}
				else
				{
					currentAdsPositionOffset = Vector3.zero;
					currentAdsRotationOffset = Vector3.zero;
					currentAdsSensitivityMultiplier = 1f;
					Debug.LogWarning($"ADS profile not found for weapon type: {weaponType}. Using default settings.");
				}
			}
		}
		
		public void StopAimDownSights()
		{
			isAimingDownSights = false;
			currentAdsPositionOffset = Vector3.zero;
			currentAdsRotationOffset = Vector3.zero;
			currentAdsSensitivityMultiplier = 1f;
		}
		private void EnableAim()
		{
			isAimingDownSights = true;
			StartAimDownSights(WeaponType.Rifle);
		}
		private void DisableAim()
		{
			isAimingDownSights = false;
			StopAimDownSights();
		}
		
		#endregion
		
		
		
		
		
		
		public Vector3 GetCameraPosition() => mainCamera.transform.position;

		public Vector3 GetCameraForward() => mainCamera.transform.forward;
		
		public Vector3 GetCameraForwardZeroedY() => new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z);
		
		public Vector3 GetCameraForwardZeroedYNormalised() => GetCameraForwardZeroedY().normalized;
		public Vector3 GetCameraRightZeroedY() => new Vector3(mainCamera.transform.right.x, 0, mainCamera.transform.right.z);
		
		public Vector3 GetCameraRightZeroedYNormalised() => GetCameraRightZeroedY().normalized;

		public float GetCameraTiltX() => mainCamera.transform.eulerAngles.x;

		public Camera GetMainCamera() => mainCamera;


	}// CameraController
}

