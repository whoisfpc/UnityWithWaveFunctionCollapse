using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxDecoder;

public class VoxelProvider : MonoBehaviour
{

    public string voxelPath;
    private VoxReader voxReader = new VoxReader();
    private VoxFile voxFile;
    public int stride = 1;
    private int idx = 0;
    VoxelProcedural.Offset[] offsets;
    VoxelProcedural voxelProcedural;
    // Start is called before the first frame update
    void Start()
    {
        voxelProcedural =  GetComponent<VoxelProcedural>();
        var root = transform.position;
        var absolutePath = Application.dataPath + "/" + voxelPath;
        if (System.IO.File.Exists(absolutePath))
        {
            voxFile = voxReader.ParseFile(absolutePath);
        }
        var size = voxFile.sizeChunks[0];
        voxelProcedural.bounds = new Bounds(root, new Vector3(size.sizeX, size.sizeZ, size.sizeY));

        Vector3[] colors = new Vector3[voxFile.rgbaChunk.colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            VoxColor voxColor = voxFile.rgbaChunk.colors[i];
            colors[i] = new Vector3(voxColor.R / 255.0f, voxColor.G / 255.0f, voxColor.B / 255.0f);
        }
        voxelProcedural.CreateColorBuffer(colors.Length);
        voxelProcedural.SetColors(colors);
        
        var instanceCount = voxFile.xyziChunks[0].voxels.Length;
        offsets = new VoxelProcedural.Offset[instanceCount];


        voxelProcedural.CreateOffsetBuffer(offsets.Length);
        voxelProcedural.SetOffsets(offsets, 0);
    }

    // Update is called once per frame
    void Update()
    {
        var voxels = voxFile.xyziChunks[0].voxels;
        for (int i = 0; i < stride && idx < offsets.Length; i++, idx++)
        {
            var xyzi = voxels[idx];
            offsets[idx] =  new VoxelProcedural.Offset()
            {
                position = transform.position + new Vector3(xyzi.X, xyzi.Z, xyzi.Y),
                colorIdx = xyzi.I,
            };           
        }
        voxelProcedural.SetOffsets(offsets, idx);
    }
}
