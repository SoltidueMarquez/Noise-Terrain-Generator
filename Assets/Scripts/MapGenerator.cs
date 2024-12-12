using UnityEngine;

/// <summary>
/// 地图生成器
/// </summary>
public class MapGenerator : MonoBehaviour {

	public int mapWidth;
	public int mapHeight;
	public float noiseScale;

	public bool autoUpdate;

	public void GenerateMap() {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapWidth, mapHeight, noiseScale);
		
		MapDisplay display = FindObjectOfType<MapDisplay> ();
		display.DrawNoiseMap (noiseMap);
	}
	
}
