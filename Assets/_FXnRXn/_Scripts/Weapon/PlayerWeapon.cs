using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


namespace FXnRXn
{
	public class PlayerWeapon : NetworkBehaviour
	{
		public static PlayerWeapon Instance { get; private set; }
		
		#region --- Animation Variable Hashes ---
		private readonly int _weaponTypeHash					= Animator.StringToHash("WeaponType");//int
		private readonly int _aimingHash						= Animator.StringToHash("Aiming");//bool
		private readonly int _shootHash							= Animator.StringToHash("Shoot");//trigger
		private readonly int _reloadHash						= Animator.StringToHash("Reload");//trigger
		#endregion


		#region --- Variables ---

		public WeaponType debugWeaponHolding;

		[Header("-------------		Refference		-------------")]
		[Space(10)]
		[SerializeField] private NetworkObject										weaponHolderPrefab;
		
		[Header("-------------		Weapon Settings		-------------")]
		[Space(25)]
		
		[SerializeField] private List<WeaponConfigDataSO>							weaponConfigsData;
		[SerializeField] private int												defaultWeaponIndex = 0;
		[SerializeField] private LayerMask											weaponLayer;
		public Weapon																currentWeapon;
		public WeaponConfigDataSO													currentWeaponConfigData { get; private set; }
		
		
		
		private Dictionary<int, WeaponConfigDataSO>									weaponDataPool;
		private int																	currentWeaponIndex = -1;
		private const int															MaxWeapons = 8;
		private Animator															playerAnim;
		private NetworkObjectReference												weaponHolderReference;
		private Transform															weaponHolder;
		private Transform															currentWeaponObjectInstance;

		private WeaponType _currentWeaponType;
		public WeaponType CurrentWeaponType
		{
			get => _currentWeaponType;
			set => _currentWeaponType = value;
		}
		
		private WeaponState _currentWeaponState;
		public WeaponState CurrentWeaponState
		{
			get => _currentWeaponState;
			set => _currentWeaponState = value;
		}
		
		
		
