using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;


namespace FXnRXn
{
	public interface ISpreadProvider
	{
		float GetCurrentSpread();
		float GetMaxSpread();
		float GetImpulseSpread();
	}
	
	// Example Spread Provider Implementation
	public class MovementSpreadProvider : ISpreadProvider
	{
		public float GetCurrentSpread() => InputHandler.instance?.GetMovementInput().magnitude > 0 ||  InputHandler.instance?.GetLookInput().magnitude > 0 ? 5f : 0f; //PlayerController.Instance.IsSprinting ? 15f : 5f;
		public float GetMaxSpread() => 20f;
		public float GetImpulseSpread() => 10f;
	}

	public class ShootingSpreadProvider : ISpreadProvider
	{
		public float GetCurrentSpread() => 3f; //WeaponManager.CurrentWeapon?.CurrentSpread ?? 0f;
		public float GetMaxSpread() => 30f; //WeaponManager.CurrentWeapon?.MaxSpread ?? 30f;
		public float GetImpulseSpread() => 15f;//WeaponManager.CurrentWeapon?.RecoilSpread ?? 15f;
	}
	public class AdvancedCrosshairSystem : MonoBehaviour
	{
		public enum CrosshairElementType { Static, Dynamic, Rotating}
		public enum CrosshairState { Default, Shooting, Moving, Hit }
		public enum CrosshairShape { Classic, DotCircle, Circle, TShape }
		

		[System.Serializable]
		public struct CrosshairProfile
		{
			[Header("Crosshair Creation Settings :")] 
			public CrosshairElementType		crosshairElementType;
			public CrosshairShape			crosshairType;
			public int						crosshairSegments;
			public float					crosshairRadius;
			public float					crosshairSize;
			public float					crosshairYRatio;
			[Header("Behavior Settings")]
			public AnimationCurve			spreadCurve;
			public float					animationDuration;
			public float					rotationSpeed;
			public float					wobbleAmount;
			public float					maxSize;
			public float					sizeMultiplier;
			
			[Header("Visual Settings")]
			public Sprite					primaryCrosshairSprite;
			public Sprite					secondaryCrosshairSprite;
			public Color					defaultColor;
			public Color					hitColor;

		}

		#region Singleton 

		public static AdvancedCrosshairSystem instance { get; private set; }

		private void Awake()
		{
			if (instance == null) instance = this;

			OnAwake();
		}

		#endregion
		
		
		#region Variables

		[Header("References")] 
		[SerializeField] private CrosshairShape					targetCrosshairShape;
		[SerializeField] private RectTransform					crosshairContainer;
		[SerializeField] private List<CrosshairProfileData>		crosshairProfileData = new List<CrosshairProfileData>();
		
		[Header("Core Settings")] 
		[SerializeField] private CrosshairProfile				defaultProfile;
		
		
		private CrosshairProfile								currentProfile;
		private List<Image>										crosshairImageElements = new List<Image>();
		private float											currentSpread;
		private float											targetSpread;
		private float											animationTimer;
		private float											velocity;
		private CrosshairState									currentState;
		private Vector2[]										basePositions;
		private Vector2[]										baseScale;
		private Quaternion[]									baseRotations;
		
		
		private List<ISpreadProvider>							spreadProviders = new List<ISpreadProvider>();


		public CrosshairShape TargetCrosshairShape
		{
			get => targetCrosshairShape;
			set => targetCrosshairShape = value;
		}

		#endregion


		private void OnAwake()
		{
			// Set Crosshair shape and assign to default profile

			AssignCrosshairShapeDefaultData(TargetCrosshairShape);

			
		}

		private void Start()
		{
			currentProfile = defaultProfile;
			RegisterCoreProviders(CrosshairState.Moving);
			GenerateCrosshair(defaultProfile);
			SetState(CrosshairState.Moving);
		}

		private void Update()
		{
			UpdateCrosshairState();
			UpdateVisuals();
		}


