﻿using UnityEngine;

namespace Data
{
	/// <summary>
	/// 可自动更新的数据
	/// </summary>
	public class UpdatableData : ScriptableObject {

		public event System.Action onValuesUpdated;
		[Tooltip("是否自动更新")] public bool autoUpdate;

#if UNITY_EDITOR
		/// <summary>
		/// 当检查面板的值发生变化时会调用的函数
		/// </summary>
		protected virtual void OnValidate() {
			if (autoUpdate) {
				UnityEditor.EditorApplication.update += NotifyOfUpdatedValues;
			}
		}

		public void NotifyOfUpdatedValues() {
			UnityEditor.EditorApplication.update -= NotifyOfUpdatedValues;
			onValuesUpdated?.Invoke();
		}
#endif
	}
}
