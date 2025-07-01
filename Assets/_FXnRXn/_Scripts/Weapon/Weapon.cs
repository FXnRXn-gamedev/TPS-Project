using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FXnRXn
{
	[RequireComponent(typeof(NetworkObject), typeof(AudioSource))]
	public class Weapon : NetworkBehaviour
	{
		public static Weapon instance { get; private set; }

		private void Awake() => instance = this;
		

		#region Variable
		[Header("-------------		Weapon Settings		-------------")]
		[Space(25)]
		public WeaponConfigDataSO						weaponData;
		public WeaponConfig								weapon;
		
		
		[SerializeField] private int					currentClip;
		[SerializeField] private bool					isReloading;
		private float									nextFireTime;
		private int										burstShotsRemaining;
		private AudioSource								audioSource;
		
		#endregion


		public  void Start()
		{
			audioSource = GetComponent<AudioSource>();
			currentClip = weapon.maxClipSize;
			burstShotsRemaining = weapon.burstCount;
		}




		public void TryFireWeapon()
		{

			if (Time.time < nextFireTime || isReloading)
			{
				return;
			}
			
			if(currentClip > 0)
			{
				switch (weapon.fireMode)
				{
					case FireMode.Single:
						FireSingleShotRpc(weapon.projectileSpawnPoint.position, weapon.projectileSpawnPoint.rotation);
						nextFireTime = Time.time + weapon.fireRate;
						
						break;
					case FireMode.Burst:
						break;
					case FireMode.Auto:
						break;
					case FireMode.SemiAuto:
						break;
				}
			}
			else
			{
				//StartReloadRpc();
			}

		}

		
		#region Fire Server/Client Sync


		[Rpc(SendTo.ClientsAndHost)] // To Server & Client
		private void FireSingleShotRpc(Vector3 origin, Quaternion direction)
		{
			
			if ( weapon.projectileSpawnPoint) //weapon.projectilePrefab &&
			{
				// string poolName = "AR Bullet";
				// NetworkObject projectileObj = NetworkedPoolManager.Instance.GetFromPool(
				// 	PoolType.Projectile, 
				// 	poolName,
				// 	origin,
				// 	direction,
				// 	OwnerClientId
				// );
				// if (projectileObj.GetComponent<ProjectileAR>())
				// {
				// 	projectileObj.GetComponent<ProjectileAR>().ProjectileVisualState(true);
				// }
				//
				// if (projectileObj.TryGetComponent<ProjectileAR>(out var projectile))
				// {
				// 	projectile.Initialize(weapon.projectileData , GetProjectileDirection());
				// }
			}
		}

		
		#endregion


		private Vector3 GetProjectileDirection()
		{
			if (CameraController.instance == null) 
			{
				return Vector3.zero;
			}
			Vector3 origin = GetAimTargetPoint();//CameraController.instance.GetCameraPosition()
			
			Vector3 spread = new Vector3(
				Random.Range(-weapon.weaponSpread, weapon.weaponSpread),
				Random.Range(-weapon.weaponSpread, weapon.weaponSpread),
				0
			);
			Vector3 rand = Random.insideUnitCircle * spread;
			origin += rand;
			
			Vector3 offsetPosition = origin + CameraController.instance.GetCameraForward();
			Vector3 direction = offsetPosition - CameraController.instance.GetCameraPosition();
			//Vector3 direction = GetAimTargetPoint();
			return direction;
		}

		private Vector3 GetAimTargetPoint()
		{
			Ray ray = CameraController.instance.GetMainCamera().ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
			
			// if (Physics.Raycast(ray, out RaycastHit hit, 100f, weapon.projectileData.collisionMask))
			// {
			// 	return hit.point;
			// }
			return ray.GetPoint(100f);
		}
		
		
		[Rpc(SendTo.Server)]
		private void ConsumeAmmoRpc()
		{
			currentClip--;
			//WeaponManager.Instance.UpdateAmmoUI(currentClip, config.maxClipSize);
		}

		#region Reload

		
		[Rpc(SendTo.Server)]
		public void StartReloadRpc()
		{
			StartCoroutine(ReloadRpc());
		}
		
		private IEnumerator ReloadRpc()
		{
			if (isReloading || currentClip == weapon.maxClipSize) yield break;
        
			isReloading = true;
			yield return new WaitForSeconds(weapon.reloadTime);
        
			currentClip = weapon.maxClipSize;
			isReloading = false;
			
		}
		#endregion
	}
	
	public enum FireMode
	{
		Single,
		Burst,
		Auto,
		SemiAuto
	}

	public enum Bullet
	{
		Projectile,
		Ray
	}
	
	[System.Serializable]
	public class WeaponConfig
	{
		public string					weaponName;
		public int						damagePerShot = 10;
		public float					fireRate = 0.15f;
		public int						burstCount = 3;
		public int						maxClipSize = 30;
		public float					reloadTime = 2f;
		public float					weaponSpread = 0.1f;
		public FireMode					fireMode;
		//public NetworkObject			projectilePrefab;
		//public ProjectileDataSO			projectileData;
		public Transform				projectileSpawnPoint;
		public Animator					weaponAnimator;
		public ParticleSystem			muzzleFlash;
		public AudioClip				fireSound;
		public AudioClip				reloadSound;
	}
}

