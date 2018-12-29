using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.Utilities;

namespace ToolboxScriptDragNDropPlugin
{
	[ExecuteInEditMode]
	public class DemoScript : Script
	{
		public MaterialBase Material;
		private Mesh _mesh;
		private static Random _rng = new Random();

		private void OnEnable()
		{
			// Create dynamic model with a single LOD with one mesh
			var model = Content.CreateVirtualAsset<Model>();
			model.SetupLODs(1);
			_mesh = model.LODs[0].Meshes[0];
			UpdateMesh(_mesh);

			// Create or reuse child model actor
			var childModel = Actor.GetOrAddChild<StaticModel>();
			childModel.Model = model;
			childModel.Entries[0].Material = Material;
			childModel.HideFlags = HideFlags.DontSave;
		}

		private void UpdateMesh(Mesh mesh)
		{
			if (mesh == null) return;
			const int count = 12;
			// 3 points => 1 triangle
			// 2 triangles => 1 quad
			// 4 quads ==> one "line-segment"
			// 3*2*4 = 24
			int len = count * 24;

			Vector3[] vertices = new Vector3[len];
			int[] triangles = new int[len];

			Vector3 lastPoint = Vector3.Zero;
			int counter = 0;
			for (float t = 0; t < len; t += 24)
			{
				Vector3 point = lastPoint + _rng.NextVector3() * 10f;

				Create3DLine(ref counter, ref lastPoint, ref point, vertices, triangles);
				lastPoint = point;
			}

			mesh.UpdateMesh(vertices, triangles);
		}

		private void Create3DLine(ref int counter, ref Vector3 lastPoint, ref Vector3 point, Vector3[] vertices, int[] triangles)
		{
			float radius = 1f;
			Vector3 line = Vector3.Normalize(point - lastPoint);
			// Upwards offset
			Vector3 offset1 = Vector3.Cross(line, Vector3.Up) * radius;
			// Right-offset
			Vector3 offset2 = Vector3.Cross(line, offset1) * radius;

			// The rectangle-points on one end
			Vector3[] startPoints = new Vector3[]
			{
				point + offset1,
				point + offset2,
				point - offset1,
				point - offset2
			};

			// The rectangle-points on the other end
			Vector3[] endPoints = new Vector3[]
			{
				lastPoint + offset1,
				lastPoint + offset2,
				lastPoint - offset1,
				lastPoint - offset2
			};

			// 3 points => 1 triangle
			// 2 triangles => 1 quad
			// 4 quads ==> one "line-segment"
			// 3*2*4 = 24
			for (int i = 0; i < 4; i++)
			{
				// 1st triangle
				vertices[counter] = startPoints[i];
				vertices[counter + 1] = endPoints[(i + 1) % 4];
				vertices[counter + 2] = startPoints[(i + 1) % 4];

				for (int j = 0; j < 3; j++)
				{
					triangles[counter] = counter;
					counter++;
				}

				// 2nd triangle
				vertices[counter] = startPoints[i];
				vertices[counter + 1] = endPoints[i];
				vertices[counter + 2] = endPoints[(i + 1) % 4];
				for (int j = 0; j < 3; j++)
				{
					triangles[counter] = counter;
					counter++;
				}
			}
		}
	}
}