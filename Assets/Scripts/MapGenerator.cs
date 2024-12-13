using UnityEngine;

public class MapGenerator : MonoBehaviour {

	public int mapWidth;
	public int mapHeight;
	public float noiseScale;

	[Tooltip("八度音阶的数量")] public int octaves;
	[Range(0, 1), Tooltip("持久性值")] public float persistance;
	[Tooltip("间隙度值")] public float lacunarity;

	[Tooltip("种子")] public int seed;//如果 seed 是固定的，则每次运行程序时，生成的随机数序列都是一样的
	[Tooltip("向量偏移量")] public Vector2 offset;

	public bool autoUpdate;

	public void GenerateMap() {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);


		MapDisplay display = FindObjectOfType<MapDisplay> ();
		display.DrawNoiseMap (noiseMap);
	}

	//使用验证方法控制值，该方法会在脚本变量值在inspector中改变时自动调用
	void OnValidate() {
		//地图宽高始终大于等于1
		if (mapWidth < 1) {
			mapWidth = 1;
		}
		if (mapHeight < 1) {
			mapHeight = 1;
		}
		//间隙度始终大于等于1
		if (lacunarity < 1) {
			lacunarity = 1;
		}
		//八度音调层数始终大于等于0
		if (octaves < 0) {
			octaves = 0;
		}
	}

}
