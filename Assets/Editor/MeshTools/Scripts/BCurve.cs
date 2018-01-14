using UnityEngine;

namespace Assets.Editor.MeshTools.Scripts
{
    public class MeshSplinesAndExtrusion
    {


/* Code snippets from Unite 2015 - A coder's guide to spline-based procedural geometry */
/* https://www.youtube.com/watch?v=o9RK6O2kOKo */

// Optimized GetPoint
        Vector3 GetPoint(Vector3[] pts, float t)
        {
            float omt = 1f - t;
            float omt2 = omt*omt;
            float t2 = t*t;
            return pts[0]*(omt2*omt) +
                   pts[1]*(3f*omt2*t) +
                   pts[2]*(3f*omt*t2) +
                   pts[3]*(t2*t);
        }

// Get Tangent
        Vector3 GetTangent(Vector3[] pts, float t)
        {
            float omt = 1f - t;
            float omt2 = omt*omt;
            float t2 = t*t;
            Vector3 tangent =
                pts[0]*(-omt2) +
                pts[1]*(3*omt2 - 2*omt) +
                pts[2]*(-3*t2 + 2*t) +
                pts[3]*(t2);
            return tangent.normalized;
        }

// Get Normal
        Vector3 GetNormal2D(Vector3[] pts, float t)
        {
            Vector3 tng = GetTangent(pts, t);
            return new Vector3(-tng.y, tng.x, 0f);
        }

        Vector3 GetNormal3D(Vector3[] pts, float t, Vector3 up)
        {
            Vector3 tng = GetTangent(pts, t);
            Vector3 binormal = Vector3.Cross(up, tng).normalized;
            return Vector3.Cross(tng, binormal);
        }

// Get Orientation
        Quaternion GetOrientation2D(Vector3[] pts, float t)
        {
            Vector3 tng = GetTangent(pts, t);
            Vector3 nrm = GetNormal2D(pts, t);
            return Quaternion.LookRotation(tng, nrm);
        }

        Quaternion GetOrientation3D(Vector3[] pts, float t, Vector3 up)
        {
            Vector3 tng = GetTangent(pts, t);
            Vector3 nrm = GetNormal3D(pts, t, up);
            return Quaternion.LookRotation(tng, nrm);
        }

//

        public struct OrientedPoint
        {

            public Vector3 position;
            public Quaternion rotation;

            public OrientedPoint(Vector3 position, Quaternion rotation)
            {
                this.position = position;
                this.rotation = rotation;
            }

            public Vector3 LocalToWorld(Vector3 point)
            {
                return position + rotation*point;
            }

            public Vector3 WorldToLocal(Vector3 point)
            {
                return Quaternion.Inverse(rotation)*(point - position);
            }

            public Vector3 LocalToWorldDirection(Vector3 dir)
            {
                return rotation*dir;
            }
        }

//// 
//        public void Extrude(Mesh mesh, ExtrudeShape shape, OrientedPoint[] path)
//        {

//            int vertsInShape = shape.vert2Ds.Length;
//            int segments = path.Length - 1;
//            int edgeLoops = path.Length;
//            int vertCount = vertsInShape * edgeLoops;
//            int triCount = shape.lines.Length * segments;
//            int triIndexCount = triCount * 3;
//            int vertsInShape = shape.vert2Ds.Length;
//            int segments = path.Length - 1;
//            int edgeLoops = path.Length;
//            int vertCount = vertsInShape * edgeLoops;
//            int triCount = shape.lines.Length * segments;
//            int triIndexCount = triCount * 3;

//            int[] triangleIndices = new int[triIndexCount];
//            Vector3[] vertices = new Vector3[vertCount];
//            Vector3[] normals = new Vector3[vertCount];
//            Vector2[] uvs = new Vector2[vertCount];

//            /* Generation code goes here */

//            mesh.Clear();
//            mesh.vertices = vertices;
//            mesh.triangles = triangleIndices;
//            mesh.normals = normals;
//            mesh.uv = uvs;
//            /*
//    foreach oriented point in the path
//        foreach vertex in the 2D shape
//            Add the vertex position, based on the oriented point
//            Add the normal direction, based on the oriented point
//            Add the UV. U is based on the shape, V is based on distance along the path
//        end
//    end

//    foreach segment
//        foreach line in the 2D shape
//            Add two triangles with vertex indices based on the line indices
//        end
//    end*/

//            for (int i = 0; i < path.Length; i++)
//            {
//                int offset = i * vertsInShape;
//                for (int j = 0; j < vertsInShape; j++)
//                {
//                    int id = offset + j;
//                    vertices[id] = path[i].LocalToWorld(shape.vert2Ds[j].point);
//                    normals[id] = path[i].LocalToWorldDirection(shape.vert2Ds[j].normal);
//                    uvs[id] = new Vector2(vert2Ds[j].uCoord, i / ((float)edgeLoops));
//                }
//            }
//            int ti = 0;
//            for (int i = 0; i < segments; i++)
//            {
//                int offset = i * vertsInShape;
//                for (int l = 0; l < lines.Length; l += 2)
//                {
//                    int a = offset + lines[l] + vertsInShape;
//                    int b = offset + lines[l];
//                    int c = offset + lines[l + 1];
//                    int d = offset + lines[l + 1] + vertsInShape;
//                    triangleIndices[ti] = a; ti++;
//                    triangleIndices[ti] = b; ti++;
//                    triangleIndices[ti] = c; ti++;
//                    triangleIndices[ti] = c; ti++;
//                    triangleIndices[ti] = d; ti++;
//                    triangleIndices[ti] = a; ti++;
//                }
//            }


//            // 
//            void CalcLengthTableInto(float[] arr, CubicBezier3D bezier) {
//                arr[0] = 0f;
//                float totalLength = 0f;
//                Vector3 prev = bezier.p0;
//                for (int i = 1; i < arr.Length; i++)
//                {
//                    float t = ((float)i) / (arr.Length - 1);
//                    Vector3 pt = bezier.GetPoint(t);
//                    float diff = (prev - pt).magnitude;
//                    totalLength += diff;
//                    arr[i] = totalLength;
//                    prev = pt;
//                }
//            }

//// 
//        public static class FloatArrayExtensions
//            {
//            public static
//                float Sample 
//                (this
//                float[] fArr,

//                float t)
//                {
//                    int count = fArr.Length;
//                    if (count == 0)
//                    {
//                        Debug.LogError("Unable to sample array - it has no elements");
//                        return 0;
//                    }
//                    if (count == 1)
//                    {
//                        return fArr[0];
//                        float iFloat = t*(count - 1);
//                        int idLower = Mathf.FloorToInt(iFloat);
//                        int idUpper = Mathf.FloorToInt(iFloat + 1);
//                        if (idUpper >= count)
//                            return fArr[count - 1];
//                        if (idLower < 0)
//                            return fArr[0];
//                        return Mathf.Lerp(fArr[idLower], fArr[idUpper], iFloat - idLower);
//                    }
//                }

//            }
    }
}