		public void AssignCrosshairShapeDefaultData(CrosshairShape _assignedCrosshairShape)
		{
			switch (_assignedCrosshairShape)
			{
				case CrosshairShape.Classic:
					break;
				case CrosshairShape.DotCircle:
					foreach (var profileData in crosshairProfileData)
					{
						if (profileData.profile.crosshairType == _assignedCrosshairShape)
						{
							defaultProfile.crosshairElementType = profileData.profile.crosshairElementType;
							defaultProfile.crosshairType = profileData.profile.crosshairType;
							defaultProfile.crosshairSegments = profileData.profile.crosshairSegments;
							defaultProfile.crosshairRadius = profileData.profile.crosshairRadius;
							defaultProfile.crosshairSize = profileData.profile.crosshairSize;
							defaultProfile.crosshairYRatio = profileData.profile.crosshairYRatio;
							
							defaultProfile.spreadCurve = profileData.profile.spreadCurve;
							defaultProfile.animationDuration = profileData.profile.animationDuration;
							defaultProfile.rotationSpeed = profileData.profile.rotationSpeed;
							defaultProfile.wobbleAmount = profileData.profile.wobbleAmount;
							defaultProfile.maxSize = profileData.profile.maxSize;
							defaultProfile.sizeMultiplier = profileData.profile.sizeMultiplier;
							
							defaultProfile.secondaryCrosshairSprite = profileData.profile.secondaryCrosshairSprite;
							defaultProfile.primaryCrosshairSprite = profileData.profile.primaryCrosshairSprite;
							defaultProfile.defaultColor = profileData.profile.defaultColor;
							defaultProfile.hitColor = profileData.profile.hitColor;
						}
					}
					break;
				case CrosshairShape.Circle:
					foreach (var profileData in crosshairProfileData)
					{
						if (profileData.profile.crosshairType == _assignedCrosshairShape)
						{
							defaultProfile.crosshairElementType = profileData.profile.crosshairElementType;
							defaultProfile.crosshairType = profileData.profile.crosshairType;
							defaultProfile.crosshairSegments = profileData.profile.crosshairSegments;
							defaultProfile.crosshairRadius = profileData.profile.crosshairRadius;
							defaultProfile.crosshairSize = profileData.profile.crosshairSize;
							defaultProfile.crosshairYRatio = profileData.profile.crosshairYRatio;
							
							defaultProfile.spreadCurve = profileData.profile.spreadCurve;
							defaultProfile.animationDuration = profileData.profile.animationDuration;
							defaultProfile.rotationSpeed = profileData.profile.rotationSpeed;
							defaultProfile.wobbleAmount = profileData.profile.wobbleAmount;
							defaultProfile.maxSize = profileData.profile.maxSize;
							defaultProfile.sizeMultiplier = profileData.profile.sizeMultiplier;
							
							defaultProfile.secondaryCrosshairSprite = profileData.profile.secondaryCrosshairSprite;
							defaultProfile.primaryCrosshairSprite = profileData.profile.primaryCrosshairSprite;
							defaultProfile.defaultColor = profileData.profile.defaultColor;
							defaultProfile.hitColor = profileData.profile.hitColor;
						}
					}
					break;
				case CrosshairShape.TShape:
					foreach (var profileData in crosshairProfileData)
					{
						if (profileData.profile.crosshairType == _assignedCrosshairShape)
						{
							defaultProfile.crosshairElementType = profileData.profile.crosshairElementType;
							defaultProfile.crosshairType = profileData.profile.crosshairType;
							defaultProfile.crosshairSegments = profileData.profile.crosshairSegments;
							defaultProfile.crosshairRadius = profileData.profile.crosshairRadius;
							defaultProfile.crosshairSize = profileData.profile.crosshairSize;
							defaultProfile.crosshairYRatio = profileData.profile.crosshairYRatio;
							
							defaultProfile.spreadCurve = profileData.profile.spreadCurve;
							defaultProfile.animationDuration = profileData.profile.animationDuration;
							defaultProfile.rotationSpeed = profileData.profile.rotationSpeed;
							defaultProfile.wobbleAmount = profileData.profile.wobbleAmount;
							defaultProfile.maxSize = profileData.profile.maxSize;
							defaultProfile.sizeMultiplier = profileData.profile.sizeMultiplier;
							
							defaultProfile.secondaryCrosshairSprite = profileData.profile.secondaryCrosshairSprite;
							defaultProfile.primaryCrosshairSprite = profileData.profile.primaryCrosshairSprite;
							defaultProfile.defaultColor = profileData.profile.defaultColor;
							defaultProfile.hitColor = profileData.profile.hitColor;
						}
					}
					break;
			}
		}
		
