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
		
		int meshSimplificationIncrement = (levelOfDetail == 0)?1:levelOfDetail * 2;//网格简化增量，就是LOD系数乘以2(为0时步长为1)，作为顶点的迭代步数
		
		int borderedSize = heightMap.GetLength (0);//边界大小
		int meshSize = borderedSize - 2 * meshSimplificationIncrement;//网格大小
		int meshSizeUnsimplified = borderedSize - 2;//未简化的网格大小尺寸
		
		float topLeftX = (meshSizeUnsimplified - 1) / -2f;
		float topLeftZ = (meshSizeUnsimplified - 1) / 2f;
		
		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1; //计算每一行的顶点数
		
		MeshData meshData = new MeshData (verticesPerLine);//网格数据
		
		int[][] vertexIndicesMap = new int[borderedSize][];//顶点索引映射
		for (int index = 0; index < borderedSize; index++)
		{
			vertexIndicesMap[index] = new int[borderedSize];
		}

		int meshVertexIndex = 0;//网格顶点索引
		int borderVertexIndex = -1;//边界顶点索引
		
		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
				bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;//判断是否是边界顶点
				if (isBorderVertex) {
					vertexIndicesMap[x][y] = borderVertexIndex;//设置为边界索引
					borderVertexIndex--;
				} else {
					vertexIndicesMap[x][y] = meshVertexIndex;//设置为顶点索引
					meshVertexIndex++;
				}
			}
		}
		
		//遍历高度图，计算三角形
		for (int y = 0; y < borderedSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderedSize; x += meshSimplificationIncrement) {
				int vertexIndex = vertexIndicesMap[x][y];//获取顶点索引
				Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);//计算uv，减去简化增量确保uv正确居中
				float height = heightCurve.Evaluate (heightMap [x, y]) * heightMultiplier;//计算高度
				Vector3 vertexPosition = new Vector3 (topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

				meshData.AddVertex (vertexPosition, percent, vertexIndex);//为网格数据添加顶点信息
				
				//为顶点设置三角形(右下，三角形*2，因此右边与底部的点不需要考虑创建三角形)，与顶点索引映射一起起效
				if (x < borderedSize - 1 && y < borderedSize - 1) {
					int a = vertexIndicesMap[x][y];																//a是(x,y)处的顶点索引映射
					int b = vertexIndicesMap[x + meshSimplificationIncrement][y];								//b是(x+网格简化增量,y)处的顶点索引映射
					int c = vertexIndicesMap[x][y + meshSimplificationIncrement];								//c是(x,y+网格简化增量)处的顶点索引映射
					int d = vertexIndicesMap[x + meshSimplificationIncrement][y + meshSimplificationIncrement];	//d是(x+网格简化增量,y+网格简化增量)处的顶点索引映射
					meshData.AddTriangle (a,d,c);//添加两个三角形
					meshData.AddTriangle (d,a,b);
				}
			}
		}

		meshData.BakeNormals();//在线程中计算每个地形块的法线数组
		
		return meshData;
	}
}

/// <summary>
/// 网格数据类
/// </summary>
public class MeshData
{
	[Tooltip("顶点数组")] Vector3[] vertices;
	[Tooltip("三角形数组")] int[] triangles;
	[Tooltip("uv映射数组")] Vector2[] uvs;
	[Tooltip("烘培的法线")] Vector3[] bakedNormals;
	
	Vector3[] borderVertices;//边界顶点数组
	int[] borderTriangles;//边界三角形
	
	[Tooltip("当前的三角形顶点索引")] int triangleIndex;
	[Tooltip("当前的边界三角形顶点索引")] int borderTriangleIndex;

	/// <summary>
	/// 构造函数
	/// </summary>
	/// <param name="verticesPerLine">每行的顶点数</param>
	public MeshData(int verticesPerLine) {
		vertices = new Vector3[verticesPerLine * verticesPerLine];//初始化顶点数组
		uvs = new Vector2[verticesPerLine * verticesPerLine];
		triangles = new int[(verticesPerLine-1)*(verticesPerLine-1)*6];//三角形数组的大小是（网格宽度-1）*（网格高度-1）*6
		
		borderVertices = new Vector3[verticesPerLine * 4 + 4];//边界顶点的数组大小是每行的顶点数*4再+4
		borderTriangles = new int[24 * verticesPerLine];//为了确保边界部分可以正确渲染
	}
	
