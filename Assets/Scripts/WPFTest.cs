using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaveFunctionCollapse;

public class WPFTest : MonoBehaviour {

    [Header("Random")]
    public int seed;

    public int step = 1;

    public int N = 2;

    public int width = 48;
    public int height = 48;
    public bool periodicInput = true;
    public bool periodicOutput = false;
    public int symmetry = 8;
    public int ground = 0;

    public int limit = 0;

    public Texture2D texture;

    private Texture2D outputTexture;

    private Color32[] colors;

    private Material mat;

    private OverlappingModel model;

    private bool started = false, pause = true;

    private void Start()
    {
        outputTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        outputTexture.filterMode = FilterMode.Point;
        outputTexture.wrapMode = TextureWrapMode.Clamp;
        colors = outputTexture.GetPixels32();

        mat = GetComponent<Renderer>().material;
        mat.mainTexture = outputTexture;
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
                    model.Capture(colors);
                    outputTexture.SetPixels32(colors);
                    outputTexture.Apply();
                    started = false;
                    Debug.Log("ALL FINISH");
                    pause = true;
                    break;
                }
                case ObserveResult.Progress:
                {
                    model.Capture(colors);
                    outputTexture.SetPixels32(colors);
                    outputTexture.Apply();
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
            model = new OverlappingModel(texture, N, width, height, periodicInput, periodicOutput, symmetry, ground);
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
        
            //for (int k = 0; k < 2; k++)
            //{
            //    int seed = Random.Range(0, int.MaxValue);
            //    bool finished = model.Run(seed, limit);
            //    if (finished)
            //    {
            //        Debug.Log("FINISH");

            //        model.Capture(colors);

            //        outputTexture.SetPixels32(colors);
            //        outputTexture.Apply();

            //        break;
            //    }
            //    else Debug.Log("CONTRADICTION");
            //}
        }

        if (GUI.Button(new Rect(30, 100, 300, 50), "ToggleStop"))
        {
            pause = !pause;
        }
    }
}
