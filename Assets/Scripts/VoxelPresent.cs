using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxDecoder;

public class VoxelPresent : MonoBehaviour
{
    public VoxelCube cubePrefab;
    public string voxelPath;
    private VoxReader voxReader = new VoxReader();
    private VoxFile voxFile;
    // Start is called before the first frame update
    void Start()
    {
        Vector3 root = transform.position;

        var absolutePath = Application.dataPath + "/" + voxelPath;
        if (System.IO.File.Exists(absolutePath))
        {
            voxFile = voxReader.ParseFile(absolutePath);
        }
        foreach (var xyzi in voxFile.xyziChunks[0].voxels)
        {
            var cube = Instantiate(cubePrefab, root + new Vector3(xyzi.X, xyzi.Z, xyzi.Y), Quaternion.identity, transform);
            VoxColor voxColor = voxFile.rgbaChunk.colors[xyzi.I];
            Color color = new Color(voxColor.R / 255.0f, voxColor.G / 255.0f, voxColor.B / 255.0f);
            cube.color = color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
