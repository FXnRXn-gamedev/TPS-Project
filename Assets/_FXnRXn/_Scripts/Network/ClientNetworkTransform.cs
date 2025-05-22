using Unity.Netcode.Components;
using UnityEngine;

namespace FXnRXn
{
	public class ClientNetworkTransform : NetworkTransform
	{
		protected override bool OnIsServerAuthoritative()
		{
			return false;
		}
	}
}

