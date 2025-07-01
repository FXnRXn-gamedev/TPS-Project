using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace FXnRXn
{
	public class NetworkedWeaponHolder : NetworkBehaviour
	{
		#region --- Variables ---

		[Header("-------------		Weapon Settings		-------------")]
		[Space(10)]
		
		[SerializeField] private WeaponConfigDataSO									currentWeaponConfigData;
		[SerializeField] private int												currentWeaponID;
		[SerializeField] private float												weaponInitTransitionSpeed = 10f;
		
		
		private bool															isAiming = false;
		private bool															_localIsAiming;
		private Coroutine														weaponTransition;
		private float															transitionProgress;
		
		
		
		private NetworkVariable<bool> networkIsAiming = new NetworkVariable<bool>(
			default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
		private NetworkVariable<Vector3> networkWeaponParentPosition = new NetworkVariable<Vector3>(
			default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
		private NetworkVariable<Quaternion> networkWeaponParentRotation = new NetworkVariable<Quaternion>(
			default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
		#endregion
		
		
		//--------------------------------------------------------------------------------------------------------------

		#region --- UNITY ---

		public override void OnNetworkSpawn()
		{
			currentWeaponID = PlayerWeapon.Instance.networkCurrentWeaponID.Value;
			currentWeaponConfigData = GetWeaponIKData(currentWeaponID);
			
			
			PlayerWeapon.Instance.networkCurrentWeaponID.OnValueChanged += OnNetworkWeaponIDChanged;
			InputHandler.instance.onAimActivated += EnableAim;
			InputHandler.instance.onAimDeactivated += DisableAim;
			networkWeaponParentPosition.OnValueChanged += SyncWeaponPosition;
			networkWeaponParentRotation.OnValueChanged += SyncWeaponRotation;
			if (IsOwner && PlayerWeapon.Instance.networkCurrentWeaponID.Value != null)
			{
				EquipWeapon();
			}
		}

		public override void OnNetworkDespawn()
		{
			InputHandler.instance.onAimActivated -= EnableAim;
			InputHandler.instance.onAimDeactivated -= DisableAim;
			networkWeaponParentPosition.OnValueChanged -= SyncWeaponPosition;
			networkWeaponParentRotation.OnValueChanged -= SyncWeaponRotation;
		}
		
		private void Update()
		{
			if (IsOwner)
			{
				HandleOwnerInput();
				UpdateLocalWeaponPosition();
			}
			
			ApplyNetworkedState();
		}

		#endregion
		
		
		//--------------------------------------------------------------------------------------------------------------

		private WeaponConfigDataSO GetWeaponIKData(int id)
		{
			if(id == -1) return null;
			return PlayerWeapon.Instance.GetWeaponDataByID(id);
		}

		private void OnNetworkWeaponIDChanged(int previousID, int newID)
		{
			WeaponConfigDataSO data = PlayerWeapon.Instance.GetWeaponDataByID(newID);
			currentWeaponConfigData = data;
		}


		#region --- Init Weapon Position Set ---

		private void EquipWeapon()
		{
			EquipWeaponServerRpc();
		}
		
		[Rpc(SendTo.Server)]
		private void EquipWeaponServerRpc()
		{
			if (weaponTransition != null) StopCoroutine(weaponTransition);
			weaponTransition = StartCoroutine(TransitionWeaponPosition(false));

			EquipWeaponClientRpc();
		}
		
		[ClientRpc]
		private void EquipWeaponClientRpc()
		{
			if (weaponTransition != null) StopCoroutine(weaponTransition);
			weaponTransition = StartCoroutine(TransitionWeaponPosition(false));
		}
		
		private IEnumerator TransitionWeaponPosition(bool aiming)
		{
			if (currentWeaponConfigData == null) yield return null;
			
			transitionProgress = 0f;
			
			Vector3 startPos = transform.localPosition;
			Vector3 endPos = aiming ? currentWeaponConfigData.adsPosition : currentWeaponConfigData.hipPosition;
        
			Quaternion startRot = transform.localRotation;
			Quaternion endRot = Quaternion.Euler(aiming ? currentWeaponConfigData.adsRotation : currentWeaponConfigData.hipRotation);
			

			while (transitionProgress < 1f)
			{
				transitionProgress += Time.deltaTime * weaponInitTransitionSpeed;
            
				transform.localPosition = Vector3.Lerp(startPos, endPos, transitionProgress);
				transform.localRotation = Quaternion.Slerp(startRot, endRot, transitionProgress);
				yield return null;
			}
		}

		#endregion
		
		#region --- Aim ---
		
		private void HandleOwnerInput()
		{
			SyncSetAiming(isAiming);
			
		}
		private void SyncSetAiming(bool aiming)
		{
			networkIsAiming.Value = aiming;
		}
		
		private void EnableAim()
		{
			isAiming = true;
		}

		private void DisableAim()
		{
			isAiming = false;
		}

		#endregion

		#region --- Update Weapon Position Server/Client ---

		private void UpdateLocalWeaponPosition()
		{
			if(currentWeaponConfigData == null) return;
			
			Vector3 desiredPosition = isAiming ? currentWeaponConfigData.adsPosition: currentWeaponConfigData.hipPosition;
			Quaternion desiredRotation = isAiming ? Quaternion.Euler(currentWeaponConfigData.adsRotation) : Quaternion.Euler(currentWeaponConfigData.hipRotation);
			
			
			transform.localPosition = Vector3.Lerp(
				transform.localPosition,
				desiredPosition,
				Time.deltaTime * 5f
			);
			
			transform.localRotation = Quaternion.Lerp(
				transform.localRotation,
				desiredRotation,
				Time.deltaTime * 5f
			);
			
			
			SyncWeaponTransform(transform.localPosition, transform.localRotation);
			
		}

		private void ApplyNetworkedState()
		{
			// Aim state
			if (networkIsAiming.Value != _localIsAiming)
			{
				if (weaponTransition != null) StopCoroutine(weaponTransition);
				weaponTransition = StartCoroutine(TransitionWeaponPosition(networkIsAiming.Value));
				
				_localIsAiming = networkIsAiming.Value;
			}

			if (!IsOwner)
			{
				transform.localPosition = Vector3.Lerp(
					transform.localPosition, 
					networkWeaponParentPosition.Value, 
					Time.deltaTime * 5f
				);
				       
				transform.localRotation = Quaternion.Slerp(
					transform.localRotation, 
					networkWeaponParentRotation.Value, 
					Time.deltaTime * 5f
				);
			}
		}
		
		private void SyncWeaponTransform(Vector3 position, Quaternion rotation)
		{
			networkWeaponParentPosition.Value = position;
			networkWeaponParentRotation.Value = rotation;
		}
		
		private void SyncWeaponPosition(Vector3 prevPos, Vector3 newPos)
		{
			
			if (!IsOwner)
			{
				transform.localPosition = newPos;
			}
			
		}
		
		private void SyncWeaponRotation(Quaternion prevRot, Quaternion newRot)
		{
			
			if (!IsOwner)
			{
				transform.localRotation = newRot;
			}
		}

		#endregion
		
		
	}
}

