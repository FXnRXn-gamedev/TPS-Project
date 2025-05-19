using Unity.Netcode.Components;
using UnityEngine;

namespace FXnRXn
{
	public class ClientNetworkAnimator : NetworkAnimator
	{
		protected override bool OnIsServerAuthoritative()
		{
			return false;
		}
	}
}

