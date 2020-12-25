using UnityEngine;
using System;
using System.Collections.Generic;

using Ceto.Common.Threading.Tasks;

namespace Ceto
{
	
    public class FourierCPU
    {

		struct LookUp
		{
			public int originX, originY;
			public int targetX, targetY;
            public float wr, wi; //OYM:  实部与虚部
        }

		public int size { get { return m_passSize; } }
		int m_passSize;
		float m_fsize;
		
		public int passes { get { return m_passes; } }
		int m_passes;

		LookUp[] m_butterflyLookupTable = null;

        public FourierCPU(int size)
        {
            //OYM:  cpu傅里叶变幻
            if (!Mathf.IsPowerOfTwo(size))
				throw new ArgumentException("Fourier grid size must be pow2 number");

            m_passSize = size;
            m_fsize = (float)m_passSize;
            m_passes = (int)(Mathf.Log(m_fsize) / Mathf.Log(2.0f)); //OYM:  计算底数2,这里也是快速傅里叶变幻?
            ComputeButterflyLookupTable(); //OYM:  这里是蝶形算法
                                           //OYM:  我晚点再来看这部分
        }


        static int BitReverse(int i,int size)
        { //OYM:  总之就是比特翻转
            int j = i;
            int Sum = 0;
            int W = 1;
            int M = size / 2;
            while (M != 0)
            {
                j = ((i & M) > M - 1) ? 1 : 0;//OYM:  这个地方写的和坨稀一样,写个I&M>0不好吗
                Sum += j * W;
                W *= 2;
                M /= 2;
            }
			return Sum;
        }
        //OYM:  这种核心代码不写注释的真的操蛋
        void ComputeButterflyLookupTable()
        {
            m_butterflyLookupTable = new LookUp[m_passSize * m_passes]; //OYM:  创建一个蝶形算法的通道映射
			int chunkSize = m_passSize;

			for (int passIndex = 0; passIndex < m_passes; passIndex++) //OYM:  对于每一个通道而言
            {
                int lengthX = (int)Mathf.Pow(2, m_passes - 1 - passIndex); //OYM:  区块位置?
                int lengthY = (int)Mathf.Pow(2, passIndex); //OYM:  该区块的数量

                for (int X = 0; X < lengthX; X++)
                {
                    for (int Y = 0; Y < lengthY; Y++)
                    {
                        int iX, iY, jX, jY;
                        if (passIndex == 0) //OYM:  如果是零通道就翻转比特位
                        {
                            iX = X * lengthY * 2 + Y;
                            iY = X * lengthY * 2 + lengthY + Y;
                            jX = BitReverse(iX,m_passSize);//OYM:  翻转比特
                            jY = BitReverse(iY,m_passSize);
                        }
                        else
                        {
                            iX = X * lengthY * 2 + Y;
                            iY = X * lengthY * 2 + lengthY + Y;
                            jX = iX;
                            jY = iY;
                        }

                        float wr = Mathf.Cos(2.0f * Mathf.PI * (float)(Y * lengthX) / m_fsize); //OYM:  lengthX是步长,fsize是周期?
						float wi = Mathf.Sin(2.0f * Mathf.PI * (float)(Y * lengthX) / m_fsize);

                        int offset1 = (iX + passIndex * m_passSize); //OYM:  写入的位置,注意Y的坐标跟X差了lengthY
						m_butterflyLookupTable[offset1].originX = iX;
						m_butterflyLookupTable[offset1].originY = -1;
						m_butterflyLookupTable[offset1].targetX = jX;
                        m_butterflyLookupTable[offset1].targetY = jY;
                        m_butterflyLookupTable[offset1].wr = wr;
                        m_butterflyLookupTable[offset1].wi = wi;

                        int offset2 = (iY + passIndex * m_passSize);

						m_butterflyLookupTable[offset2].originX = -1;
						m_butterflyLookupTable[offset2].originY = iY;
						m_butterflyLookupTable[offset2].targetX = jX;
                        m_butterflyLookupTable[offset2].targetY = jY;
                        m_butterflyLookupTable[offset2].wr = -wr;
                        m_butterflyLookupTable[offset2].wi = -wi;

                    }
                }
            }
        }

