using UnityEngine;
using UnityEditor;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Neatberry
{
	public class CreateSurfaceChain : ScriptableWizard
	{
		[Header("General")]
		public string optionalName;
		public Material surfaceMaterial;
		public bool staticSlices = false;

		[Header("Geometry")]
		public float totalWidth = 100.0f;
		public float totalLength = 100.0f;
		public float geometryResolution = 10.0f;

		[Header("Heightmap")]
		public int textureSize = 2048;
		public RenderTextureFormat textureFormat = RenderTextureFormat.R8;
		public int texelsPerWorldUnit = 16;

		[MenuItem("GameObject/Create Other/Surface Chain...")]
		static void CreateWizard()
		{
			ScriptableWizard.DisplayWizard("Create Surface Chain", typeof(CreateSurfaceChain));
		}

		Mesh CreateBaseMesh(string name, int xVerticies, int zVerticies, float width, float length)
		{
			string planeAssetName = name + xVerticies + "x" + zVerticies + "W" + width + "L" + length + ".asset";
			
			var mesh = new Mesh();
			mesh.name = name;

			int xPoints = xVerticies + 1;
			int yPoints = zVerticies + 1;
			int numTriangles = xVerticies * zVerticies * 6;
			int numVertices = xPoints * yPoints;

			Vector3[] vertices = new Vector3[numVertices];
			Vector2[] uvs = new Vector2[numVertices];
			Vector4[] sliceUv = new Vector4[numVertices];
			int[] triangles = new int[numTriangles];

			int index = 0;
			float uvFactorX = 1.0f / xVerticies;
			float uvFactorY = 1.0f / zVerticies;
			float scaleX = width / xVerticies;
			float scaleY = length / zVerticies;
			for (float y = 0.0f; y < yPoints; y++)
			{
				for (float x = 0.0f; x < xPoints; x++)
				{
					vertices[index] = new Vector3(x * scaleX, 0.0f, y * scaleY);
					uvs[index] = new Vector2(x * uvFactorX, y * uvFactorY);

					index++;
				}
			}

			index = 0;
			for (int y = 0; y < zVerticies; y++)
			{
				for (int x = 0; x < xVerticies; x++)
				{
					triangles[index] = (y * xPoints) + x;
					triangles[index + 1] = ((y + 1) * xPoints) + x;
					triangles[index + 2] = (y * xPoints) + x + 1;

					triangles[index + 3] = ((y + 1) * xPoints) + x;
					triangles[index + 4] = ((y + 1) * xPoints) + x + 1;
					triangles[index + 5] = (y * xPoints) + x + 1;
					index += 6;
				}
			}

			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.uv2 = uvs;
			mesh.uv3 = uvs;
			mesh.triangles = triangles;
			mesh.RecalculateNormals();
			mesh.RecalculateBounds();

			AssetDatabase.CreateAsset(mesh, "Assets/" + planeAssetName);
			AssetDatabase.SaveAssets();

			return mesh;
		}

		void OnWizardCreate()
		{
			// pre-compute some data
			int xTextures = Mathf.CeilToInt(totalWidth * texelsPerWorldUnit / textureSize);
			int zTextures = Mathf.CeilToInt(totalLength * texelsPerWorldUnit / textureSize);

			float segmentWidth = totalWidth / xTextures;
			float segmentLength = totalLength / zTextures;
			int xVerticiesPerSegment = Math.Max(2, Mathf.CeilToInt(segmentWidth / geometryResolution));
			int zVerticiesPerSegment = Math.Max(2, Mathf.CeilToInt(segmentLength / geometryResolution));

			// root object
			GameObject surfaceChain = new GameObject();
			surfaceChain.transform.position = Vector3.zero;

			string surfaceName = optionalName;
			if (string.IsNullOrEmpty(surfaceName))
				surfaceName = "Surface Chain";
			surfaceName += " (" + xTextures + " x " + zTextures + " )";
			surfaceChain.name = surfaceName;

			// prepare mesh
			Mesh mesh = CreateBaseMesh("Plane", xVerticiesPerSegment, zVerticiesPerSegment, segmentWidth, segmentLength);

			// make grid
			var grid = new DisplaceableSurface[xTextures, zTextures];
			for (int z = 0; z < zTextures; ++z)
			{
				for (int x = 0; x < xTextures; ++x)
				{
					GameObject slice = new GameObject();
					slice.name = "Slice (" + x.ToString() + ", " + z.ToString() + ")";
					slice.isStatic = staticSlices;

					// add mesh and renderer
					var meshFilter = slice.AddComponent<MeshFilter>();
					meshFilter.sharedMesh = mesh;

					var renderer = slice.AddComponent<MeshRenderer>();
					renderer.material = surfaceMaterial;

					// surface
					var script = slice.AddComponent<DisplaceableSurface>();
					script.textureFormat = textureFormat;
					script.maximumTextureSize = textureSize;

					// collider
					var collider = slice.AddComponent<MeshCollider>();

					// save item and push to the parent
					grid[x, z] = script;

					slice.transform.parent = surfaceChain.transform;
					slice.transform.position = Vector3.zero;
					slice.transform.localPosition = new Vector3(x * mesh.bounds.size.x, 0, z * mesh.bounds.size.z);
				}
			}

			// link items on the grid
			for (int z = 0; z < zTextures - 1; ++z)
			{
				for (int x = 1; x < xTextures; ++x)
				{
					var current = grid[x, z];
					var left = grid[x - 1, z];
					var top = grid[x, z + 1];

					current.leftNeighbour = left;
					current.topNeighbour = top;
				}
			}

			// manager
			var manager = surfaceChain.AddComponent<SurfaceChainManager>();

			// done
			Selection.activeObject = surfaceChain;
		}
	}
}