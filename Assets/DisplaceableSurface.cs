using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Neatberry
{
	/// <summary>
	/// Surface
	/// </summary>
	public class DisplaceableSurface : MonoBehaviour
	{
		public Bounds bounds { get; private set; }

		// render texture
		[HideInInspector] public RenderTextureFormat textureFormat = RenderTextureFormat.R8;
		[HideInInspector] public int maximumTextureSize = 4096;

		// neighbors
		public DisplaceableSurface leftNeighbour;
		public DisplaceableSurface topNeighbour;

		private RenderTexture splatmap;
		private Material flexMaterial;

		private void Awake()
		{
			var root = GetComponentInParent<SurfaceChainManager>();
			var renderer = GetComponent<MeshRenderer>();
			bounds = renderer.bounds;

			// main render texture
			flexMaterial = renderer.material;
			splatmap = new RenderTexture(maximumTextureSize, maximumTextureSize, 0, textureFormat)
			{
				dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
				anisoLevel = 8
			};
			flexMaterial.SetTexture("_Splatmap", splatmap);

			if (root != null)
			{
				flexMaterial.SetFloat("_Displacement", root.displacement);
				flexMaterial.SetFloat("_Tess", root.tesselation);
			}
		}

		/// <summary>
		/// Applies prepared brush onto the heightmap texture
		/// </summary>
		/// <param name="coordinates">UV coordinates to apply texture at</param>
		/// <param name="brush">Brush material</param>
		/// <param name="brushSize">Brush size in world units</param>
		internal void ApplyBrush(Vector4 coordinates, Material brush, float brushSize)
		{
			// adjust brush size
			Vector2 worldToPixel = new Vector2(
				1.0f / bounds.size.x,
				1.0f / bounds.size.z
			);
			brushSize *= Math.Max(worldToPixel.x, worldToPixel.y);

			// setup brush
			brush.SetFloat("_Size", brushSize);
			brush.SetVector("_Coordinates", coordinates);

			// render
			RenderTexture tmp = RenderTexture.GetTemporary(splatmap.width, splatmap.height, 0, splatmap.format);
			Graphics.CopyTexture(splatmap, tmp);
			Graphics.Blit(tmp, splatmap, brush);
			RenderTexture.ReleaseTemporary(tmp);
		}

		/// <summary>
		/// Stitches it's left side with neighbor's texture
		/// </summary>
		internal void FetchLeftEdge()
		{
			if (null == leftNeighbour)
				return;

			var sourceTexture = leftNeighbour.splatmap;
			var targetTexture = this.splatmap;

			Graphics.CopyTexture(
				sourceTexture, 0, 0, sourceTexture.width - 1, 0, 1, sourceTexture.height,
				targetTexture, 0, 0, 0, 0
			);
		}

		/// <summary>
		/// Stitches it's top side with neighbor's texture
		/// </summary>
		internal void FetchTopEdge()
		{
			if (null == topNeighbour)
				return;

			var sourceTexture = topNeighbour.splatmap;
			var targetTexture = this.splatmap;

			Graphics.CopyTexture(
				sourceTexture, 0, 0, 0, 0, sourceTexture.width, 1,
				targetTexture, 0, 0, 0, targetTexture.height - 1
			);
		}
	}
}