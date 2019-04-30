/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{

    public class Pattern : IEquatable<Pattern>
    {
        public readonly int N;

        public int idx;

        public byte[] bytes;

        public Pattern(byte[] patternBytes)
        {
            this.bytes = patternBytes;
            N = patternBytes.Length;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash = hash * 31 + bytes[i];
            }
            return hash;
        }

        public bool Equals(Pattern other)
        {
            if (bytes.Length != other.bytes.Length)
            {
                return false;
            }
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != other.bytes[i])
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object other)
        {
            if (!(other is Pattern))
            {
                return false;
            }
            return Equals((Pattern)other);
        }
    }

    public class OverlappingModel : Model
    {
        int N; // Sample Length
        Pattern[] patterns;
        List<Color32> colors; // unique colors
        int ground;

        public OverlappingModel(byte[,] sample, List<Color32> colors, int N, int outputWidth, int outputHeight, bool periodicInput, bool periodicOutput, int symmetry, int ground)
            : base(outputWidth, outputHeight)
        {
            this.colors = colors;
            this.N = N; // Sample Length
            periodic = periodicOutput; // whether output graphics is periodic
            var SMX = sample.GetLength(0);
            var SMY = sample.GetLength(1);

            // Process raw patterns, return a new pattern (N*N byte array)
            byte[] getPattern (Func<int, int, byte>  f)
            {
                byte[] result = new byte[N * N];
                for (int y = 0; y < N; y++)
                {
                    for (int x = 0; x < N; x++)
                    {
                        result[x + y * N] = f(x, y);
                    }
                }
                return result;
            };

            // Sample a raw pattern from sample
            byte[] patternFromSample(int x, int y) => getPattern((dx, dy) => sample[(x + dx) % SMX, (y + dy) % SMY]);
            // Rotate a pattern (anti-clockwise)
            byte[] rotate(byte[] p) => getPattern((x, y) => p[N - 1 - y + x * N]);
            // Reflect a pattern
            byte[] reflect(byte[] p) => getPattern((x, y) => p[N - 1 - x + y * N]);

            // Dictionary<long, int> weights = new Dictionary<long, int>();
            Dictionary<Pattern, int> patternDict = new Dictionary<Pattern, int>();

            for (int y = 0; y < (periodicInput ? SMY : SMY - N + 1); y++)
            {
                for (int x = 0; x < (periodicInput ? SMX : SMX - N + 1); x++)
                {
                    byte[][] ps = new byte[8][];

                    // 对于采样的每个pattern，得到另3个旋转后的pattern，以及每个的反射后的pattern
                    ps[0] = patternFromSample(x, y);
                    ps[1] = reflect(ps[0]);
                    ps[2] = rotate(ps[0]);
                    ps[3] = reflect(ps[2]);
                    ps[4] = rotate(ps[2]);
                    ps[5] = reflect(ps[4]);
                    ps[6] = rotate(ps[4]);
                    ps[7] = reflect(ps[6]);

                    // symmetry决定了是否将pattern旋转反射后的新pattern纳入pattern列表中
                    for (int k = 0; k < symmetry; k++)
                    {
                        Pattern pt = new Pattern(ps[k]);
                        if (patternDict.ContainsKey(pt))
                        {
                            patternDict[pt]++;
                        }
                        else
                        {
                            pt.idx = patternDict.Count;
                            patternDict.Add(pt, 1);
                        }
                    }
                }
            }

            // patternCount = weights.Count;// 不重复的pattern数量
            patternCount = patternDict.Count;
            this.ground = (ground + patternCount) % patternCount; // ground输入是负的
            patterns = new Pattern[patternCount];
            weights = new double[patternCount]; // 记录了每个pattern的出现次数，取决于symmetry，还会考虑旋转和反射后的量

            foreach (var kv in patternDict)
            {
                patterns[kv.Key.idx] = kv.Key;
                weights[kv.Key.idx] = kv.Value;
            }

            // 判断pattern1在移动(dx,dy)后，是否和pattern2重叠
            bool agrees(byte[] p1, byte[] p2, int dx, int dy)
            {
                int xmin = dx < 0 ? 0 : dx;
                int xmax = dx < 0 ? dx + N : N;
                int ymin = dy < 0 ? 0 : dy;
                int ymax = dy < 0 ? dy + N : N;
                for (int y = ymin; y < ymax; y++)
                {
                    for (int x = xmin; x < xmax; x++)
                    {
                        if (p1[x + N * y] != p2[x - dx + N * (y - dy)]) 
                        {
                        return false;
                        }
                    }
                }
                return true;
            };

            propagator = new int[4][][];
            // 确定任意两个pattern在上下左右4个方向上各移动一格后是否重叠(这里有简化，实际overlap的可能有(2*N-1)^2个)
            for (int d = 0; d < 4; d++)
            {
                propagator[d] = new int[patternCount][];
                for (int t = 0; t < patternCount; t++)
                {
                    List<int> list = new List<int>();
                    for (int t2 = 0; t2 < patternCount; t2++)
                        if (agrees(patterns[t].bytes, patterns[t2].bytes, DX[d], DY[d])) list.Add(t2);

                    propagator[d][t] = list.ToArray();
                }
            }
        }

        //检测给定的的位置是否到达边界（不够N*N的大小），输出图像为periodic时始终返回false
        protected override bool OnBoundary(int x, int y) => !periodic && (x + N > FMX || y + N > FMY || x < 0 || y < 0);

        protected override void Clear()
        {
            base.Clear();

            if (ground != 0)
            {
                for (int x = 0; x < FMX; x++)
                {
                    // 对于最后一行，只保留ground pattern
                    for (int t = 0; t < patternCount; t++)
                        if (t != ground)
                            Ban(x + (FMY - 1) * FMX, t);

                    // 对于其他所有的位置，去除ground pattern
                    for (int y = 0; y < FMY - 1; y++)
                        Ban(x + y * FMX, ground);
                }

                Propagate();
            }
        }
        
        public void Capture(Color32[] bitmapData)
        {
            if (bitmapData == null || bitmapData.Length != FMX * FMY)
            {
                return;
            }

            if (done)
            {
                for (int y = 0; y < FMY; y++)
                {
                    int dy = y < FMY - N + 1 ? 0 : N - 1;
                    for (int x = 0; x < FMX; x++)
                    {
                        int dx = x < FMX - N + 1 ? 0 : N - 1;
                        int colorIdx = patterns[observed[x - dx + (y - dy) * FMX]].bytes[dx + dy * N];
                        Color32 c = colors[colorIdx];
                        bitmapData[x + y * FMX] = c;
                    }
                }
            }
            else
            {
                for (int i = 0; i < wave.Length; i++)
                {
                    int contributors = 0;
                    int r = 0, g = 0, b = 0;
                    int x = i % FMX, y = i / FMX;

                    for (int dy = 0; dy < N; dy++)
                    {
                        for (int dx = 0; dx < N; dx++)
                        {
                            int sx = x - dx;
                            if (sx < 0) sx += FMX;

                            int sy = y - dy;
                            if (sy < 0) sy += FMY;

                            int s = sx + sy * FMX;
                            if (OnBoundary(sx, sy)) continue;
                            for (int t = 0; t < patternCount; t++)
                            {
                                if (wave[s][t])
                                {
                                    contributors++;
                                    Color32 color = colors[patterns[t].bytes[dx + dy * N]];
                                    r += color.r;
                                    g += color.g;
                                    b += color.b;
                                }
                            }
                        }
                    }

                    bitmapData[i] = new Color32();
                    bitmapData[i].a = 0xFF;
                    bitmapData[i].r = (byte)(r / contributors);
                    bitmapData[i].g = (byte)(g / contributors);
                    bitmapData[i].b = (byte)(b / contributors);
                }
            }

            // flip Y-axis
            for (int y = 0; y < FMY / 2; y++)
            {
                for (int x = 0; x < FMX; x++)
                {
                    var tmp = bitmapData[x + y * FMX];
                    bitmapData[x + y * FMX] = bitmapData[x + (FMY - 1 - y) * FMX];
                    bitmapData[x + (FMY - 1 - y) * FMX] = tmp;
                }
            }
        }
    }
}