		public NetworkVariable<int> networkCurrentWeaponID = new NetworkVariable<int>(
			-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
		
		private NetworkVariable<NetworkObjectReference> WeaponHolderRef = new NetworkVariable<NetworkObjectReference>();

		#endregion
		
		
		//--------------------------------------------------------------------------------------------------------------

		#region --- UNITY ---
		private void Awake()
		{
			if(Instance == null) Instance = this;
			playerAnim = GetComponentInChildren<Animator>();
		}

		public override void OnNetworkSpawn()
		{
			if (IsOwner)
			{
				CurrentWeaponType = debugWeaponHolding;
				CurrentWeaponState = WeaponState.Idle;
				// InputHandler.instance.onShoot += Shoot;
				InputHandler.instance.onAimActivated += EnableAim;
				InputHandler.instance.onAimDeactivated += DisableAim;
				
				
				ReInitializeWeaponConfigData();
				WeaponHolderSpawnRpc();
			}
			WeaponHolderRef.OnValueChanged += ConfigureWeaponHolder;
			networkCurrentWeaponID.OnValueChanged += OnNetworkWeaponIDChanged;
			
			
			OnNetworkWeaponIDChanged(-1, networkCurrentWeaponID.Value);
		}
		
		public override void OnNetworkDespawn()
		{
			if (IsOwner)
			{
				
				//InputHandler.instance.onShoot -= Shoot;
				InputHandler.instance.onAimActivated -= EnableAim;
				InputHandler.instance.onAimDeactivated -= DisableAim;
			}
			WeaponHolderRef.OnValueChanged -= ConfigureWeaponHolder;
			networkCurrentWeaponID.OnValueChanged -= OnNetworkWeaponIDChanged;
		}
		
		private void Update()
		{
			if (IsOwner)
			{
				SwitchWeaponType(CurrentWeaponType);
				SwitchWeaponState(CurrentWeaponState); 
			}
			
		}
		#endregion
		
		//--------------------------------------------------------------------------------------------------------------

		public void ReInitializeWeaponConfigData()
		{
			if(weaponConfigsData.Count > MaxWeapons) return;
			
			weaponDataPool = new Dictionary<int, WeaponConfigDataSO>();
			foreach (var configData in weaponConfigsData)
			{
				weaponDataPool.Add(configData.weaponId, configData);
			}
		}

		
		

		#region --- Spawn Weapon Holder ---

		[Rpc(SendTo.Server, RequireOwnership = false)] // Server And Host
		private void WeaponHolderSpawnRpc()
		{
			if(weaponHolderPrefab == null) return;
			
			NetworkObject socketInstance = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(weaponHolderPrefab ,OwnerClientId); //
			socketInstance.name = $"WeaponHolder_{OwnerClientId}";
			socketInstance.transform.SetParent(transform);
			weaponHolder = socketInstance.transform;
			weaponHolderReference = new NetworkObjectReference(socketInstance);
			WeaponHolderRef.Value = new NetworkObjectReference(socketInstance);
			SetWeaponHolderClientRpc(weaponHolderReference);
		}

		[ClientRpc]
		private void SetWeaponHolderClientRpc(NetworkObjectReference weaponHolderRef)
		{
			if (weaponHolderRef.TryGet(out NetworkObject weaponHolderNetObj))
			{
				weaponHolder = weaponHolderNetObj.transform;
				weaponHolderNetObj.name = $"WeaponHolder_{OwnerClientId}";

				if (weaponConfigsData.Count > 0)
				{
					EquipWeaponID();
				}
			}
		}

	
		private void ConfigureWeaponHolder(NetworkObjectReference _, NetworkObjectReference current)
		{
			// Resolve the reference on the client
			if (current.TryGet(out NetworkObject netObj))
			{
				weaponHolder = netObj.transform;
				netObj.name = $"WeaponHolder_{OwnerClientId}";
				if (weaponConfigsData.Count > 0)
				{
					EquipWeaponID();
				}
			}
			
			
		}

		#endregion

		private void EquipWeaponID()
		{
			if(!IsOwner) return;
			
			switch (CurrentWeaponType)
			{
				case WeaponType.Unarmed:
					networkCurrentWeaponID.Value = -1;
					break;
				case WeaponType.Rifle:
					networkCurrentWeaponID.Value = 1001;
					break;
				case WeaponType.Pistol:
					networkCurrentWeaponID.Value = 2001;
					break;
				case WeaponType.Melee:
					networkCurrentWeaponID.Value = 3001;
					break;
			}
		}
		
		public WeaponConfigDataSO GetWeaponDataByID(int id)
		{
			if(id == -1) return null;
			return weaponConfigsData.Find(w => w.weaponId == id);
		}
		
		private void OnNetworkWeaponIDChanged(int previousID, int newID)
		{
			if(newID == -1) return;
			
			WeaponConfigDataSO data = GetWeaponDataByID(newID);
			currentWeaponConfigData = data;
			
			UpdateWeaponVisuals(data);
		}
		
		#region --- Spawn Weapon ---

		private void UpdateWeaponVisuals(WeaponConfigDataSO data)
		{
			if (currentWeaponObjectInstance && currentWeaponObjectInstance.GetComponent<NetworkObject>()) 
				currentWeaponObjectInstance.GetComponent<NetworkObject>().Despawn(true);
			
			currentWeaponObjectInstance = null;

			if (data != null && data.weaponPrefab && weaponHolder)
			{
				GenerateWeaponServerRpc();
			}
			
		}
		
		[Rpc(SendTo.Server)]
		private void GenerateWeaponServerRpc()
		{
			if (currentWeaponConfigData == null) return;
			
			currentWeaponObjectInstance = NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(currentWeaponConfigData.weaponPrefab, OwnerClientId).transform;
			currentWeaponObjectInstance.name = $"Weapon_{currentWeaponConfigData.weaponType}";
			currentWeaponObjectInstance.SetParent(weaponHolder);
			currentWeaponObjectInstance.localPosition = Vector3.zero;
			currentWeaponObjectInstance.localRotation = Quaternion.identity;

			NetworkObject weaponNetObj = currentWeaponObjectInstance.GetComponent<NetworkObject>();
			SetCurrentWeaponClientRpc(new NetworkObjectReference(weaponNetObj));

		}
		
		[ClientRpc]
		private void SetCurrentWeaponClientRpc(NetworkObjectReference weaponRef)
		{
			if (weaponRef.TryGet(out NetworkObject weaponNetObj))
			{
				currentWeaponObjectInstance = weaponNetObj.transform;
				weaponNetObj.transform.localPosition = Vector3.zero;
				weaponNetObj.transform.localRotation = Quaternion.identity;
			}
		}

		#endregion
		
		//--------------------------------------------------------------------------------------------------------------
		
		#region --- Weapon Type/State Change ---

		public void SetCurrentWeaponType(WeaponType _) => CurrentWeaponType = _;
		public void SetCurrentWeaponState(WeaponState _) => CurrentWeaponState = _;
		
		private void SwitchWeaponType(WeaponType weaponType)
		{
			WeaponTypeChange(weaponType);
		}
		
		private void SwitchWeaponState(WeaponState weaponState)
		{
			WeaponStateChange(weaponState);
		}

		public void WeaponTypeChange(WeaponType type)
		{
			switch (type)
			{
				case WeaponType.Unarmed:
					playerAnim.SetInteger(_weaponTypeHash, 0);
					break;
				case WeaponType.Rifle:
					playerAnim.SetInteger(_weaponTypeHash, 1);
					break;
				case WeaponType.Pistol:
					playerAnim.SetInteger(_weaponTypeHash, 2);
					break;
				case WeaponType.Melee:
					playerAnim.SetInteger(_weaponTypeHash, 3);
					break;
			}
		}
		
		public void WeaponStateChange(WeaponState _state)
		{
			switch (_state)
			{
				case WeaponState.Idle:
					OnAimAnim(false);
					
					break;
				case WeaponState.Firing:
					break;
				case WeaponState.Reloading:
					break;
				case WeaponState.Switching:
					break;
				case WeaponState.ADS:
					OnAimAnim(true);
					break;

			}
		}

		#endregion
		
		
		#region --- Aim ---
		
		public void OnAimAnim(bool enable)
		{
			playerAnim.SetBool(_aimingHash, enable);
		}

		private void EnableAim()
		{
			CurrentWeaponState = WeaponState.ADS;
			
		}

		private void DisableAim()
		{
			CurrentWeaponState = WeaponState.Idle;
		}

		#endregion
		
		
		
	}
	
}

