using UnityEngine;
using System;
using System.Collections.Generic;

using Ceto.Common.Threading.Scheduling;
using Ceto.Common.Containers.Interpolation;

namespace Ceto
{

	public class DisplacementBufferCPU : WaveSpectrumBufferCPU, IDisplacementBuffer
	{
		
		const int NUM_BUFFERS = 3;

		IList<InterpolatedArray2f[]> m_displacements;

 		public DisplacementBufferCPU(int size, Scheduler scheduler) : base(size, NUM_BUFFERS, scheduler)
		{

            int GRIDS = QueryDisplacements.GRIDS;//OYM:  4
            int CHANNELS = QueryDisplacements.CHANNELS;//OYM:  3

            m_displacements = new List<InterpolatedArray2f[]>(2); //OYM:  差值数组,注释上说,可以用浮点数对数组进行采样,将使用bilinear过滤执行 

            m_displacements.Add(new InterpolatedArray2f[GRIDS]);//OYM:  注意这里是添加一个新建的数组,容量为4
            m_displacements.Add( new InterpolatedArray2f[GRIDS] );

			for (int i = 0; i < GRIDS; i++)
			{
				//OYM:  2*4一共八个
				m_displacements[0][i] = new InterpolatedArray2f(size, size, CHANNELS, true); 
                m_displacements[1][i] = new InterpolatedArray2f(size, size, CHANNELS, true); 
            }
		}

		protected override void Initilize(WaveSpectrumCondition condition, float time)
		{

			InterpolatedArray2f[] displacements = GetWriteDisplacements();

			displacements[0].Clear();
			displacements[1].Clear();
			displacements[2].Clear();
			displacements[3].Clear();

            if (m_initTask == null)
            {
                m_initTask = condition.GetInitSpectrumDisplacementsTask(this, time);
            }
            else if(m_initTask.SpectrumType != condition.Key.SpectrumType || m_initTask.NumGrids != condition.Key.NumGrids)
            {
                m_initTask = condition.GetInitSpectrumDisplacementsTask(this, time);
            }
            else
            {
                m_initTask.Reset(condition, time);
            }
			
		}

        public InterpolatedArray2f[] GetWriteDisplacements()
		{
			return m_displacements[WRITE];
		}

		public InterpolatedArray2f[] GetReadDisplacements()
		{
			return m_displacements[READ];
		}

		public override void Run(WaveSpectrumCondition condition, float time)
		{
			SwapDisplacements();
			base.Run(condition, time);
		}

		public void CopyAndCreateDisplacements(out IList<InterpolatedArray2f> displacements)
		{
            //Debug.Log("Copy and create");

			InterpolatedArray2f[] source = GetReadDisplacements();
			QueryDisplacements.CopyAndCreateDisplacements(source, out displacements);

        }

		public void CopyDisplacements(IList<InterpolatedArray2f> displacements)
		{
			InterpolatedArray2f[] source = GetReadDisplacements();
			QueryDisplacements.CopyDisplacements(source, displacements);
		}

		void SwapDisplacements()
		{

			InterpolatedArray2f[] tmp = m_displacements[0];
			m_displacements[0] = m_displacements[1];
			m_displacements[1] = tmp;

		}

        public Vector4 MaxRange(Vector4 choppyness, Vector2 gridScale)
		{

			InterpolatedArray2f[] displacements = GetReadDisplacements();

			return QueryDisplacements.MaxRange(displacements, choppyness, gridScale, null);

		}

		public void QueryWaves(WaveQuery query, QueryGridScaling scaling)
		{

			int enabled = EnabledBuffers();

			//If no buffers are enabled there is nothing to sample.
			if(enabled == 0) return;

			InterpolatedArray2f[] displacements = GetReadDisplacements();
			
			QueryDisplacements.QueryWaves(query, enabled, displacements, scaling);
			
		}

	}

}











