﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using lrpc.val;

namespace lrpc.fun
{
    /// <summary>
    /// register call method
    /// </summary>
    public class Fun
    {
        private class ObjectMethod
        {
            public object obj;
            public MethodInfo fun;
        }

        private Dictionary<string, ObjectMethod> funs = new Dictionary<string, ObjectMethod>();

        /// <summary>
        /// register the method being called
        /// </summary>
        /// <param name="name">the name to use when calling</param>
        /// <param name="obj">method object</param>
        /// <param name="fun">the actual method used</param>
        public void Regist(string name, object obj, MethodInfo fun)
        {
            ObjectMethod om = new ObjectMethod();
            om.obj = obj;
            om.fun = fun;
            funs.Add(name, om);
        }

        /// <summary>
        /// register and call a method with the same name
        /// </summary>
        /// <param name="name">register and call names</param>
        /// <param name="obj">method object</param>
        public void Regist(string name, object obj)
        {
            Regist(name, obj, obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
        }

        private ByteQue Except(string msg)
        {
            ByteQue que = new ByteQue();
            que.Push(msg);
            return que;
        }

        /// <summary>
        /// call the registered method
        /// </summary>
        /// <param name="que">the queue generated by fun</param>
        /// <returns>serialize error messages or call results into a queue</returns>
        public ByteQue Invoke(ByteQue que)
        {
            byte[] arr = new byte[que.PopSize()];
            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = que.Pop<byte>();
            }
            string name = Encoding.UTF8.GetString(arr);
            if (!funs.ContainsKey(name))
            {
                return Except(name + " function not found");
            }
            ObjectMethod om = funs[name];
            ParameterInfo[] types = om.fun.GetParameters();
            object[] args = new object[types.Length];
            for (int i = 0; i < types.Length; ++i)
            {
                Type type = types[i].ParameterType;
                if (que.Len == 0)
                {
                    return Except("error when calling function " + name + " to restore parameters to the " + i + "th parameter " + type.ToString());
                }
                try
                {
                    args[i] = que.Pop(type);
                }
                catch (Exception e)
                {
                    return Except("error when calling function " + name + " to restore parameters to the " + i + "th parameter " + type.ToString() + ": " + e.ToString());
                }
            }
            if (que.Len != 0)
            {
                return Except("error when calling function " + name + " to restore parameters");
            }
            ByteQue ret = new ByteQue();
            object rst;
            try
            {
                rst = om.fun.Invoke(om.obj, args);
            }
            catch (Exception e)
            {
                return Except("error calling function " + name + " " + e.ToString());
            }
            ret.Push(false);
            Type rtp = om.fun.ReturnType;
            if (rtp != typeof(void))
            {
                try
                {
                    ret.Push(rtp, rst);
                }
                catch (Exception e)
                {
                    return Except("error calling function " + name + " to store result " + e.ToString());
                }
            }
            return ret;
        }

        /// <summary>
        /// call the method serialized into a queue
        /// </summary>
        /// <param name="name">the name of the calling method</param>
        /// <param name="args">calling method parameters</param>
        /// <returns>the result of calling the method</returns>
        public static ByteQue Make(string name, params object[] args)
        {
            ByteQue que = new ByteQue();
            byte[] arr = Encoding.UTF8.GetBytes((string)name);
            que.PushSize(arr.Length);
            foreach (byte ch in arr)
            {
                que.Push(ch);
            }
            foreach (object arg in args)
            {
                que.Push(arg.GetType(), arg);
            }
            return que;
        }
    }
}
