using UnityEngine;

namespace DisasterReady.Utility
{
    /// <summary>
    /// Small library of hand-rolled low-poly meshes (cones, gable roofs) used to keep
    /// the demo's visual language "cute stylized indie" instead of raw primitive cubes.
    /// Used only at scene-build time by the editor tooling.
    /// </summary>
    public static class ProceduralMeshFactory
    {
        public static Mesh CreateCone(float radius, float height, int segments = 8)
        {
            var mesh = new Mesh { name = "ProcCone" };
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            var normals = new System.Collections.Generic.List<Vector3>();

            Vector3 apex = new Vector3(0, height, 0);
            int apexIndex = 0;
            verts.Add(apex);
            normals.Add(Vector3.up);

            int baseStart = verts.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 p = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                verts.Add(p);
                normals.Add(p.normalized);
            }

            for (int i = 0; i < segments; i++)
            {
                int a = baseStart + i;
                int b = baseStart + (i + 1) % segments;
                tris.Add(apexIndex);
                tris.Add(b);
                tris.Add(a);
            }

            // Bottom cap
            int centerBottom = verts.Count;
            verts.Add(Vector3.zero);
            normals.Add(Vector3.down);
            for (int i = 0; i < segments; i++)
            {
                int a = baseStart + i;
                int b = baseStart + (i + 1) % segments;
                tris.Add(centerBottom);
                tris.Add(a);
                tris.Add(b);
            }

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Simple gable (tent) roof: a triangular-prism shape.</summary>
        public static Mesh CreateGableRoof(float width, float depth, float height)
        {
            var mesh = new Mesh { name = "ProcGableRoof" };
            float hw = width / 2f;
            float hd = depth / 2f;

            Vector3 v0 = new Vector3(-hw, 0, -hd);
            Vector3 v1 = new Vector3(hw, 0, -hd);
            Vector3 v2 = new Vector3(hw, 0, hd);
            Vector3 v3 = new Vector3(-hw, 0, hd);
            Vector3 ridgeA = new Vector3(0, height, -hd);
            Vector3 ridgeB = new Vector3(0, height, hd);

            var verts = new System.Collections.Generic.List<Vector3>
            {
                v0, v1, ridgeA, // front slope tri part
                v1, v2, ridgeB, ridgeA, // right slope quad
                v2, v3, ridgeB,
                v3, v0, ridgeA, ridgeB
            };

            // Build explicit faces for reliability instead of reusing indices above.
            verts.Clear();
            var tris = new System.Collections.Generic.List<int>();

            void AddTri(Vector3 a, Vector3 b, Vector3 c)
            {
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
            }

            void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int i = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 3);
            }

            // Front & back gable triangles
            AddTri(v0, v1, ridgeA);
            AddTri(v3, ridgeB, v2);
            // Two roof slopes
            AddQuad(v0, ridgeA, ridgeB, v3);
            AddQuad(v1, v2, ridgeB, ridgeA);
            // NOTE: an "underside" face (AddQuad(v0, v3, v2, v1) at local y=0)
            // used to be added here so the roof didn't look hollow from below.
            // It doesn't - this roof is placed at localPosition (0, height, 0)
            // on top of the building body cube, whose top face sits at that
            // exact same world Y. The underside face and the body's top face
            // were therefore perfectly coplanar, which is a textbook z-fighting
            // setup: two opaque triangles occupying the same depth-buffer plane
            // flicker between which one wins as the camera moves, which is
            // exactly the roof flicker/vanish/reappear bug. The body cube's own
            // opaque top face already fully occludes the view up into the roof
            // from below/inside, so the underside face was purely redundant
            // geometry - removing it fixes the flicker with no visual loss.

            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