	/// <summary>
	/// 用于添加一个顶点及其UV坐标到网格数据中
	/// </summary>
	/// <param name="vertexPosition">顶点位置</param>
	/// <param name="uv">顶点uv</param>
	/// <param name="vertexIndex">顶点索引</param>
	public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) {
		if (vertexIndex < 0) {//如果vertexIndex为负，说明这是一个边界顶点，其位置会被存储在borderVertices数组中。
			borderVertices [-vertexIndex - 1] = vertexPosition;
		} else {//如果vertexIndex为非负，顶点和UV坐标会被添加到vertices和uvs数组中对应的位置。
			vertices [vertexIndex] = vertexPosition;
			uvs [vertexIndex] = uv;
		}
	}	
	
	/// <summary>
	/// 添加三角形
	/// </summary>
	/// <param name="a">顶点a</param>
	/// <param name="b">顶点b</param>
	/// <param name="c">顶点c</param>
	public void AddTriangle(int a, int b, int c) {
		if (a < 0 || b < 0 || c < 0) {//如果构成三角形的顶点中有边界顶点，则加入边界三角形数组中
			borderTriangles [borderTriangleIndex] = a;
			borderTriangles [borderTriangleIndex + 1] = b;
			borderTriangles [borderTriangleIndex + 2] = c;
			borderTriangleIndex += 3;
		} else {//如果构成三角形的顶点中没有边界顶点，则加入常规三角形数组中
			triangles [triangleIndex] = a;
			triangles [triangleIndex + 1] = b;
			triangles [triangleIndex + 2] = c;
			triangleIndex += 3;
		}
	}

	/// <summary>
	/// 计算法线
	/// </summary>
	/// <returns></returns>
	Vector3[] CalculateNormals() {
		//声明顶点法线数组
		Vector3[] vertexNormals = new Vector3[vertices.Length];
		//遍历所有常规三角形
		int triangleCount = triangles.Length / 3;//得到三角形的数量
		for (int i = 0; i < triangleCount; i++) {
			int normalTriangleIndex = i * 3;//获得当前三角形的顶点
			int vertexIndexA = triangles [normalTriangleIndex];
			int vertexIndexB = triangles [normalTriangleIndex + 1];
			int vertexIndexC = triangles [normalTriangleIndex + 2];
			//传入顶点索引得到法线，将法线添加到顶点法线数组的对应顶点上
			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals [vertexIndexA] += triangleNormal;
			vertexNormals [vertexIndexB] += triangleNormal;
			vertexNormals [vertexIndexC] += triangleNormal;
		}
		//遍历所有边界三角形
		int borderTriangleCount = borderTriangles.Length / 3;
		for (int i = 0; i < borderTriangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = borderTriangles [normalTriangleIndex];
			int vertexIndexB = borderTriangles [normalTriangleIndex + 1];
			int vertexIndexC = borderTriangles [normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
			if (vertexIndexA >= 0) {
				vertexNormals [vertexIndexA] += triangleNormal;
			}
			if (vertexIndexB >= 0) {
				vertexNormals [vertexIndexB] += triangleNormal;
			}
			if (vertexIndexC >= 0) {
				vertexNormals [vertexIndexC] += triangleNormal;
			}
		}

		//对顶点法线数组的每个法线都归一化然后返回
		for (int i = 0; i < vertexNormals.Length; i++) {
			vertexNormals [i].Normalize ();
		}
		return vertexNormals;
	}

	/// <summary>
	/// 计算表面法线向量
	/// </summary>
	/// <param name="indexA"></param>
	/// <param name="indexB"></param>
	/// <param name="indexC"></param>
	/// <returns></returns>
	Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
		//如果顶点索引小于0我们就从边界顶点索引中获取顶点信息，这样就能正确计算边界法线
		Vector3 pointA = (indexA < 0)?borderVertices[-indexA-1] : vertices [indexA];
		Vector3 pointB = (indexB < 0)?borderVertices[-indexB-1] : vertices [indexB];
		Vector3 pointC = (indexC < 0)?borderVertices[-indexC-1] : vertices [indexC];
		//叉乘向量归一化得到法线
		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross (sideAB, sideAC).normalized;
	}
	
	public void BakeNormals() {//计算法线
		bakedNormals = CalculateNormals ();
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
		mesh.normals = bakedNormals;//便于光照效果呈现
		return mesh;
	}
}