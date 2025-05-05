using UnityEngine;

namespace FXnRXn
{
	[CreateAssetMenu(fileName = "Crosshair Profile Data", menuName = "Game/Weapon/Crosshair/CrosshairProfile")]
	public class CrosshairProfileData : ScriptableObject
	{
		public AdvancedCrosshairSystem.CrosshairProfile profile;
	}
}

