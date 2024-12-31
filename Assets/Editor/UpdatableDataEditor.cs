using Data;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	/// <summary>
	/// 可编辑数据的自定义编辑器类型，true表示功能对派生类也有效
	/// </summary>
	[CustomEditor (typeof(UpdatableData), true)]
	public class UpdatableDataEditor : UnityEditor.Editor {

		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI ();

			UpdatableData data = (UpdatableData)target;

			if (GUILayout.Button ("Update")) {
				data.NotifyOfUpdatedValues();
				EditorUtility.SetDirty (target);//确保按下按钮能够更新数据
			}
		}
	
	}
}
