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
    public class OverlappingModel3D : Model3D
    {
        int N; // Sample Length
        int N2;
        Pattern[] patterns;
        Vector3[] colors; // unique colors

        public OverlappingModel3D(byte[,,] sample, Vector3[] colors, int N, int FMX, int FMY, int FMZ)
            : base(FMX, FMY, FMZ)
        {
            this.colors = colors;
            this.N = N; // Sample Length
            this.N2 = N * N;
            var SMX = sample.GetLength(0);
            var SMY = sample.GetLength(1);
            var SMZ = sample.GetLength(2);

            // Sample a raw pattern from sample
            byte[] patternFromSample(int x0, int y0, int z0)
            {
                byte[] result = new byte[N * N * N];
                for (int dz = 0; dz < N; dz++)
                {
                    for (int dy = 0; dy < N; dy++)
                    {
                        for (int dx = 0; dx < N; dx++)
                        {
                            var x = x0 + dx;
                            var y = y0 + dy;
                            var z = z0 + dz;
                            result[dx + dy * N + dz * N2] = sample[x % SMX, y % SMY, z % SMZ];
                        }
                    }
                }
                return result;
            };

            Dictionary<Pattern, int> patternDict = new Dictionary<Pattern, int>();

            for (int z = 0; z < (SMZ - N + 1); z++)
            {
                for (int y = 0; y < (SMY - N + 1); y++)
                {
                    for (int x = 0; x < (SMX - N + 1); x++)
                    {
                        // 对于采样的每个pattern，得到另3个旋转后的pattern，以及每个的反射后的pattern
                        var ps = patternFromSample(x, y, z);

                        Pattern pt = new Pattern(ps);
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

            patternCount = patternDict.Count;
            patterns = new Pattern[patternCount];
            weights = new double[patternCount]; // 记录了每个pattern的出现次数，取决于symmetry，还会考虑旋转和反射后的量

            foreach (var kv in patternDict)
            {
                patterns[kv.Key.idx] = kv.Key;
                weights[kv.Key.idx] = kv.Value;
            }

            // 判断pattern1在移动(dx,dy)后，是否和pattern2重叠
            bool agrees(byte[] p1, byte[] p2, int dx, int dy, int dz)
            {
                int xmin = dx < 0 ? 0 : dx;
                int xmax = dx < 0 ? dx + N : N;
                int ymin = dy < 0 ? 0 : dy;
                int ymax = dy < 0 ? dy + N : N;
                int zmin = dz < 0 ? 0 : dz;
                int zmax = dz < 0 ? dz + N : N;
                for (int z = zmin; z < zmax; z++)
                {
                    for (int y = ymin; y < ymax; y++)
                    {
                        for (int x = xmin; x < xmax; x++)
                        {
                            if (p1[x + N * y + N2 * z] != p2[x - dx + N * (y - dy) + N2 * (z - dz)]) 
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            };

            propagator = new int[DIRECTION_COUNT][][];
            for (int d = 0; d < DIRECTION_COUNT; d++)
            {
                propagator[d] = new int[patternCount][];
                for (int t = 0; t < patternCount; t++)
                {
                    List<int> list = new List<int>();
                    for (int t2 = 0; t2 < patternCount; t2++)
                    {
                        if (agrees(patterns[t].bytes, patterns[t2].bytes, DX[d], DY[d], DZ[d]))
                        {
                            list.Add(t2);
                        }
                    }
                    propagator[d][t] = list.ToArray();
                }
            }
        }

        protected override bool OnBoundary(int x, int y, int z) => (x + N > FMX || y + N > FMY || z + N > FMZ || x < 0 || y < 0 || z < 0);

        protected override void Clear()
        {
            base.Clear();
        }

        public void Capture(byte[] data)
        {
            if (data == null || data.Length != FMX * FMY * FMZ)
            {
                return;
            }

            if (done)
            {
                for (int z = 0; z < FMZ; z++)
                {
                    int dz = z < FMZ - N + 1 ? 0 : N - 1;
                    for (int y = 0; y < FMY; y++)
                    {
                        int dy = y < FMY - N + 1 ? 0 : N - 1;
                        for (int x = 0; x < FMX; x++)
                        {
                            int dx = x < FMX - N + 1 ? 0 : N - 1;
                            byte colorIdx = patterns[observed[CoordsToFlat(x - dx, y - dy, z - dz)]].bytes[dx + dy * N + dz * N2];
                            data[CoordsToFlat(x, y, z)] = colorIdx;
                        }
                    }
                }
            }
        }
    }
}
