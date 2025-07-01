using Unity.Netcode;
using UnityEngine;

namespace FXnRXn
{
	[CreateAssetMenu(menuName = "Game/Weapon/WeaponConfigDataSO")]
	public class WeaponConfigDataSO : ScriptableObject
	{
		public int					weaponId;
		public WeaponType			weaponType;
		public string				weaponName;
		public NetworkObject		weaponPrefab;
		public Vector3				weaponWorldOffset;
		public bool					isDualWield;
		public LODGroup				lodSetting;

		[Header("-------------		Weapon Hold Settings		-------------")] 
		[Space(10)]
		public bool									ShowWeaponHoldSetting;
		
		[ShowIf("ShowWeaponHoldSetting", true)]
		public Vector3								hipPosition;
		[ShowIf("ShowWeaponHoldSetting", true)]
		public Vector3								hipRotation;
		[ShowIf("ShowWeaponHoldSetting", true)]
		public Vector3								adsPosition;
		[ShowIf("ShowWeaponHoldSetting", true)]
		public Vector3								adsRotation;
		[ShowIf("ShowWeaponHoldSetting", true)]
		public Vector3								hipOffset = new Vector3(0f, 0.05f, 0.1f);
		[ShowIf("ShowWeaponHoldSetting", true)]
		public Vector3								aimSightOffset = new Vector3(0, 0.05f, 0.1f);

		[Header("-------------		Recoil Settings		-------------")]
		[Space(10)]
		public bool									ShowRecoilSetting;
		
		[ShowIf("ShowRecoilSetting", true)]
		public Vector3								recoilPositionForce = new Vector3(0, 0.01f, -0.05f); // Local space for weapon holder
		[ShowIf("ShowRecoilSetting", true)]
		public Vector3								recoilRotationForce = new Vector3(-5f, 0, 0); // Euler angles for weapon holder
		[ShowIf("ShowRecoilSetting", true)]
		public float								recoilDuration = 0.1f;
		[ShowIf("ShowRecoilSetting", true)]
		public float								recoilRecoverySpeed = 10f;
	}
}

