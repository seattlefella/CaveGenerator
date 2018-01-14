using Assets.Scripts;
using UnityEditor;
using UnityEngine;

namespace Assets.Editor.MeshTools.Scripts
{
    public class DrawTargetGizmo : MonoBehaviour {

        public Texture MyTexture;

        // This emulates OnDrawGizmos
        [DrawGizmo(GizmoType.NotInSelectionHierarchy |
                   GizmoType.InSelectionHierarchy |
                   GizmoType.Selected |
                   GizmoType.Active |
                   GizmoType.Pickable)]

        private static void MyCustomOnDrawGizmos(GizmoTarget gizmoTarget, GizmoType gizmoType)
        {


            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(gizmoTarget.transform.position, Vector3.one);
 
        }
    }
}
