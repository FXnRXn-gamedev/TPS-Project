using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace FXnRXn
{
	public class UIOverlapDetector : MonoBehaviour
	{
		#region Singleton

		public static UIOverlapDetector instance { get; private set; }

		private void Awake()
		{
			if (instance == null) instance = this;
		}

		#endregion

		#region Variables

		private bool _isOverlapping;

		public bool IsOverlapping
		{
			get => _isOverlapping;
			set => _isOverlapping = value;
		} 
		

		private RectTransform					rectTransform1;
		private RectTransform					rectTransform2;
		private bool							previousOverlapState;
		private Vector3[]						prevCorners1 = new Vector3[4];
		private Vector3[]						prevCorners2 = new Vector3[4];

		#endregion


		public void Init(Image _img1, Image _img2)
		{
			rectTransform1 = _img1.rectTransform;
			rectTransform2 = _img2.rectTransform;
			
			rectTransform1.GetWorldCorners(prevCorners1);
			rectTransform2.GetWorldCorners(prevCorners2);

			IsOverlapping = false;
		}

		private void FixedUpdate()
		{
			if (rectTransform1 == null || rectTransform2 == null) return;
			
			bool moved = HasMoved(rectTransform1, prevCorners1) || 
			             HasMoved(rectTransform2, prevCorners2);

			if (moved)
			{
				IsOverlapping = AreUIElementsOverlapping(rectTransform1, rectTransform2);
			}
			
		}
		
		private bool HasMoved(RectTransform rect, Vector3[] previousCorners)
		{
			Vector3[] currentCorners = new Vector3[4];
			rect.GetWorldCorners(currentCorners);

			for (int i = 0; i < 4; i++)
			{
				if (!ApproximatelyEqual(currentCorners[i], previousCorners[i]))
				{
					return true;
				}
			}
			return false;
		}

		private bool ApproximatelyEqual(Vector3 a, Vector3 b)
		{
			return Mathf.Approximately(a.x, b.x) && 
			       Mathf.Approximately(a.y, b.y) && 
			       Mathf.Approximately(a.z, b.z);
		}
		
		public bool AreUIElementsOverlapping(RectTransform rect1, RectTransform rect2)
		{
			if (!QuickDistanceCheck(rect1, rect2))
			{
				return false;
			}
			
			Rect rect1ScreenSpace = GetScreenSpaceRect(rect1);
			Rect rect2ScreenSpace = GetScreenSpaceRect(rect2);
			if (rect1ScreenSpace.Overlaps(rect2ScreenSpace))
			{
				return true;
			}
			else
			{
				return false;
			}

		}
		
		private Rect GetScreenSpaceRect(RectTransform rectTransform)
		{
			Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
			Camera renderCamera = GetRenderCameraForCanvas(canvas);

			Vector3[] corners = new Vector3[4];
			rectTransform.GetWorldCorners(corners);

			Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
			Vector2 max = new Vector2(float.MinValue, float.MinValue);

			foreach (Vector3 corner in corners)
			{
				Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(renderCamera, corner);
				min = Vector2.Min(min, screenPoint);
				max = Vector2.Max(max, screenPoint);
			}

			return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
		}
		
		private Camera GetRenderCameraForCanvas(Canvas canvas)
		{
			if (canvas == null) return null;
			return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
		}
		
		private bool QuickDistanceCheck(RectTransform rect1, RectTransform rect2)
		{
			// Get screen space centers
			Vector2 center1 = GetScreenSpaceCenter(rect1);
			Vector2 center2 = GetScreenSpaceCenter(rect2);

			// Calculate approximate radii
			float radius1 = GetApproximateRadius(rect1);
			float radius2 = GetApproximateRadius(rect2);

			// Check if distance between centers is greater than sum of radii
			float distance = Vector2.Distance(center1, center2);
			return distance <= (radius1 + radius2);
		}
		
		private Vector2 GetScreenSpaceCenter(RectTransform rectTransform)
		{
			Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
			Camera renderCamera = GetRenderCameraForCanvas(canvas);

			// Get world position of rect center (accounts for pivot position)
			Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
			return RectTransformUtility.WorldToScreenPoint(renderCamera, worldCenter);
		}

		private float GetApproximateRadius(RectTransform rectTransform)
		{
			// Calculate approximate radius based on rect size and scale
			Vector2 size = rectTransform.rect.size;
			Vector3 scale = rectTransform.lossyScale;
        
			float scaledWidth = size.x * scale.x;
			float scaledHeight = size.y * scale.y;
        
			// Return half of the diagonal length
			return Mathf.Sqrt(
				Mathf.Pow(scaledWidth / 2f, 2f) + 
				Mathf.Pow(scaledHeight / 2f, 2f)
			) * 0.85f; // Add safety factor for rotation
		}
	}
}

