using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Neatberry
{
	public class SurfaceChainManager : MonoBehaviour
	{
		DisplaceableSurface[] surfaces;

		[Header("Brush")]
		public Shader brushShader;
		public float brushSize = 15.0f;
		public float brushSmooth = 2.0f;
		public float brushStrength = 1.0f;

		[Header("Displacement")]
		public float tesselation = 4;
		public float displacement = 1.0f;
		
		private Vector2 sliceSize = Vector2.zero;   // single plane mesh .xz dimensions in world space
		private Material brushMaterial;

		// Start is called before the first frame update
		void Start()
		{
			surfaces = GetComponentsInChildren<DisplaceableSurface>();

			// read base surface params
			if (surfaces.Length > 0)
			{
				Bounds bbox = surfaces[0].bounds;
				sliceSize = new Vector2(bbox.size.x, bbox.size.z);
			}

			// brush material
			brushMaterial = new Material(brushShader);
			brushMaterial.SetFloat("_Strength", brushStrength);
			brushMaterial.SetFloat("_Smooth", brushSmooth);
		}

		// Update is called once per frame
		void Update()
		{
			// listen for mouse press
			if (!Input.GetKey(KeyCode.Mouse0))
				return;

			// ray casting towards mesh
			RaycastHit rayHit;
			if (!Physics.Raycast(Camera.allCameras[0].ScreenPointToRay(Input.mousePosition), out rayHit))
				return;

			// origin
			DisplaceableSurface origin = rayHit.transform.gameObject.GetComponent<DisplaceableSurface>();
			if (null == origin)
				return;

			// distance check to add neighboring surfaces
			float checkDistance = brushSize * brushSize;
			List<DisplaceableSurface> toDraw = new List<DisplaceableSurface>();
			toDraw.Add(origin);

			foreach (var surface in surfaces)
			{
				if (surface == origin)
					continue;

				var distance = surface.bounds.SqrDistance(rayHit.point);
				if (distance <= checkDistance)
					toDraw.Add(surface);
			}

			// draw
			ApplyBrush(origin, rayHit, toDraw);
		}

		void ApplyBrush(DisplaceableSurface origin, RaycastHit rayHit, List<DisplaceableSurface> canvases)
		{
			// draw strokes
			foreach (var canvas in canvases)
			{
				Vector2 uv = rayHit.textureCoord;

				// adjust texture coordinates
				if (origin != canvas)
				{
					Vector3 dv = canvas.bounds.min - origin.bounds.min;
					uv.x -= dv.x / sliceSize.x;
					uv.y -= dv.z / sliceSize.y;
				}

				// actual brush drawing
				canvas.ApplyBrush(uv, brushMaterial, brushSize);
			}

			// DEBUG:
			// Leave this instead of foreach to test edges stitching
			//origin.ApplyBrush(rayHit.textureCoord, brushMaterial, brushSize);

			// stitch the edges
			StitchChain(canvases);
		}

		void StitchChain(List<DisplaceableSurface> items)
		{
			var localCache = new HashSet<DisplaceableSurface>(items);

			foreach (var surface in items)
				if (localCache.Contains(surface.topNeighbour))
					surface.FetchTopEdge();

			foreach (var surface in items)
				if (localCache.Contains(surface.leftNeighbour))
					surface.FetchLeftEdge();
		}
	}
}