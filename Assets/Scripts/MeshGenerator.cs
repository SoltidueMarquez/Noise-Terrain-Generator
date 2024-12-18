using UnityEngine;

/// <summary>
/// 网格生成器
/// </summary>
public static class MeshGenerator {
	/// <summary>
	/// 生成地形网格数据的静态方法
	/// </summary>
	/// <param name="heightMap">高度图</param>
	/// <param name="heightMultiplier">高度乘数</param>
	/// <param name="heightCurve">不同高度收乘数影响的程度曲线</param>
	/// <param name="levelOfDetail">LOD层数</param>
	/// <returns></returns>
	public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail) {
		AnimationCurve heightCurve = new AnimationCurve (_heightCurve.keys);//创建一个新的动画曲线，让线程们可以正常访问
		
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);
		float topLeftX = (width - 1) / -2f;
		float topLeftZ = (height - 1) / 2f;

		int meshSimplificationIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2;//网格简化层次就是LOD系数乘以2(为0时步长为1)，作为顶点的迭代步数
		int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;//计算每一行的顶点数
		
		MeshData meshData = new MeshData (verticesPerLine, verticesPerLine);//网格数据
		int vertexIndex = 0;//顶点索引
		
		//遍历高度图，计算三角形
		for (int y = 0; y < height; y += meshSimplificationIncrement) {
			for (int x = 0; x < width; x += meshSimplificationIncrement) {
				meshData.vertices [vertexIndex] = new Vector3 (topLeftX + x, heightCurve.Evaluate (heightMap [x, y]) * heightMultiplier, topLeftZ - y);//传入顶点参数，做数学处理保证网格居中显示
				meshData.uvs [vertexIndex] = new Vector2 (x / (float)width, y / (float)height);//传入uv参数
				//为顶点设置三角形(右下，三角形*2，因此右边与底部的点不需要考虑创建三角形)
				if (x < width - 1 && y < height - 1) {
					meshData.AddTriangle (vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
					meshData.AddTriangle (vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
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