        //Performs two FFTs on two complex numbers packed in a vector4
        Vector4 FFT(Vector2 w, Vector4 input1, Vector4 input2)
        {
            input1.x += w.x * input2.x - w.y * input2.y;
            input1.y += w.y * input2.x + w.x * input2.y;
            input1.z += w.x * input2.z - w.y * input2.w;
            input1.w += w.y * input2.z + w.x * input2.w;

            return input1;
        }

        //Performs one FFT on a complex number
        Vector2 FFT(Vector2 w, Vector2 input1, Vector2 input2)
        {
            input1.x += w.x * input2.x - w.y * input2.y;
            input1.y += w.y * input2.x + w.x * input2.y;

            return input1;
        }

        public int PeformFFT_SinglePacked(int startIdx, IList<Vector4[]> data0, ICancelToken token) //OYM:  包装
        {
			
			int x; int y; int i;
			int idx = 0; int idx1; int bftIdx;
			int X; int Y;
			float wx, wy;
			int ii, xi, yi, si, sy;
			
			int j = startIdx;
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];

				Vector4[] read0 = data0[idx1];
				
				si = i * m_passSize;
				
				for (x = 0; x < m_passSize; x++)
				{
					
					bftIdx = x + si;
					
					X = m_butterflyLookupTable[bftIdx].targetX;
					Y = m_butterflyLookupTable[bftIdx].targetY;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;
					
					for (y = 0; y < m_passSize; y++)
					{
                        if (token.Cancelled) return -1;

						sy = y * m_passSize;
						
						ii = x + sy;
						xi = X + sy;
						yi = Y + sy;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;

					}
				}
			}
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				
				Vector4[] read0 = data0[idx1];
				
				si = i * m_passSize;
				
