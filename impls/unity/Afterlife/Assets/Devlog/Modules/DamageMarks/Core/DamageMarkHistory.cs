using System;

namespace Afterlife.Devlog.DamageMarks
{
    public sealed class DamageMarkHistory
    {
        public struct Mark { public float X,Y,Z,Nx,Ny,Nz,Strength; }
        public const int Capacity=8;
        public readonly Mark[] Marks=new Mark[Capacity];
        public int Count { get; private set; }
        private int _replace;
        public void Add(float x,float y,float z,float nx,float ny,float nz)
        {
            int slot=-1;
            float best=.13f*.13f;
            for (int i=0;i<Count;i++)
            {
                var m=Marks[i];
                float dx=m.X-x,dy=m.Y-y,dz=m.Z-z, distance=dx*dx+dy*dy+dz*dz;
                if (distance<best && m.Nx*nx+m.Ny*ny+m.Nz*nz>.7f) { best=distance; slot=i; }
            }
            if (slot<0)
            {
                slot=Count<Capacity ? Count++ : _replace++%Capacity;
                Marks[slot]=new Mark { X=x,Y=y,Z=z,Nx=nx,Ny=ny,Nz=nz };
            }
            var mark=Marks[slot]; mark.Strength=Math.Min(1,mark.Strength+.34f); Marks[slot]=mark;
        }
        public void Clear() { Array.Clear(Marks,0,Capacity); Count=0; _replace=0; }
    }
}
