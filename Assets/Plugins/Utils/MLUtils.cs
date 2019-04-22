using System;
using UniRx;
using UniRx.Async;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;

namespace Utils
{
	public static class MLUtils
	{
		public static async UniTask RequestPrivilege(MLPrivilegeId privilege)
		{
			// Don't do privilege if app is running in neither ML device nor ZI mode
			if (!XRDevice.isPresent)
			{
				return;
			}

			MLPrivileges.Start().ThrowIfFail();

			MLResult? result = null;
			MLPrivileges.RequestPrivilegeAsync(privilege, (r, _) =>
			{
				result = r;
			}).ThrowIfFail();

			await UniTask.WaitUntil(() => result.HasValue);

			result?.ThrowIfFail();
		}

		public static void ThrowIfFail(this MLResult result)
		{
			if (result.IsOk || result.Code == MLResultCode.PrivilegeGranted)
			{
				return;
			}

			throw new Exception($"IsOK: {result.IsOk}, Code: {result.Code}");
		}

		public static IObservable<Unit> OnTriggerUpAsObservable(KeyCode? editorKey = null)
		{
			var controller = Observable.FromEvent<Action<byte, float>, Unit>(
				f => (_, __) => f(Unit.Default),
				h => MLInput.OnTriggerUp += h,
				h => MLInput.OnTriggerUp -= h);

			// Support secondary input on keyboard if app is running in Editor
			if (Application.isEditor)
			{
				var key = editorKey ?? KeyCode.Space;
				var keyboard = Observable.EveryUpdate()
				                         .Where(_ => Input.GetKeyUp(key))
				                         .AsUnitObservable();

				controller = controller.Merge(keyboard);
			}

			return controller;
		}

		public static IObservable<string> OnCaptureCompletedAsObservable()
		{
			return Observable.FromEvent<Action<MLCameraResultExtras, string>, string>(
				f => (_, path) => f(path),
				h => MLCamera.OnCaptureCompleted += h,
				h => MLCamera.OnCaptureCompleted -= h);
		}

		public static IObservable<Unit> OnVideoPreparedAsObservable(this MLMediaPlayer self)
		{
			return Observable.FromEvent(
				h => self.OnVideoPrepared += h,
				h => self.OnVideoPrepared -= h);
		}

		public static IObservable<Unit> OnMediaErrorAsObservable(this MLMediaPlayer self)
		{
			return Observable.FromEvent<Action<MLResultCode, string>, (MLResultCode, string)>(
				f => (c, m) => f((c, m)),
				h => self.OnMediaError += h,
				h => self.OnMediaError -= h).Select<(MLResultCode, string), Unit>((c, m) =>
			{
				throw new Exception($"MediaError: {c}: {m}");
			});
		}
	}
}