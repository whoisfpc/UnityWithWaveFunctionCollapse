/*
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;

namespace WaveFunctionCollapse
{
    public abstract class Model
    {
        protected bool[][] wave; // wave[FMX*FMY][T]，每个元素代表某个pattern在某个像素位置的状态，true代表not forbidden, false 代表forbidden，初始状态为true

        protected int[][][] propagator; //propagator[4][T][<=T] 确定任意两个pattern在上下左右4个方向上各移动一格后是否重叠
        int[][][] compatible; // compatible[FMX*FMY][T][4]，compatible[i][t][d] 对于每个像素i，对于pattern t，方向d上兼容的pattern数量
        protected int[] observed; // 保留最后结果的数组，保存的时pattern的index

        Tuple<int, int>[] stack; // 只是模拟一个stack，Tuple<int, int>存的时被ban掉的像素位置索引和对应的pattern索引
        int stacksize;

        protected Random random;
        protected int FMX, FMY; //  输出图片的width和height
        protected int T; // 不重复的pattern数量
        protected bool periodic; // whether output graphics is periodic

        protected double[] weights; // 记录了每个pattern的出现次数，取决于symmetry的值，还会考虑旋转和反射后的量
        double[] weightLogWeights; // weightLogWeights[i] = weights[i] * Math.Log(weights[i]);
        double sumOfWeights, sumOfWeightLogWeights, startingEntropy;

        int[] sumsOfOnes; // sumsOfOnes[FMX*FMY] 初始值为 pattern 总数
        double[] sumsOfWeights;// 初始值为 weights和
        double[] sumsOfWeightLogWeights; // 初始值为weightlogweights和
        double[] entropies; // 初始值为startingEntropy

        private bool setuped;

        protected Model(int width, int height)
        {
            FMX = width;
            FMY = height;
        }

        void Init()
        {
            wave = new bool[FMX * FMY][];
            compatible = new int[wave.Length][][];
            for (int i = 0; i < wave.Length; i++)
            {
                wave[i] = new bool[T];
                compatible[i] = new int[T][];
                for (int t = 0; t < T; t++)
                    compatible[i][t] = new int[4];
            }

            weightLogWeights = new double[T];
            sumOfWeights = 0;
            sumOfWeightLogWeights = 0;

            for (int t = 0; t < T; t++)
            {
                weightLogWeights[t] = weights[t] * Math.Log(weights[t]);
                sumOfWeights += weights[t];
                sumOfWeightLogWeights += weightLogWeights[t];
            }

            startingEntropy = Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;

            sumsOfOnes = new int[FMX * FMY];
            sumsOfWeights = new double[FMX * FMY];
            sumsOfWeightLogWeights = new double[FMX * FMY];
            entropies = new double[FMX * FMY];

            stack = new Tuple<int, int>[wave.Length * T];
            stacksize = 0;
        }

        bool? Observe()
        {
            double min = 1E+3; // 满足条件的最小熵
            int argmin = -1; // 最小熵对应的index

            for (int i = 0; i < wave.Length; i++)
            {
                // i % FMX = x, i / FMY = y
                if (OnBoundary(i % FMX, i / FMX)) continue;

                int amount = sumsOfOnes[i];
                if (amount == 0) return false; // CONTRADICTION

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
                observed = new int[FMX * FMY];
                for (int i = 0; i < wave.Length; i++)
                    for (int t = 0; t < T; t++)
                        if (wave[i][t])
                        {
                            observed[i] = t; break;
                        }

                return true; // FINISH
            }

            // 每个可选的pattern占的权重
            double[] distribution = new double[T];
            for (int t = 0; t < T; t++)
                distribution[t] = wave[argmin][t] ? weights[t] : 0;

            // 从中按照每个pattern的概率选择一个pattern
            int r = distribution.Random(random.NextDouble());

            for (int t = 0; t < T; t++)
                if (wave[argmin][t] != (t == r))
                    Ban(argmin, t);

            return null; // PROGRESS
        }

        protected void Propagate()
        {
            while (stacksize > 0)
            {
                var e1 = stack[stacksize - 1];
                stacksize--;

                int i1 = e1.Item1; // i1为ban掉的某个像素位置索引
                int x1 = i1 % FMX, y1 = i1 / FMX; // 对应的坐标

                // 对当前位置，上下左右四个方向
                for (int d = 0; d < 4; d++)
                {
                    int dx = DX[d], dy = DY[d];
                    int x2 = x1 + dx, y2 = y1 + dy;
                    if (OnBoundary(x2, y2)) continue;

                    // 一下两个if，其实只有在输出图像是periodic时才会走到
                    if (x2 < 0) x2 += FMX;
                    else if (x2 >= FMX) x2 -= FMX;
                    if (y2 < 0) y2 += FMY;
                    else if (y2 >= FMY) y2 -= FMY;

                    int i2 = x2 + y2 * FMX;
                    int[] p = propagator[d][e1.Item2]; // ban掉的pattern在方向d上可选的pattern列表
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
            if (wave == null) Init();

            Clear();
            random = new Random(seed);

            setuped = true;
        }

        public bool? Forward(int limit)
        {
            if (!setuped)
            {
                UnityEngine.Debug.Log("MUST SETUP BEFORE FORWARD!");
                return false;
            }

            for (int l = 0; l < limit || limit == 0; l++)
            {
                bool? result = Observe();
                if (result != null)
                {
                    setuped = false;
                    return result.Value;
                }
                Propagate();
            }

            return null;
        }

        // seed: 随机种子, limit: 限制observe次数，只至多确定limit数量的像素点
        public bool Run(int seed, int limit)
        {
            if (wave == null) Init();

            Clear();
            random = new Random(seed);

            for (int l = 0; l < limit || limit == 0; l++)
            {
                bool? result = Observe();
                if (result != null) return result.Value;
                Propagate();
            }

            return true;
        }

        // forbid wave[i][t] and update entropies[i]
        protected void Ban(int i, int t)
        {
            wave[i][t] = false;

            int[] comp = compatible[i][t];
            for (int d = 0; d < 4; d++) comp[d] = 0;
            stack[stacksize] = new Tuple<int, int>(i, t);
            stacksize++;

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
            for (int i = 0; i < wave.Length; i++)
            {
                for (int t = 0; t < T; t++)
                {
                    wave[i][t] = true; // set all wave[i][t] not forbidden
                                       // 确定每个像素i，对于pattern t，方向d上兼容的pattern数量
                    for (int d = 0; d < 4; d++)
                        compatible[i][t][d] = propagator[opposite[d]][t].Length;
                }

                sumsOfOnes[i] = weights.Length; // pattern 总数
                sumsOfWeights[i] = sumOfWeights; // weights和
                sumsOfWeightLogWeights[i] = sumOfWeightLogWeights; // logweights和
                entropies[i] = startingEntropy; // 起始熵
            }
        }

        protected abstract bool OnBoundary(int x, int y);
        public abstract UnityEngine.Texture2D Graphics();

        protected static int[] DX = { -1, 0, 1, 0 };
        protected static int[] DY = { 0, 1, 0, -1 };
        static int[] opposite = { 2, 3, 0, 1 }; //反方向索引
    }
}
