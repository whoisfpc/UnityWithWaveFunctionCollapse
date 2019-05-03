/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Collections.Generic;

namespace WaveFunctionCollapse
{
    public abstract class Model3D
    {
        protected bool[][] wave; // wave[FMX*FMY][T]，每个元素代表某个pattern在某个像素位置的状态，true代表not forbidden, false 代表forbidden，初始状态为true

        protected int[][][] propagator; //propagator[4][T][<=T] 确定任意两个pattern在上下左右4个方向上各移动一格后是否重叠
        int[][][] compatible; // compatible[FMX*FMY][T][4]，compatible[i][t][d] 对于每个像素i，对于pattern t，方向d上兼容的pattern数量
        protected int[] observed; // 保留最后结果的数组，保存的时pattern的index

        Stack<Tuple<int, int>> bannedStack; // Tuple<int, int>存的被ban掉的像素位置索引和对应的pattern索引

        protected Random random;
        public int FMX, FMY, FMZ, TotalOutputCount, FMO; //  输出图片的width和height
        protected int patternCount; // 不重复的pattern数量

        protected double[] weights; // 记录了每个pattern的出现次数，取决于symmetry的值，还会考虑旋转和反射后的量
        double[] weightLogWeights; // weightLogWeights[i] = weights[i] * Math.Log(weights[i]);
        double sumOfWeights, sumOfWeightLogWeights, startingEntropy;

        double[] distribution; // 每个可选的pattern占的权重
        int[] sumsOfOnes; // sumsOfOnes[FMX*FMY] 初始值为 pattern 总数
        double[] sumsOfWeights;// 初始值为 weights和
        double[] sumsOfWeightLogWeights; // 初始值为weightlogweights和
        double[] entropies; // 初始值为startingEntropy

        private bool setuped;

        public bool done;

        protected Model3D(int FMX, int FMY,int FMZ)
        {
            this.FMX = FMX;
            this.FMY = FMY;
            this.FMZ = FMZ;
            this.FMO = FMX * FMY;
            this.TotalOutputCount = FMX * FMY * FMZ;
            observed = new int[TotalOutputCount];
        }

        protected int CoordsToFlat(int x, int y, int z) => x + y * FMX + z * FMO;
        protected (int x, int y, int z) FlatToCoords(int i) => (i % FMO % FMX, i % FMO / FMX, i / FMO);

        void Init()
        {
            wave = new bool[TotalOutputCount][];
            compatible = new int[wave.Length][][];
            for (int i = 0; i < wave.Length; i++)
            {
                wave[i] = new bool[patternCount];
                compatible[i] = new int[patternCount][];
                for (int t = 0; t < patternCount; t++)
                    compatible[i][t] = new int[DIRECTION_COUNT];
            }

            distribution = new double[patternCount];
            weightLogWeights = new double[patternCount];
            sumOfWeights = 0;
            sumOfWeightLogWeights = 0;

            for (int t = 0; t < patternCount; t++)
            {
                weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
                sumOfWeights += weights[t];
                sumOfWeightLogWeights += weightLogWeights[t];
            }

            startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

            sumsOfOnes = new int[TotalOutputCount];
            sumsOfWeights = new double[TotalOutputCount];
            sumsOfWeightLogWeights = new double[TotalOutputCount];
            entropies = new double[TotalOutputCount];

            bannedStack = new Stack<Tuple<int, int>>(wave.Length);
        }

        ObserveResult Observe()
        {
            double min = 1E+3; // 满足条件的最小熵
            int argmin = -1; // 最小熵对应的index

            for (int i = 0; i < wave.Length; i++)
            {
                var (x, y, z) = FlatToCoords(i);
                if (OnBoundary(x, y, z))
                {
                    continue;
                }
                int amount = sumsOfOnes[i];
                if (amount == 0)
                {
                    return ObserveResult.Contradiction; // CONTRADICTION
                }

                double entropy = entropies[i];
                // 只有在可选pattern不止一个，且熵小于当前最小值时才考虑
                if (amount > 1 && entropy <= min)
                {
                    // noise用于在第一次observe时选择随机的i，而不是每次都是第一个
                    double noise = 1E-6 * random.NextDouble();
                    if (entropy + noise < min)
                    {
                        min = entropy + noise;
                        argmin = i;
                    }
                }
            }

            // 如果没有满足条件的index，则从wave中选一个可取的返回
            // 代表整张图只剩下最后一个像素位置, completely observed state
            if (argmin == -1)
            {
                done = true;
                for (int i = 0; i < wave.Length; i++)
                {
                    for (int t = 0; t < patternCount; t++)
                    {
                        if (wave[i][t])
                        {
                            observed[i] = t;
                            break;
                        }
                    }
                }
                return ObserveResult.Finish; // FINISH
            }

            for (int t = 0; t < patternCount; t++)
            {
                distribution[t] = wave[argmin][t] ? weights[t] : 0;
            }

            // 从中按照每个pattern的概率选择一个pattern
            int r = distribution.Random(random.NextDouble());

            for (int t = 0; t < patternCount; t++)
            {
                if (wave[argmin][t] && t != r)
                {
                    Ban(argmin, t);
                }
            }

            return ObserveResult.Progress; // PROGRESS
        }

