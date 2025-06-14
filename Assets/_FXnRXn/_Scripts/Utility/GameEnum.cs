using UnityEngine;

[System.Serializable]
public enum WeaponType { Unarmed, Rifle, Pistol, Melee }

[System.Serializable]
public enum WeaponState
{
	Idle,
	Firing,
	Reloading,
	Switching,
	ADS
}