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

    Material material;
    int vertexCount;
    int instanceCount = 1;
    Vector3[] vertices;
    int[] triangles;
    GraphicsBuffer indexBuffer;
    int indexCount;
    ComputeBuffer pointBuffer;
    ComputeBuffer offsetBuffer;
    ComputeBuffer colorBuffer;
    public MeshFilter cubeMeshFilter;

    public string voxelPath;
    private VoxReader voxReader = new VoxReader();
    private VoxFile voxFile;
    private Bounds bounds;

    // Start is called before the first frame update
    void Start()
    {
        Mesh mesh = cubeMeshFilter.sharedMesh;
        vertices = mesh.vertices;
        triangles = mesh.triangles;
        var normals = mesh.normals;

        vertexCount = vertices.Length;
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
        
        var root = transform.position;
        var absolutePath = Application.dataPath + "/" + voxelPath;
        if (System.IO.File.Exists(absolutePath))
        {
            voxFile = voxReader.ParseFile(absolutePath);
        }
        var size = voxFile.sizeChunks[0];
        bounds = new Bounds(root, new Vector3(size.sizeX, size.sizeZ, size.sizeY));
        instanceCount = voxFile.xyziChunks[0].voxels.Length;
        Vector3[] offsets = new Vector3[instanceCount];
        Vector3[] colors = new Vector3[instanceCount];
        int idx = 0;
        foreach (var xyzi in voxFile.xyziChunks[0].voxels)
        {
            offsets[idx] = root + new Vector3(xyzi.X, xyzi.Z, xyzi.Y);
            VoxColor voxColor = voxFile.rgbaChunk.colors[xyzi.I];
            colors[idx] = new Vector3(voxColor.R / 255.0f, voxColor.G / 255.0f, voxColor.B / 255.0f);
            idx++;
        }

        offsetBuffer = new ComputeBuffer(instanceCount,  Marshal.SizeOf(typeof(Vector3)));
        offsetBuffer.SetData(offsets);
        material.SetBuffer("offsets", offsetBuffer);

        colorBuffer = new ComputeBuffer(instanceCount, Marshal.SizeOf(typeof(Vector3)));
        colorBuffer.SetData(colors);
        material.SetBuffer("colors", colorBuffer);
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
