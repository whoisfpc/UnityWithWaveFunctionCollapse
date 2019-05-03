using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using VoxDecoder;

public class VoxelProcedural : MonoBehaviour
{

    struct Point
    {
        public Vector3 vertex;
        public Vector3 normal;
    }

    public struct Offset
    {
        public Vector3 position;
        public uint colorIdx;
    }

    Material material;
    int instanceCount;
    Vector3[] vertices;
    int[] triangles;
    GraphicsBuffer indexBuffer;
    int indexCount;
    ComputeBuffer pointBuffer;
    ComputeBuffer offsetBuffer;
    ComputeBuffer colorBuffer;
    public MeshFilter cubeMeshFilter;
    public Bounds bounds;

    void Awake()
    {
        Mesh mesh = cubeMeshFilter.sharedMesh;
        vertices = mesh.vertices;
        triangles = mesh.triangles;
        var normals = mesh.normals;

        var vertexCount = vertices.Length;
        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, triangles.Length, Marshal.SizeOf(typeof(int)));
        indexBuffer.SetData(triangles);
        indexCount = indexBuffer.count;

        Point[] points = new Point[vertexCount];
        for(int i = 0; i < points.Length; i++)
        {
            points[i] = new Point()
            {
                vertex = vertices[i],
                normal = normals[i]
            };
        }
        pointBuffer = new ComputeBuffer(points.Length, Marshal.SizeOf(typeof(Point)), ComputeBufferType.Default);
        pointBuffer.SetData(points);
        material = new Material(Shader.Find("MyProcedural"));
        material.SetBuffer("points", pointBuffer);
        bounds = new Bounds();
    }

    public void CreateColorBuffer(int count)
    {
        colorBuffer = new ComputeBuffer(count, Marshal.SizeOf(typeof(Vector3)));
    }

    public void CreateOffsetBuffer(int count)
    {
        offsetBuffer = new ComputeBuffer(count,  Marshal.SizeOf(typeof(Offset)));
    }

    public void SetColors(Vector3[] colors)
    {
        colorBuffer.SetData(colors);
        material.SetBuffer("colors", colorBuffer);
    }

    public void SetOffsets(Offset[] offsets, int count)
    {
        offsetBuffer.SetData(offsets);
        material.SetBuffer("offsets", offsetBuffer);
        instanceCount = count;
    }

    // Update is called once per frame
    void Update()
    {
        Graphics.DrawProcedural(
            material,
            bounds,
            MeshTopology.Triangles,
            indexBuffer,
            indexCount,
            instanceCount,
            null,
            null
        );
    }

    void OnDestroy()
    {
        indexBuffer.Release();
        pointBuffer.Release();
        offsetBuffer.Release();
        colorBuffer.Release();
    }
}
