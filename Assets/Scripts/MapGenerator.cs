using System;
using UnityEngine;

[Serializable]
public enum DrawMode {NoiseMap, ColourMap};
public class MapGenerator : MonoBehaviour {
	[Tooltip("绘制模式")] public DrawMode drawMode;
	
	[Header("地图设置")]
	public int mapWidth;
	public int mapHeight;
	public float noiseScale;

	[Tooltip("八度音阶的数量")] public int octaves;
	[Range(0, 1), Tooltip("持久性值")] public float persistance;
	[Tooltip("间隙度值")] public float lacunarity;

	[Tooltip("种子")] public int seed;//如果 seed 是固定的，则每次运行程序时，生成的随机数序列都是一样的
	[Tooltip("向量偏移量")] public Vector2 offset;

	[Tooltip("是否自动更新")] public bool autoUpdate;

	[Tooltip("地形类型数组")] public TerrainType[] regions;
	
	public void GenerateMap() {
		float[,] noiseMap = Noise.GenerateNoiseMap (mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

		Color[] colourMap = new Color[mapWidth * mapHeight];//声明一维颜色地图
		for (int y = 0; y < mapHeight; y++) {
			for (int x = 0; x < mapWidth; x++) {
				float currentHeight = noiseMap [x, y];//获取当前地图点高度
				for (int i = 0; i < regions.Length; i++) {//遍历所有地形类型设置当前点的类型
					if (currentHeight <= regions [i].height) {//如果当前区域小于等于地形类型规定的高度就设置颜色地图
						colourMap [y * mapWidth + x] = regions [i].colour;
						break;
					}
				}
			}
		}

		//可视化展示
		MapDisplay display = FindObjectOfType<MapDisplay> ();
		if (drawMode == DrawMode.NoiseMap) {
			display.DrawTexture (TextureGenerator.TextureFromHeightMap(noiseMap));
		} else if (drawMode == DrawMode.ColourMap) {
			display.DrawTexture (TextureGenerator.TextureFromColourMap(colourMap, mapWidth, mapHeight));
		}
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

/// <summary>
/// 地形类型结构体
/// </summary>
[System.Serializable]
public struct TerrainType {
	[Tooltip("地形名称")] public string name;
	[Tooltip("地形高度")] public float height;
	[Tooltip("对应颜色")] public Color colour;
}
