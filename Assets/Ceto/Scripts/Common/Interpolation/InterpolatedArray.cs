using System;

namespace Ceto.Common.Containers.Interpolation
{
	/// <summary>
	/// Abstract class providing common functions for Interpolated array.
	/// A filtered array allows the bilinear filtering of its contained data.
	/// </summary>
	//OYM:  为插值数组提供通用功能的抽象类。过滤后的数组允许对其包含的数据进行双线性过滤。

	public abstract class InterpolatedArray
    {
		/// <summary>
		/// Should the sampling of the array be wrapped or clamped.
		/// </summary>
		//OYM:  阵列的采样是否应该缠绕或夹紧。
		public bool Wrap { get { return m_wrap; } set { m_wrap = value; } }
        bool m_wrap;

        /// <summary>
        /// Should the interpolation be done with a 
        /// half pixel offset.
        /// </summary>
        public bool HalfPixelOffset { get; set; }

        public InterpolatedArray(bool wrap) //OYM:  默认是true.你就当这玩意是true就行
        {
            m_wrap = wrap;
            HalfPixelOffset = true; //OYM:  
        }

		/// <summary>
		/// Get the index that needs to be sampled for point filtering.
		/// </summary>
		public void Index(ref int x, int sx)
		{

			if(m_wrap)
			{
				if(x >= sx || x <= -sx) x = x % sx;
				if(x < 0) x = sx - -x;
			}
			else
			{
				if(x < 0) x = 0;
				else if(x >= sx) x = sx-1;
			}
			
		}

		/// <summary>
		/// Get the two indices that need to be sampled for bilinear filtering.
		/// </summary>
		public void Index(double x, int sx, out int ix0, out int ix1)
		{
			
			ix0 = (int)x;
			ix1 = (int)x + (int)Math.Sign(x);
			
			if(m_wrap)
			{
				if(ix0 >= sx || ix0 <= -sx) ix0 = ix0 % sx;
				if(ix0 < 0) ix0 = sx - -ix0;
				
				if(ix1 >= sx || ix1 <= -sx) ix1 = ix1 % sx;
				if(ix1 < 0) ix1 = sx - -ix1;
			}
			else
			{
				if(ix0 < 0) ix0 = 0;
				else if(ix0 >= sx) ix0 = sx-1;

				if(ix1 < 0) ix1 = 0;
				else if(ix1 >= sx) ix1 = sx-1;
			}
			
		}

    }


}





