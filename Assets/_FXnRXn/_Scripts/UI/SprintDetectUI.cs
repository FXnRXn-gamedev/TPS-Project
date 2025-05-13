using System;
using UnityEngine;
using UnityEngine.UI;

namespace FXnRXn
{
	public class SprintDetectUI : MonoBehaviour
	{
		#region Variables
		[Header("Settings")]
		[SerializeField] private bool isOn = false;


		private Image image;

		#endregion


		private void Start()
		{
			image = GetComponent<Image>();
			image.CrossFadeAlpha(0.5f, 0.5f, true);
		}

		private void Update()
		{
			// if (InputHandler.instance.GetMovementInput().magnitude > 0f)
			// {
			// 	InputHandler.instance.IsRunning 
			// }
			// else
			// {
			// 	InputHandler.instance.IsRunning = false;
			// }
			
		}

		public void Clicked()
		{
			isOn = !isOn;
			InputHandler.instance.IsRunning = isOn;
			float a = isOn ? 1f : 0.5f;
			image.color = new Color(image.color.r, image.color.g, image.color.b, a);

			if (isOn)
			{
				PlayerMovementController.instance.ActivateSprint();
			}
			else
			{
				PlayerMovementController.instance.DeactivateSprint();
			}
		}
	}
}

