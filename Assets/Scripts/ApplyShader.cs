using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ApplyShader : MonoBehaviour
{
    public int width, height;

    public int sleepMilliseconds = 100; 

    private Texture2D texture;

    private Color32[] colors;

    private CancellationTokenSource ts;

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
            colors[i] = new Color32(255, 0, 0, 255);
        }
        texture.SetPixels32(colors);
        texture.Apply();

        ts = new CancellationTokenSource();
        CancellationToken ct = ts.Token;
        Task.Factory.StartNew(() =>
        {
            int i = 0;
            var random = new System.Random();
            while (true)
            {
                if (ct.IsCancellationRequested)
                {
                    Debug.Log("task canceled");
                    break;
                }
                lock (colors)
                {
                    colors[i].r = (byte)random.Next(256);
                    colors[i].g = (byte)random.Next(256);
                    colors[i].b = (byte)random.Next(256);
                    i++;
                    if (i == colors.Length) i = 0;
                }
                Thread.Sleep(sleepMilliseconds);
            }
        }, ct);

    }

    private void Update()
    {
        lock (colors)
        {
            texture.SetPixels32(colors);
            texture.Apply();
        }
    }

    private void OnDestroy()
    {
        ts.Cancel();
    }
}
