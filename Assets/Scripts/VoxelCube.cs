using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelCube : MonoBehaviour
{

    public Color color = Color.white;

    private MaterialPropertyBlock materialPB;
    private Renderer render;

    private void Start()
    {
        materialPB = new MaterialPropertyBlock();
        render = GetComponent<Renderer>();

        render.GetPropertyBlock(materialPB);
        materialPB.SetColor("_Color", color);
        render.SetPropertyBlock(materialPB);
    }

}