using UnityEngine;
using VoxDecoder;

public class VoxelLoader : MonoBehaviour
{

    public string voxelPath;

    public VoxelPresent prefab;

    private VoxReader voxReader;

    private void Start()
    {
        voxReader = new VoxReader();
    }

    private void LoadVoxel()
    {
        if (!prefab)
        {
            Debug.LogError("Prefab is null");
            return;
        }

        if (string.IsNullOrWhiteSpace(voxelPath))
        {
            Debug.LogError("voxel path is invalied");
            return;
        }

        string path = Application.dataPath + "/" + voxelPath;
        var voxelFile = voxReader.ParseFile(path);
        foreach(var model in voxelFile.models)
        {
            foreach(var voxel in model.voxels)
            {
                // Y is up in unity, but Z is up in magicavoxel
                var obj = Instantiate(prefab, new Vector3(voxel.X, voxel.Z, voxel.Y), Quaternion.identity, transform);
                var color = voxelFile.palette.colors[voxel.I];
                obj.color = new Color32(color.R, color.G, color.B, color.A);
            }
        }
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(30, 30, 300, 50), "Load"))
        {
            LoadVoxel();
        }
    }
}
