using UnityEngine;

namespace Afterlife.Devlog.DamageMarks
{
    // 표면 자국 어댑터. 타깃의 체력·파괴·씬을 직접 참조하지 않는다.
    public sealed class ShowcaseDamageMarks
    {
        private readonly DamageMarkHistory _history=new DamageMarkHistory();
        private readonly Vector4[] _points=new Vector4[DamageMarkHistory.Capacity];
        private readonly Vector4[] _normals=new Vector4[DamageMarkHistory.Capacity];
        public void Add(Transform frame,Vector3 worldPoint,Vector3 worldNormal)
        {
            Vector3 p=frame.InverseTransformPoint(worldPoint);
            Vector3 n=frame.localToWorldMatrix.transpose.MultiplyVector(worldNormal).normalized;
            _history.Add(p.x,p.y,p.z,n.x,n.y,n.z);
            for (int i=0;i<_history.Count;i++)
            {
                var mark=_history.Marks[i];
                _points[i]=new Vector4(mark.X,mark.Y,mark.Z,mark.Strength);
                _normals[i]=new Vector4(mark.Nx,mark.Ny,mark.Nz,0);
            }
        }
        public void Write(MaterialPropertyBlock block,Transform frame,bool enabled,bool wood)
        {
            block.SetInt("_DamageMarkCountV1",enabled ? _history.Count : 0);
            block.SetFloat("_DamageWood",wood ? 1 : 0);
            block.SetVectorArray("_DamageMarksV1",_points);
            block.SetVectorArray("_DamageNormalsV1",_normals);
            block.SetMatrix("_DamageWorldToLocal",frame.worldToLocalMatrix);
        }
        public void Clear()
        {
            _history.Clear(); System.Array.Clear(_points,0,_points.Length); System.Array.Clear(_normals,0,_normals.Length);
        }
    }
}
