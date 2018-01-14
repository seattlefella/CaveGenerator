using System.Collections.Generic;
using UnityEngine;

namespace Assets.Editor.Handle
{
	public class Bezier : MonoBehaviour
	{

		public float T;
		public float TMax = 128;
		public int PointsInCurve = 128;
		public Vector3[] Pts = new Vector3[4];
		public List<Vector3> Curve = new List<Vector3>();
		public List<Vector3> Tangent = new List<Vector3>();
		public List<Vector3> Normal = new List<Vector3>();
		public List<Quaternion> Orientation = new List<Quaternion>();
		public GameObject Pt0, Pt1, Pt2, Pt3;




		void OnDrawGizmos()

		{

			Pts[0] = new Vector3(Pt0.transform.localPosition.x, Pt0.transform.localPosition.y, Pt0.transform.localPosition.z);
			Pts[1] = new Vector3(Pt1.transform.localPosition.x, Pt1.transform.localPosition.y, Pt1.transform.localPosition.z);
			Pts[2] = new Vector3(Pt2.transform.localPosition.x, Pt2.transform.localPosition.y, Pt2.transform.localPosition.z);
			Pts[3] = new Vector3(Pt3.transform.localPosition.x, Pt3.transform.localPosition.y, Pt3.transform.localPosition.z);

			Curve.Clear();
			Tangent.Clear();
			Normal.Clear();
			Orientation.Clear();
			var step = 0f;

			for (var i = 0; i < PointsInCurve; i++)
			{
				step = i/(float)PointsInCurve; 
				Curve.Add(GetPoint(Pts, step));
				Tangent.Add(GetTangent(Pts, step));
				Normal.Add(GetNormal3D(Pts, step, Vector3.up));
				Orientation.Add(GetOrientation3D(Pts, step, Vector3.up));
			}


			for (var i = 0; i < PointsInCurve - 1; i++)

			{
				Gizmos.DrawLine(Curve[i], Curve[i + 1]);

			}

		}


	/* Code snippets from Unite 2015 - A coder's guide to spline-based procedural geometry */
	/* https://www.youtube.com/watch?v=o9RK6O2kOKo */

	// Optimized GetPoint
	public static Vector3 GetPoint(Vector3[] pts, float t)
	{
		float omt = 1f - t;
		float omt2 = omt * omt;
		float t2 = t * t;
		return pts[0] * (omt2 * omt) +
				pts[1] * (3f * omt2 * t) +
				pts[2] * (3f * omt * t2) +
				pts[3] * (t2 * t);
	}

        // Get Tangent
    public static Vector3 GetTangent(Vector3[] pts, float t)
    {
	    float omt = 1f - t;
	    float omt2 = omt * omt;
	    float t2 = t * t;
	    Vector3 tangent =
		    pts[0] * (-omt2) +
		    pts[1] * (3 * omt2 - 2 * omt) +
		    pts[2] * (-3 * t2 + 2 * t) +
		    pts[3] * (t2);
	    return tangent.normalized;
    }

        // Get Normal
    public static Vector3 GetNormal2D(Vector3[] pts, float t)
    {
	    Vector3 tng = GetTangent(pts, t);
	    return new Vector3(-tng.y, tng.x, 0f);
    }

    public static Vector3 GetNormal3D(Vector3[] pts, float t, Vector3 up)
    {
	    Vector3 tng = GetTangent(pts, t);
	    Vector3 binormal = Vector3.Cross(up, tng).normalized;
	    return Vector3.Cross(tng, binormal);
    }

        // Get Orientation
    public static Quaternion GetOrientation2D(Vector3[] pts, float t)
    {
	    Vector3 tng = GetTangent(pts, t);
	    Vector3 nrm = GetNormal2D(pts, t);
	    return Quaternion.LookRotation(tng, nrm);
    }

    public static Quaternion GetOrientation3D(Vector3[] pts, float t, Vector3 up)
    {
	    Vector3 tng = GetTangent(pts, t);
	    Vector3 nrm = GetNormal3D(pts, t, up);
	    return Quaternion.LookRotation(tng, nrm);
    }

	//

	public struct OrientedPoint
	{

		public Vector3 Position;
		public Quaternion Rotation;

		public OrientedPoint(Vector3 position, Quaternion rotation)
		{
			this.Position = position;
			this.Rotation = rotation;
		}

		public Vector3 LocalToWorld(Vector3 point)
		{
			return Position + Rotation * point;
		}

		public Vector3 WorldToLocal(Vector3 point)
		{
			return Quaternion.Inverse(Rotation) * (point - Position);
		}

		public Vector3 LocalToWorldDirection(Vector3 dir)
		{
			return Rotation * dir;
		}
	}

	}









}
