using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplyShader : MonoBehaviour
{
    public int width, height;

    private Texture2D texture;

    private Color32[] colors;

    private int i = 0;

    private void Start()
    {
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var mat = GetComponent<Renderer>().material;
        mat.mainTexture = texture;
        colors = new Color32[width * height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = new Color32(255, 0, 0, 1);
        }
        texture.SetPixels32(colors);
        texture.Apply();
    }

    private void Update()
    {
        colors[i].r = (byte)Random.Range(0, 256);
        colors[i].g = (byte)Random.Range(0, 256);
        colors[i].b = (byte)Random.Range(0, 256);
        i++;
        if (i == colors.Length) i = 0;

        texture.SetPixels32(colors);
        texture.Apply();
    }
}
