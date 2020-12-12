using UnityEngine;
using System;
using System.Collections.Generic;


namespace Ceto.Common.Unity.Utility
{

    /// <summary>
    /// Allows a list of functions to be added to a gameobject.
    /// When the object gets rendered each function is called.
    /// Allows for some custom code to run before rendering.
    /// </summary>
    //OYM:  调用一些函数?
    public class NotifyOnWillRender : NotifyOnEvent
	{
	
        /// <summary>
        /// Called when this gameobject gets rendered.
        /// </summary>
		void OnWillRenderObject()
		{
			OnEvent();
            //Debug.Log(gameObject.name + " is being rendered by " + Camera.current.name + " at " + Time.time);
            //OYM:  总之就是每一个camera渲染这个gameobject前都会调用这个方法,并且是不与update同步的
        }


	}
}
