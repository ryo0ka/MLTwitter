using System;
using UniRx.Async;
using UnityEngine;

namespace Utils
{
	public static class UnityUtils
	{
		public static async UniTask Animate(float durationSecs, AnimationCurve curve, Action<float> onTick)
		{
			float startTime = Time.time;
			float time = 0;
			while (time < 1f)
			{
				time = (Time.time - startTime) / durationSecs;
				float t = Mathf.Clamp01(time);
				t = curve?.Evaluate(t) ?? t;
				onTick(t);

				await UniTask.Yield();
			}
		}

		public static void SetAnchoredPosition(this RectTransform t, float? x = null, float? y = null, float? z = null)
		{
			Vector3 p = t.anchoredPosition3D;
			p.x = x ?? p.x;
			p.y = y ?? p.y;
			p.z = z ?? p.z;
			t.anchoredPosition3D = p;
		}

		public static void SetLocalEulerAngles(this Transform t, float? x = null, float? y = null, float? z = null)
		{
			Vector3 p = t.localEulerAngles;
			p.x = x ?? p.x;
			p.y = y ?? p.y;
			p.z = z ?? p.z;
			t.localEulerAngles = p;
		}
	}
}