				for (y = 0; y < m_passSize; y++)
				{
					
					bftIdx = y + si;
					
					X = m_butterflyLookupTable[bftIdx].targetX * m_passSize;
					Y = m_butterflyLookupTable[bftIdx].targetY * m_passSize;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;
					
					for (x = 0; x < m_passSize; x++)
					{
                        if (token.Cancelled) return -1;

                        ii = x + y * m_passSize;
						xi = x + X;
						yi = x + Y;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
					}
				}
			}
			
			return idx;
		}

        public int PeformFFT_DoublePacked(int startIdx, IList<Vector4[]> data0, ICancelToken token) //OYM:  二次封装?
        {

			int x; int y; int i;
			int idx = 0; int idx1; int bftIdx;
			int X; int Y;
			float wx, wy;
			int ii, xi, yi, si, sy;
			
			int j = startIdx;
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];

				Vector4[] read0 = data0[idx1];

				si = i * m_passSize;

				for (x = 0; x < m_passSize; x++)
				{

					bftIdx = x + si;
					
					X = m_butterflyLookupTable[bftIdx].targetX;
					Y = m_butterflyLookupTable[bftIdx].targetY;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (y = 0; y < m_passSize; y++)
					{
                        if (token.Cancelled) return -1;

                        sy = y * m_passSize;

						ii = x + sy;
						xi = X + sy;
						yi = Y + sy;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						
					}
				}
			}
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];

				Vector4[] read0 = data0[idx1];

				si = i * m_passSize;

				for (y = 0; y < m_passSize; y++)
				{

					bftIdx = y + si;
					
					X = m_butterflyLookupTable[bftIdx].targetX * m_passSize;
					Y = m_butterflyLookupTable[bftIdx].targetY * m_passSize;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (x = 0; x < m_passSize; x++)
					{
                        if (token.Cancelled) return -1;

                        ii = x + y * m_passSize;
						xi = x + X;
						yi = x + Y;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
					}
				}
			}
			
			return idx;
		}

        /*
		public int PeformFFT(int startIdx, IList<Vector4[]> data0, IList<Vector4[]> data1)
		{
			
			
			int x; int y; int i;
			int idx = 0; int idx1; int bftIdx;
			int X; int Y;
			float wx, wy;
			int ii, xi, yi;
			
			int j = startIdx;
			
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];

				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				
				for (x = 0; x < m_size; x++)
				{

					bftIdx =  x + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (y = 0; y < m_size; y++)
					{

						ii = x + y * m_size;
						xi = X + y * m_size;
						yi = Y + y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
					}
				}
			}
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];

				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];

				for (y = 0; y < m_size; y++)
				{
					bftIdx = y + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (x = 0; x < m_size; x++)
					{

						ii = x + y * m_size;
						xi = x + X * m_size;
						yi = x + Y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
					}
				}
			}
			
			return idx;
		}

		public int PeformFFT(int startIdx, IList<Vector4[]> data0, IList<Vector4[]> data1, IList<Vector4[]> data2)
        {


            int x; int y; int i;
            int idx = 0; int idx1; int bftIdx;
            int X; int Y;
            float wx, wy;
			int ii, xi, yi;

            int j = startIdx;

            for (i = 0; i < m_passes; i++, j++)
            {
                idx = j % 2;
                idx1 = (j + 1) % 2;

				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];
				Vector4[] write2 = data2[idx];

				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				Vector4[] read2 = data2[idx1];

                for (x = 0; x < m_size; x++)
                {

					bftIdx = x + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

                    for (y = 0; y < m_size; y++)
                    {

						ii = x + y * m_size;
						xi = X + y * m_size;
						yi = Y + y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;

						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;

						write2[ii].x = read2[xi].x + wx * read2[yi].x - wy * read2[yi].y;
						write2[ii].y = read2[xi].y + wy * read2[yi].x + wx * read2[yi].y;
						write2[ii].z = read2[xi].z + wx * read2[yi].z - wy * read2[yi].w;
						write2[ii].w = read2[xi].w + wy * read2[yi].z + wx * read2[yi].w;

                    }
                }
            }

            for (i = 0; i < m_passes; i++, j++)
            {
                idx = j % 2;
                idx1 = (j + 1) % 2;

				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];
				Vector4[] write2 = data2[idx];
				
				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				Vector4[] read2 = data2[idx1];

                for (y = 0; y < m_size; y++)
                {

					bftIdx = y + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

                    for (x = 0; x < m_size; x++)
                    {
   
						ii = x + y * m_size;
						xi = x + X * m_size;
						yi = x + Y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
						write2[ii].x = read2[xi].x + wx * read2[yi].x - wy * read2[yi].y;
						write2[ii].y = read2[xi].y + wy * read2[yi].x + wx * read2[yi].y;
						write2[ii].z = read2[xi].z + wx * read2[yi].z - wy * read2[yi].w;
						write2[ii].w = read2[xi].w + wy * read2[yi].z + wx * read2[yi].w;

                    }
                }
            }

            return idx;
        }

		public int PeformFFT(int startIdx, IList<Vector4[]> data0, IList<Vector4[]> data1, IList<Vector4[]> data2, IList<Vector4[]> data3)
		{
			
			
			int x; int y; int i;
			int idx = 0; int idx1; int bftIdx;
			int X; int Y;
			float wx, wy;
			int ii, xi, yi;
			
			int j = startIdx;
			
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];
				Vector4[] write2 = data2[idx];
				Vector4[] write3 = data3[idx];
				
				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				Vector4[] read2 = data2[idx1];
				Vector4[] read3 = data3[idx1];
				
				for (x = 0; x < m_size; x++)
				{

					bftIdx = x + i * m_size;
					
					X = m_butterflyLookupTable[bftIdx].j1;
					Y = m_butterflyLookupTable[bftIdx].j2;
					wx = m_butterflyLookupTable[bftIdx].wr;
					wy = m_butterflyLookupTable[bftIdx].wi;

					for (y = 0; y < m_size; y++)
					{

						ii = x + y * m_size;
						xi = X + y * m_size;
						yi = Y + y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
						write2[ii].x = read2[xi].x + wx * read2[yi].x - wy * read2[yi].y;
						write2[ii].y = read2[xi].y + wy * read2[yi].x + wx * read2[yi].y;
						write2[ii].z = read2[xi].z + wx * read2[yi].z - wy * read2[yi].w;
						write2[ii].w = read2[xi].w + wy * read2[yi].z + wx * read2[yi].w;

						write3[ii].x = read3[xi].x + wx * read3[yi].x - wy * read3[yi].y;
						write3[ii].y = read3[xi].y + wy * read3[yi].x + wx * read3[yi].y;
						write3[ii].z = read3[xi].z + wx * read3[yi].z - wy * read3[yi].w;
						write3[ii].w = read3[xi].w + wy * read3[yi].z + wx * read3[yi].w;
						
					}
				}
			}
			
			for (i = 0; i < m_passes; i++, j++)
			{
				idx = j % 2;
				idx1 = (j + 1) % 2;
				
				Vector4[] write0 = data0[idx];
				Vector4[] write1 = data1[idx];
				Vector4[] write2 = data2[idx];
				Vector4[] write3 = data3[idx];
				
				Vector4[] read0 = data0[idx1];
				Vector4[] read1 = data1[idx1];
				Vector4[] read2 = data2[idx1];
				Vector4[] read3 = data3[idx1];
				
				for (y = 0; y < m_size; y++)
				{
					for (x = 0; x < m_size; x++)
					{
						bftIdx = y + i * m_size;
						
						X = m_butterflyLookupTable[bftIdx].j1;
						Y = m_butterflyLookupTable[bftIdx].j2;
						wx = m_butterflyLookupTable[bftIdx].wr;
						wy = m_butterflyLookupTable[bftIdx].wi;
						
						ii = x + y * m_size;
						xi = x + X * m_size;
						yi = x + Y * m_size;

						write0[ii].x = read0[xi].x + wx * read0[yi].x - wy * read0[yi].y;
						write0[ii].y = read0[xi].y + wy * read0[yi].x + wx * read0[yi].y;
						write0[ii].z = read0[xi].z + wx * read0[yi].z - wy * read0[yi].w;
						write0[ii].w = read0[xi].w + wy * read0[yi].z + wx * read0[yi].w;
						
						write1[ii].x = read1[xi].x + wx * read1[yi].x - wy * read1[yi].y;
						write1[ii].y = read1[xi].y + wy * read1[yi].x + wx * read1[yi].y;
						write1[ii].z = read1[xi].z + wx * read1[yi].z - wy * read1[yi].w;
						write1[ii].w = read1[xi].w + wy * read1[yi].z + wx * read1[yi].w;
						
						write2[ii].x = read2[xi].x + wx * read2[yi].x - wy * read2[yi].y;
						write2[ii].y = read2[xi].y + wy * read2[yi].x + wx * read2[yi].y;
						write2[ii].z = read2[xi].z + wx * read2[yi].z - wy * read2[yi].w;
						write2[ii].w = read2[xi].w + wy * read2[yi].z + wx * read2[yi].w;

						write3[ii].x = read3[xi].x + wx * read3[yi].x - wy * read3[yi].y;
						write3[ii].y = read3[xi].y + wy * read3[yi].x + wx * read3[yi].y;
						write3[ii].z = read3[xi].z + wx * read3[yi].z - wy * read3[yi].w;
						write3[ii].w = read3[xi].w + wy * read3[yi].z + wx * read3[yi].w;
						
					}
				}
			}
			
			return idx;
		}
        */

    }

}


/*
 			int test_sum = 0;
			size = size / 2;

			while (size != 0)
			{
				if (i % 2 == 1)
				{
					test_sum += i * size;
				}
				i /= 2;
				size /= 2;

			}
			bool isSame = test_sum == Sum;
 */












