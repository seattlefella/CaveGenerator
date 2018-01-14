using UnityEngine;

namespace Assets.Scripts
{
    // TODO: Convert this class to a simple create grid in the editor
    // TODO: This should also become an Editor Script and not appear in game play
    public class GizmoPlay : MonoBehaviour
    {

        public Texture MyTexture ;

        [SerializeField]
        public int _totalColumns = 25;
        [SerializeField]
        public int _totalRows = 10;

        public int TotalColumns
        {
            get { return _totalColumns; }
            set { _totalColumns = value; }
        }

        public int TotalRows
        {
            get { return _totalRows; }
            set { _totalRows = value; }
        }

        public const float GridSize = 1.00f;
        private readonly Color _normalColor = Color.grey;
        private readonly Color _selectedColor = Color.yellow;

        private void OnDrawGizmos()
        {
            //Color oldColor = Gizmos.color;
            //Matrix4x4 oldMatrix = Gizmos.matrix;
            //Gizmos.matrix = transform.localToWorldMatrix;

            //Gizmos.color = _normalColor;
            //GridGizmo(_totalColumns, _totalRows);
            //GridFrameGizmo(_totalColumns, _totalRows);

            //Gizmos.color = oldColor;
            //Gizmos.matrix = oldMatrix;
        }

        private void OnDrawGizmosSelected()
        {
            //Color oldColor = Gizmos.color;
            //Gizmos.color = _selectedColor;
            //GridFrameGizmo(_totalColumns, _totalRows);
            //Gizmos.color = oldColor;
        }


        private void GridFrameGizmo(int cols, int rows)
        {
            Gizmos.DrawLine(new Vector3(0, 0, 0), new Vector3(0, rows * GridSize, 0));
            Gizmos.DrawLine(new Vector3(0, 0, 0), new Vector3(cols * GridSize, 0, 0));
            Gizmos.DrawLine(new Vector3(cols * GridSize, 0, 0), new Vector3(cols * GridSize, rows * GridSize, 0));
            Gizmos.DrawLine(new Vector3(0, rows * GridSize, 0), new Vector3(cols * GridSize, rows * GridSize, 0));
        }
        private void GridGizmo(int cols, int rows)
        {
            for (int i = 1; i < cols; i++)
            {
                Gizmos.DrawLine(new Vector3(i * GridSize, 0, 0), new Vector3(i * GridSize, rows * GridSize, 0));
            }
            for (int j = 1; j < rows; j++)
            {
                Gizmos.DrawLine(new Vector3(0, j * GridSize, 0), new Vector3(cols * GridSize, j * GridSize, 0));
            }
        }

    }
}
