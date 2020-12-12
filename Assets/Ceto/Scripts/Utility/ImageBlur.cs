using UnityEngine;
using System.Collections;

namespace Ceto
{

	public class ImageBlur 
	{
        //OYM:  模糊方式
        public enum BLUR_MODE { OFF = 0, NO_DOWNSAMPLE = 1, DOWNSAMPLE_2 = 2, DOWNSAMPLE_4 = 4 };

		public BLUR_MODE BlurMode { get; set; }
        //OYM:  最大模糊迭代次数
        /// Blur iterations - larger number means more blur.
        public int BlurIterations { get; set; }
        //OYM: 模糊扩散
        /// Blur spread for each iteration. Lower values
        /// give better looking blur, but require more iterations to
        /// get large blurs. Value is usually between 0.5 and 1.0.
        public float BlurSpread { get; set; }
        //OYM:  偏移量(为啥是四个值)
        Vector2[] m_offsets = new Vector2[4];
        //OYM:  反射材质
        public Material m_blurMaterial;
        //OYM:  构造,这里是创建一个啥玩意
        public ImageBlur(Shader blurShader)
		{

            BlurIterations = 1;//OYM:  迭代一次
            BlurSpread = 0.6f;//OYM:  反射区域=0.6f
            BlurMode = BLUR_MODE.DOWNSAMPLE_2;//OYM:   不知道干啥

            if(blurShader != null)
                m_blurMaterial = new Material(blurShader);//OYM:  材料

        }

        public void Blur(RenderTexture source) //OYM:  计算模糊
        {

            int blurDownSample = (int)BlurMode;//OYM:  采样值

            if (BlurIterations > 0 && m_blurMaterial != null && blurDownSample > 0)//OYM:  迭代次数>0且有材质且采样值大于0
            {
                int rtW = source.width / blurDownSample;//OYM:  采样值越大,这个值越小
                int rtH = source.height / blurDownSample;

                RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0, source.format, RenderTextureReadWrite.Default);//OYM:  获取一份渲染材质的缓冲

                // Copy source to the smaller texture.
                DownSample(source, buffer);
                //OYM:  跟模糊取样有关的迭代,奈何我太菜了
                // Blur the small texture
                for (int i = 0; i < BlurIterations; i++)
				{
					RenderTexture buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0, source.format, RenderTextureReadWrite.Default);
					FourTapCone(buffer, buffer2, i);
					RenderTexture.ReleaseTemporary(buffer);
					buffer = buffer2;
				}
				
				Graphics.Blit(buffer, source);
                RenderTexture.ReleaseTemporary(buffer);//OYM:  释放缓冲
            }

		}

		// Performs one blur iteration.
		void FourTapCone (RenderTexture source, RenderTexture dest, int iteration)
		{
			float off = 0.5f + iteration*BlurSpread;

            m_offsets[0].x = -off;
            m_offsets[0].y = -off;

            m_offsets[1].x = -off;
            m_offsets[1].y = off;

            m_offsets[2].x = off;
            m_offsets[2].y = off;

            m_offsets[3].x = off;
            m_offsets[3].y = -off;

            Graphics.BlitMultiTap(source, dest, m_blurMaterial, m_offsets);
		}

        // Downsamples the texture to a quarter resolution.
        //OYM:  不清楚,跟模糊采样有关?
        void DownSample(RenderTexture source, RenderTexture dest)
		{
			float off = 1.0f;

            m_offsets[0].x = -off;
            m_offsets[0].y = -off;

            m_offsets[1].x = -off;
            m_offsets[1].y = off;

            m_offsets[2].x = off;
            m_offsets[2].y = off;

            m_offsets[3].x = off;
            m_offsets[3].y = -off;

            Graphics.BlitMultiTap (source, dest, m_blurMaterial, m_offsets);
		}

	}

}
