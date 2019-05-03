using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxDecoder;
using WaveFunctionCollapse;

public class WFC3DTest : MonoBehaviour {

    [Header("Random")]
    public int seed;

    public int step = 1;

    public int N = 2;
    public int FMX = 20;
    public int FMY = 20;
    public int FMZ = 20;

    public int limit = 0;

    public string voxelPath;
    private VoxReader voxReader = new VoxReader();
    private VoxFile voxFile;
    private OverlappingModel3D model;
    private bool started = false, pause = true;

    VoxelProcedural voxelProcedural;
    VoxelProcedural.Offset[] offsets;

    private void Start()
    {
        var absolutePath = Application.dataPath + "/" + voxelPath;
        if (System.IO.File.Exists(absolutePath))
        {
            voxFile = voxReader.ParseFile(absolutePath);
        }
        var size = voxFile.sizeChunks[0];
        var sample = new byte[size.sizeX, size.sizeY, size.sizeZ];

        foreach (var xyzi in voxFile.xyziChunks[0].voxels)
        {
            sample[xyzi.X, xyzi.Y, xyzi.Z] = xyzi.I;
        }

        Vector3[] colors = new Vector3[voxFile.rgbaChunk.colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            VoxColor voxColor = voxFile.rgbaChunk.colors[i];
            colors[i] = new Vector3(voxColor.R / 255.0f, voxColor.G / 255.0f, voxColor.B / 255.0f);
        }

        model = new OverlappingModel3D(sample, colors, N, FMX, FMY, FMZ);

        voxelProcedural =  GetComponent<VoxelProcedural>();
        voxelProcedural.bounds = new Bounds(transform.position, new Vector3(FMX, FMZ, FMY)); // swap Y and Z axis
        voxelProcedural.CreateColorBuffer(colors.Length);
        voxelProcedural.SetColors(colors);

        offsets = new VoxelProcedural.Offset[FMX * FMY * FMZ];
        voxelProcedural.CreateOffsetBuffer(offsets.Length);
        voxelProcedural.SetOffsets(offsets, 0);
    }

    private void Update()
    {
        if (started && !pause)
        {
            var result = model.Forward(step);
            switch (result)
            {
                case ObserveResult.Finish:
                {
                    var data = new byte[FMX * FMY * FMZ];
                    model.Capture(data);
                    int tot = 0;
                    var FMO = FMX * FMY;
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != 0)
                        {
                            var x = i % FMO % FMX; 
                            var y = i % FMO / FMX;
                            var z = i / FMO;
                            offsets[tot] = new VoxelProcedural.Offset()
                            {
                                position = new Vector3(x, z, y), // swap Y and Z axis
                                colorIdx = data[i],
                            };
                            tot++;
                        }
                    }
                    voxelProcedural.SetOffsets(offsets, tot);
                    started = false;
                    Debug.Log("ALL FINISH");
                    pause = true;
                    break;
                }
                case ObserveResult.Progress:
                {

                   break;
                }
                case ObserveResult.Contradiction:
                {
                    started = false;
                    Debug.Log("CONTRADICTION");
                    pause = true;
                    break;
                }
                default:
                    pause = true;
                    break;
            }
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(30, 30, 300, 50), "Start"))
        {
            if (seed == 0)
            {
                model.Setup(Random.Range(0, int.MaxValue));
            }
            else
            {
               model.Setup(seed); 
            }
            started = true;
            pause = false;
        }

        if (GUI.Button(new Rect(30, 100, 300, 50), "ToggleStop"))
        {
            pause = !pause;
        }
    }
}
