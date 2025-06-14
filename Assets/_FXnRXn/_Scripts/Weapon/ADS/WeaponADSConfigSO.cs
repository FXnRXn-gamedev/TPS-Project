using UnityEngine;

namespace FXnRXn
{
	[CreateAssetMenu(menuName = "Game/Weapon/ADS Config")]
	public class WeaponADSConfigSO : ScriptableObject
	{
		public WeaponType					weaponType;
		public Vector3						adsCameraOffset;
		[Range(0.1f, 2.0f)]public float		adsSensitivityMultiplier = 1f;
		public Vector3						rotationOffset;
	}
}