        protected void Propagate()
        {
            while (bannedStack.Count > 0)
            {
                var (i, t) = bannedStack.Pop();

                var (x1, y1, z1) = FlatToCoords(i);

                // 对当前位置，上下左右四个方向
                for (int d = 0; d < DIRECTION_COUNT; d++)
                {
                    int dx = DX[d], dy = DY[d], dz = DZ[d];
                    int x2 = x1 + dx, y2 = y1 + dy, z2 = z1 + dz;
                    if (OnBoundary(x2, y2, z2))
                    {
                        continue;
                    }

                    // 以下两个if，其实只有在输出图像是periodic时才会走到
                    if (x2 < 0)
                    {
                        x2 += FMX;
                    }
                    else if (x2 >= FMX)
                    {
                        x2 -= FMX;
                    }
                    if (y2 < 0)
                    {
                        y2 += FMY;
                    }
                    else if (y2 >= FMY)
                    {
                        y2 -= FMY;
                    }
                    if (z2 < 0)
                    {
                        z2 += FMZ;
                    }
                    else if (z2 >= FMZ)
                    {
                        z2 -= FMZ;
                    }

                    int i2 = CoordsToFlat(x2, y2, z2);
                    int[] p = propagator[d][t]; // ban掉的pattern在方向d上可选的pattern列表
                    int[][] compat = compatible[i2]; // 像素i2位置，对于pattern T，可兼容的pattern数量列表

                    for (int l = 0; l < p.Length; l++)
                    {
                        int t2 = p[l];
                        int[] comp = compat[t2];

                        comp[d]--;
                        if (comp[d] == 0) Ban(i2, t2);
                    }
                }
            }
        }

        public void Setup(int seed)
        {
            if (wave == null)
            {
                Init();
            }
            Clear();
            random = new Random(seed);
            setuped = true;
        }

        public ObserveResult Forward(int limit)
        {
            if (!setuped)
            {
                return ObserveResult.Contradiction;
            }

            for (int l = 0; l < limit || limit == 0; l++)
            {
                var result = Observe();
                if (result != ObserveResult.Progress)
                {
                    setuped = false;
                    return result;
                }
                Propagate();
            }

            return ObserveResult.Progress;
        }

        // seed: 随机种子, limit: 限制observe次数，只至多确定limit数量的像素点
        public ObserveResult Run(int seed, int limit)
        {
            if (wave == null) Init();

            Clear();
            random = new Random(seed);

            for (int l = 0; l < limit || limit == 0; l++)
            {
                var result = Observe();
                if (result != ObserveResult.Progress) return result;
                Propagate();
            }

            return ObserveResult.Finish;
        }

        // forbid wave[i][t] and update entropies[i]
        protected void Ban(int i, int t)
        {
            wave[i][t] = false;
            bannedStack.Push(Tuple.Create(i, t));

            int[] comp = compatible[i][t];
            for (int d = 0; d < DIRECTION_COUNT; d++)
            {
                comp[d] = 0;
            }

            double sum = sumsOfWeights[i];
            entropies[i] += sumsOfWeightLogWeights[i] / sum - Math.Log(sum);

            // 每个像素位置剔除当前pattern的影响
            sumsOfOnes[i] -= 1;
            sumsOfWeights[i] -= weights[t];
            sumsOfWeightLogWeights[i] -= weightLogWeights[t];

            sum = sumsOfWeights[i];
            entropies[i] -= sumsOfWeightLogWeights[i] / sum - Math.Log(sum);
        }

        protected virtual void Clear()
        {
            done = false;
            for (int i = 0; i < wave.Length; i++)
            {
                for (int t = 0; t < patternCount; t++)
                {
                    wave[i][t] = true; // set all wave[i][t] not forbidden
                                       // 确定每个像素i，对于pattern t，方向d上兼容的pattern数量
                    for (int d = 0; d < DIRECTION_COUNT; d++)
                        compatible[i][t][d] = propagator[opposite[d]][t].Length;
                }

                sumsOfOnes[i] = weights.Length; // pattern 总数
                sumsOfWeights[i] = sumOfWeights; // weights和
                sumsOfWeightLogWeights[i] = sumOfWeightLogWeights; // logweights和
                entropies[i] = startingEntropy; // 起始熵
            }
        }

        protected abstract bool OnBoundary(int x, int y, int z);

        protected const int DIRECTION_COUNT = 6;
        protected static int[] DX = { -1, 0, 1, 0, 0, 0 };
        protected static int[] DY = { 0, 1, 0, -1, 0, 0 };
        protected static int[] DZ = { 0, 0, 0, 0, -1, 1 };
        static int[] opposite = { 2, 3, 0, 1, 5, 4 }; //反方向索引
    }
}
