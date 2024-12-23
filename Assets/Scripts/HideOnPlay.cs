using UnityEngine;

/// <summary>
/// 播放时隐藏
/// </summary>
public class HideOnPlay : MonoBehaviour {

	// Use this for initialization
	void Start () {
		gameObject.SetActive (false);
	}
}
