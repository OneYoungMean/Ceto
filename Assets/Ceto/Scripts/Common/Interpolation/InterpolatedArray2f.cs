using System;

namespace Ceto.Common.Containers.Interpolation
{

    /// <summary>
	/// A Interpolated 2 dimensional array.
    /// The array can be sampled using a float and the sampling
    /// will be performed using bilinear filtering.
    /// </summary>
	public class InterpolatedArray2f : InterpolatedArray
    {
        //OYM:  全部数据
        /// <summary>
        /// Gets the data.
        /// </summary>
        public float[] Data { get { return m_data; } }
		float[] m_data;
        //OYM:  是x点的维度吗
        /// <summary>
        /// Size on the x dimension.
        /// </summary>
        public int SX { get { return m_sizeX; } }
		int m_sizeX;
        //OYM:  y点的维度
        /// <summary>
        /// Size on the y dimension.
        /// </summary>
        public int SY { get { return m_sizeY; } }
		int m_sizeY;
        //OYM:  频道数量
        /// <summary>
        /// Number of channels.
        /// </summary>
        public int Channels { get { return m_channels; } }
		int m_channels;

        public InterpolatedArray2f(int sx, int sy, int c, bool wrap) : base(wrap) //OYM:  
        {
			m_sizeX = sx;
			m_sizeY = sy;
			m_channels = c;

            m_data = new float[m_sizeX * m_sizeY * m_channels]; //OYM:  一个超级大的采样数组
        }

		public InterpolatedArray2f(float[] data, int sx, int sy, int c, bool wrap) : base(wrap)
		{
			m_sizeX = sx;
			m_sizeY = sy;
			m_channels = c;

			m_data = new float[m_sizeX * m_sizeY * m_channels];

            Copy(data);
		}

		public InterpolatedArray2f(float[,,] data, bool wrap) : base(wrap)
        {
			m_sizeX = data.GetLength(0);
			m_sizeY = data.GetLength(1);
			m_channels = data.GetLength(2);

			m_data = new float[m_sizeX * m_sizeY * m_channels];

            Copy(data);
        }

		/// <summary>
		/// Clear the data in array to 0.
		/// </summary>
		public void Clear()
		{
			Array.Clear(m_data, 0, m_data.Length);
		}

		/// <summary>
		/// Copy the specified data.
		/// </summary>
		public void Copy(Array data)
		{
			Array.Copy(data, m_data, m_data.Length);
		}

        /// <summary>
        /// Get a value from the data array using normal indexing.
        /// </summary>
		public float this[int x, int y, int c]
        {
            get{
				return m_data[(x + y * m_sizeX) * m_channels + c];
            }
            set{
				m_data[(x + y * m_sizeX) * m_channels + c] = value;
            }
        }

        /// <summary>
        /// Get a channel from array.
        /// </summary>
        public float Get(int x, int y, int c)
        {
            return m_data[(x + y * m_sizeX) * m_channels + c];
        }

        /// <summary>
        /// Set a channel from array.
        /// </summary>
        public void Set(int x, int y, int c, float v)
        {
            m_data[(x + y * m_sizeX) * m_channels + c] = v;
        }

		/// <summary>
		/// Set all channels from array
		/// </summary>
		public void Set(int x, int y, float[] v)
		{
			for(int c = 0; c < m_channels; c++)
				m_data[(x + y * m_sizeX) * m_channels + c] = v[c];
		}
		
		/// <summary>
		/// Get all channels into array
		/// </summary>
		public void Get(int x, int y, float[] v)
		{
			for(int c = 0; c < m_channels; c++)
				v[c] = m_data[(x + y * m_sizeX) * m_channels + c];
		}

        /// <summary>
        /// Get a value from the data array using bilinear filtering.
        /// </summary>
		public void Get(float x, float y, float[] v)
        {

            //un-normalize cords
            if (HalfPixelOffset)
            {
                x *= (float)m_sizeX;
                y *= (float)m_sizeY;

                x -= 0.5f;
                y -= 0.5f;
            }
            else
            {
                x *= (float)(m_sizeX - 1);
                y *= (float)(m_sizeY - 1);
            }

			int x0, x1;
			float fx = Math.Abs(x - (int)x);
			Index(x, m_sizeX, out x0, out x1);
			
			int y0, y1;
			float fy = Math.Abs(y - (int)y);
			Index(y, m_sizeY, out y0, out y1);

			for(int c = 0; c < m_channels; c++)
			{
				float v0 = m_data[(x0 + y0 * m_sizeX) * m_channels + c] * (1.0f-fx) + m_data[(x1 + y0 * m_sizeX) * m_channels + c] * fx;
				float v1 = m_data[(x0 + y1 * m_sizeX) * m_channels + c] * (1.0f-fx) + m_data[(x1 + y1 * m_sizeX) * m_channels + c] * fx;

            	v[c] = v0 * (1.0f-fy) + v1 * fy;
			}
        }

        /// <summary>
        /// Get a value from the data array using bilinear filtering.
        /// </summary>
        public float Get(float x, float y, int c)
        {

            //un-normalize cords
            if (HalfPixelOffset)
            {
                x *= (float)m_sizeX;
                y *= (float)m_sizeY;

                x -= 0.5f;
                y -= 0.5f;
            }
            else
            {
                x *= (float)(m_sizeX - 1);
                y *= (float)(m_sizeY - 1);
            }

            int x0, x1;
            float fx = Math.Abs(x - (int)x);
            Index(x, m_sizeX, out x0, out x1);

            int y0, y1;
            float fy = Math.Abs(y - (int)y);
            Index(y, m_sizeY, out y0, out y1);

            float v0 = m_data[(x0 + y0 * m_sizeX) * m_channels + c] * (1.0f - fx) + m_data[(x1 + y0 * m_sizeX) * m_channels + c] * fx;
            float v1 = m_data[(x0 + y1 * m_sizeX) * m_channels + c] * (1.0f - fx) + m_data[(x1 + y1 * m_sizeX) * m_channels + c] * fx;

            return v0 * (1.0f - fy) + v1 * fy;
            
        }

    }


}














