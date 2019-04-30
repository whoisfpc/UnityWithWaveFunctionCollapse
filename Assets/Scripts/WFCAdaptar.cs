using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaveFunctionCollapse;

public class WFCAdaptar
{
    public List<Color32> colorPalette; // unique colors
    public byte[,] sample;

    public WFCAdaptar(Texture2D texture)
    {
        colorPalette = new List<Color32>();
        var width = texture.width;
        var height = texture.height;
        var allColors = texture.GetPixels32();
        sample = new byte[width, height];
        // Record color, and make a color-index list
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color32 color = allColors[x + (height - 1 - y) * width];
                int idx = colorPalette.IndexOf(color);
                if (idx == -1)
                {
                    sample[x, y] = (byte)colorPalette.Count;
                    colorPalette.Add(color);
                }
                else
                {
                    sample[x, y] = (byte)idx;
                }
            }
        }
    }
}
