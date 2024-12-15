using UnityEngine;

/// <summary>
/// 网格生成器
/// </summary>
public static class MeshGenerator {
	/// <summary>
	/// 生成地形网格数据的静态方法
	/// </summary>
	/// <param name="heightMap">高度图</param>
	/// <returns></returns>
	public static MeshData GenerateTerrainMesh(float[,] heightMap) {
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);
		float topLeftX = (width - 1) / -2f;
		float topLeftZ = (height - 1) / 2f;

		MeshData meshData = new MeshData (width, height);//网格数据
		int vertexIndex = 0;//顶点索引
		
		//遍历高度图，计算三角形
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {

				meshData.vertices [vertexIndex] = new Vector3 (topLeftX + x, heightMap [x, y], topLeftZ - y);//传入顶点参数，做数学处理保证网格居中显示
				meshData.uvs [vertexIndex] = new Vector2 (x / (float)width, y / (float)height);//传入uv参数
				//为顶点设置三角形(右下，三角形*2，因此右边与底部的点不需要考虑创建三角形)
				if (x < width - 1 && y < height - 1) {
					meshData.AddTriangle (vertexIndex, vertexIndex + width + 1, vertexIndex + width);
					meshData.AddTriangle (vertexIndex + width + 1, vertexIndex, vertexIndex + 1);
				}

				vertexIndex++;//顶点索引后移
			}
		}

		return meshData;
	}
}

/// <summary>
/// 网格数据类
/// </summary>
public class MeshData
{
	[Tooltip("顶点数组")] public Vector3[] vertices;
	[Tooltip("三角形数组")] public int[] triangles;
	[Tooltip("uv映射数组")]public Vector2[] uvs;

	[Tooltip("当前的三角形顶点索引")] int triangleIndex;

	/// <summary>
	/// 构造函数
	/// </summary>
	/// <param name="meshWidth">网格宽度</param>
	/// <param name="meshHeight">网格高度</param>
	public MeshData(int meshWidth, int meshHeight) {
		vertices = new Vector3[meshWidth * meshHeight];//初始化顶点数组
		uvs = new Vector2[meshWidth * meshHeight];
		triangles = new int[(meshWidth-1)*(meshHeight-1)*6];//三角形数组的大小是（网格宽度-1）*（网格高度-1）*6
	}
	
	/// <summary>
	/// 添加三角形
	/// </summary>
	/// <param name="a">顶点a</param>
	/// <param name="b">顶点b</param>
	/// <param name="c">顶点c</param>
	public void AddTriangle(int a, int b, int c) {
		triangles [triangleIndex] = a;
		triangles [triangleIndex + 1] = b;
		triangles [triangleIndex + 2] = c;
		triangleIndex += 3;
	}

	/// <summary>
	/// 创建网格
	/// </summary>
	/// <returns></returns>
	public Mesh CreateMesh() {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.RecalculateNormals ();//便于光照效果呈现
		return mesh;
	}
}