		public void RegisterCoreProviders(CrosshairState _currentState)
		{
			switch (_currentState)
			{
				case CrosshairState.Default:
					break;
				case CrosshairState.Shooting:
					spreadProviders.Add(new ShootingSpreadProvider());
					break;
				case CrosshairState.Moving:
					spreadProviders.Add(new MovementSpreadProvider());
					break;
				case CrosshairState.Hit:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_currentState), _currentState, null);
			}
			
			
		}
		
		#region Create Crosshair
		public void GenerateCrosshair(CrosshairProfile profile)
		{
			ClearCrosshair();
			currentProfile = profile;
			CreateCrosshair();
			StoreBaseTransforms();
		}


		
		
		private void CreateCrosshair()
		{
			float angleStep = 360f / currentProfile.crosshairSegments;
			for (int i = 0; i < currentProfile.crosshairSegments; i++)
			{
				float angle = i * angleStep * Mathf.Deg2Rad;
				Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
				CreateCrosshairElement(direction , currentProfile.crosshairType);
			}
		}
		private void CreateCrosshairElement(Vector2 direction, CrosshairShape _crosshairShape)
		{
			switch (_crosshairShape)
			{
				case CrosshairShape.Classic:
					break;
				case CrosshairShape.DotCircle:
					CreatePrimaryCrosshair(direction);
					CreateSecondaryCrosshair(direction);
					break;
				case CrosshairShape.Circle:
					CreatePrimaryCrosshair(direction);
					break;
				case CrosshairShape.TShape:
					CreatePrimaryCrosshair(direction);
					break;
			}
			
		}


		private void CreatePrimaryCrosshair(Vector2 direction)
		{
			GameObject element = new GameObject("CrosshairElement");
			element.transform.SetParent(crosshairContainer);
			element.AddComponent<CanvasGroup>();
			
			Image img = element.AddComponent<Image>();
			img.sprite = currentProfile.primaryCrosshairSprite;
			img.color = currentProfile.defaultColor;
			img.rectTransform.sizeDelta = Vector2.one * currentProfile.crosshairSize * (1 / defaultProfile.crosshairSegments);
			// float tempX = direction.x < 0 ? 1 : direction.x;
			// float tempY = direction.y < 0 ? 1 : direction.y;
			//
			// img.rectTransform.sizeDelta = currentProfile.crosshairSize * (new Vector2(tempX, tempY) * (1 / defaultProfile.crosshairYRatio));

			if(direction != Vector2.zero)
			{
				img.rectTransform.anchoredPosition = direction * currentProfile.crosshairRadius;
			}
			crosshairImageElements.Add(img);
		}

		private void CreateSecondaryCrosshair(Vector2 direction)
		{
			GameObject element = new GameObject("CrosshairElement");
			element.transform.SetParent(crosshairContainer);
			element.AddComponent<CanvasGroup>();
			
			Image img = element.AddComponent<Image>();
			img.sprite = currentProfile.secondaryCrosshairSprite;
			img.color = currentProfile.defaultColor;
			img.rectTransform.sizeDelta = Vector2.one * (currentProfile.crosshairSize/ 4);

			if(direction != Vector2.zero)
			{
				img.rectTransform.anchoredPosition = direction * currentProfile.crosshairRadius;
			}
			//crosshairImageElements.Add(img);
		}
		private void ClearCrosshair()
		{
			foreach (var element in crosshairImageElements)
			{
				Destroy(element.gameObject);
			}
			crosshairImageElements.Clear();
		}

		private void StoreBaseTransforms()
		{
			basePositions = new Vector2[crosshairImageElements.Count];
			baseScale = new Vector2[crosshairImageElements.Count];
			baseRotations = new Quaternion[crosshairImageElements.Count];

			for (int i = 0; i < crosshairImageElements.Count; i++)
			{
				basePositions[i] = crosshairImageElements[i].rectTransform.anchoredPosition;
				baseRotations[i] = crosshairImageElements[i].rectTransform.localRotation;
				baseScale[i] = crosshairImageElements[i].rectTransform.sizeDelta;
			}
		}
		
		#endregion

		#region Spread
		
		private void UpdateCrosshairState()
		{
			// animationTimer += Time.deltaTime;
			// float curveTime = Mathf.Clamp01(animationTimer / currentProfile.animationDuration);
			// currentSpread = currentProfile.spreadCurve.Evaluate(curveTime);
			targetSpread = CalculateSpread();
			
			currentSpread = Mathf.SmoothDamp(currentSpread, targetSpread, ref velocity, currentProfile.animationDuration);
			currentSpread = Mathf.Clamp(currentSpread, 0, currentProfile.maxSize);
		}

		private float CalculateSpread()
		{
			float totalSpread = 0;
			foreach (var provider in spreadProviders)
			{
				totalSpread += provider.GetCurrentSpread();
			}
			return Mathf.Clamp(totalSpread, 0, currentProfile.maxSize);
		}

		#endregion


		

		private void UpdateVisuals()
		{
			
			for (int i = 0; i < crosshairImageElements.Count; i++)
			{
				float spreadRatio = currentSpread / currentProfile.maxSize;
				float evaluatedSpread = currentProfile.spreadCurve.Evaluate(spreadRatio);
				Vector2 spreadOffset = baseScale[i] * evaluatedSpread * currentProfile.sizeMultiplier;
				//spread
				switch (currentProfile.crosshairType)
				{
					case CrosshairShape.Classic:
						break;
					case CrosshairShape.Circle:
						crosshairImageElements[i].rectTransform.sizeDelta = baseScale[i] + spreadOffset;
						break;
					case CrosshairShape.DotCircle:
						crosshairImageElements[i].rectTransform.sizeDelta = baseScale[i] + spreadOffset;
						break;
					case CrosshairShape.TShape:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				// Vector2 spreadOffset = basePositions[i].normalized * currentSpread;
				// crosshairImageElements[i].rectTransform.anchoredPosition = basePositions[i] + spreadOffset;

				// Rotation
				// float rotationAmount = currentProfile.rotationSpeed * Time.deltaTime;
				// crosshairImageElements[i].rectTransform.localRotation = baseRotations[i] * Quaternion.Euler(0, 0, rotationAmount);
				
				// Wobble
				if (currentState == CrosshairState.Moving)
				{
					// Vector2 wobble = Random.insideUnitCircle * currentProfile.wobbleAmount;
					// crosshairImageElements[i].rectTransform.anchoredPosition += wobble;
				}
			}
		}
		
		public void SetState(CrosshairState newState)
		{
			if (currentState != newState)
			{
				currentState = newState;
				animationTimer = 0f;
			}
		}
		
	} // END
	
}

