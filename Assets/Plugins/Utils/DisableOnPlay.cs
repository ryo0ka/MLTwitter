using UnityEngine;

namespace Plugins.Utils
{
	public class DisableOnPlay : MonoBehaviour
	{
		[SerializeField]
		Behaviour[] _components;

		void Start()
		{
			foreach (var c in _components)
			{
				c.enabled = false;
			}
		}
	}
}