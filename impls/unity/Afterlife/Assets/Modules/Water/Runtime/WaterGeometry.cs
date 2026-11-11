using System;
using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Modules.Water
{
    public sealed class WaterMeshData
    {
        public List<Vector3> Vertices,Normals;public List<Vector2> Uv;public List<Vector4> Shores;public List<int> Indices;
    }
    public static class WaterGeometry
    {
        public static WaterMeshData Calculate(int minX,int minZ,int maxX,int maxZ,int radius,Func<int,int,bool> water,float height,float bottom)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
            var shores=new List<Vector4>();var indices=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Vector4 shore)
            {
                int first=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                uv.Add(new Vector2(0,0));uv.Add(new Vector2(0,1));uv.Add(new Vector2(1,0));uv.Add(new Vector2(1,1));
                for(int i=0;i<4;i++){normals.Add(normal);shores.Add(shore);}
                indices.Add(first);indices.Add(first+1);indices.Add(first+2);indices.Add(first+2);indices.Add(first+1);indices.Add(first+3);
            }
            bool Land(int x,int z)=>Math.Abs(x)<=radius && Math.Abs(z)<=radius && !water(x,z);
            for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++)
            {
                if(!water(x,z))continue;
                var a=new Vector3(x-.5f,height,z-.5f);var b=new Vector3(x-.5f,height,z+.5f);
                var c=new Vector3(x+.5f,height,z-.5f);var d=new Vector3(x+.5f,height,z+.5f);
                Quad(a,b,c,d,Vector3.up,new Vector4(Land(x-1,z)?1:0,Land(x+1,z)?1:0,Land(x,z-1)?1:0,Land(x,z+1)?1:0));
                // 내부 물 타일 경계에는 면을 만들지 않는다.
                void Wall(Vector3 left,Vector3 right,Vector3 normal)
                { Quad(left,right,new Vector3(left.x,bottom,left.z),new Vector3(right.x,bottom,right.z),normal,Vector4.zero); }
                if(x==-radius)Wall(b,a,Vector3.left);
                if(x==radius)Wall(c,d,Vector3.right);
                if(z==-radius)Wall(a,c,Vector3.back);
                if(z==radius)Wall(d,b,Vector3.forward);
            }
            return new WaterMeshData { Vertices=vertices,Normals=normals,Uv=uv,Shores=shores,Indices=indices };
        }
    }
}
