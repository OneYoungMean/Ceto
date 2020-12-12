using UnityEngine;
using System;
using System.Collections.Generic;


namespace Ceto.Common.Unity.Utility
{
	
	/// <summary>
	/// Allows a list of functions to be added to a gameobject.
	/// When some event occurs each function is called.
	/// Allows for some custom code to run before event.
	/// </summary>
	public abstract class NotifyOnEvent : MonoBehaviour 
	{
        //OYM:  注释说这是一个防止递归的通知系统

        //OYM:  虽然不知道这个系统谁在用,但是看上去很牛逼的样子
        /// <summary>
        /// Globally disable/enable the notification.
        /// Used to prevent a recursive notifications
        /// from happening.
        /// </summary>
        public static bool Disable;
        //OYM:  关闭通知
        /// <summary>
        /// 
        /// </summary>
        interface INotify
		{
            //OYM:  空接口
        }
		
		/// <summary>
		/// Notification with a action.
		/// </summary>
		class Notify : INotify
		{
			public Action<GameObject> action;
            //OYM:  一个需要传入gameobject才能实现的action
        }
		
		/// <summary>
		/// Notification with a action and argument.
		/// </summary>
		class NotifyWithArg : INotify
		{
			public Action<GameObject, object> action;
			public object arg;
            //OYM:  带参数的action
        }

        /// <summary>
        /// The list of functions that will be called.
        /// </summary>
        IList<INotify> m_actions = new List<INotify>();//OYM:  这个list里面的action都会被调用...蛤?这不是一个空接口吗

        /// <summary>
        /// Call to execute actions.
        /// </summary>
        protected void OnEvent()
		{
			if(Disable) return;
			
			int count = m_actions.Count;
			for(int i = 0; i < count; i++)
			{
				INotify  notify = m_actions[i];

                if (notify is Notify)//OYM:  还能这样?
                {
					Notify n = notify as Notify;
					n.action(gameObject);
				}
                else if (notify is NotifyWithArg)//OYM:  这是什么骚操作
                {
					NotifyWithArg n = notify as NotifyWithArg;
					n.action(gameObject, n.arg);
				}
			}
		}
		
		/// <summary>
		/// Add a action with a argument.
		/// </summary>
		public void AddAction(Action<GameObject, object> action, object arg)
		{
			NotifyWithArg notify = new NotifyWithArg(); 
			notify.action = action;
			notify.arg = arg;
			
			m_actions.Add(notify);
            //OYM:  添加action
        }
		
		/// <summary>
		/// Add a action with no argument.
		/// </summary>
		public void AddAction(Action<GameObject> action)
		{
			Notify notify = new Notify();
			notify.action = action;
			
			m_actions.Add(notify);
		}
        //OYM:  添加Action2

    }
}
