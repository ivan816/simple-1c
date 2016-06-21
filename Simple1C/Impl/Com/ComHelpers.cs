using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Simple1C.Impl.Com
{
    internal static class ComHelpers
    {
        public static object GetProperty(object obj, string name)
        {
            return obj.GetType()
                .InvokeMember(name, BindingFlags.GetProperty, null, obj, new object[0]);
        }

        public static void SetProperty(object obj, string name, object value)
        {
            obj.GetType()
                .InvokeMember(name, BindingFlags.SetProperty, null, obj, new[] {value});
        }

        public static object Invoke(object obj, string method, params object[] parameters)
        {
            return obj.GetType()
                .InvokeMember(method, BindingFlags.InvokeMethod, null, obj, parameters);
        }

        public static string DumpObjectType(object obj)
        {
            var dispId = DISPID_UNKNOWN;
            var disp = obj as IDispatchEx;
            if (disp == null)
                return "IDispatchEx is not implemented";
            var memberNames = new List<string>();

            while (disp.GetNextDispID(fdexEnumAll, dispId, ref dispId) == S_OK)
            {
                string name;
                disp.GetMemberName(dispId, out name);
                memberNames.Add(name);
            }
            return string.Join("\r\n", memberNames);
        }

        private const int S_OK = 0;
        private const int fdexEnumAll = 2;
        private const int DISPID_UNKNOWN = -1;

        [ComImport]
        [Guid("A6EF9860-C720-11D0-9337-00A0C90DCAA9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IDispatchEx
        {
            // IDispatch
            int GetTypeInfoCount();

            [return: MarshalAs(UnmanagedType.Interface)]
            System.Runtime.InteropServices.ComTypes.ITypeInfo GetTypeInfo([In, MarshalAs(UnmanagedType.U4)] int iTInfo,
                [In, MarshalAs(UnmanagedType.U4)] int lcid);

            void GetIDsOfNames([In] ref Guid riid, [In, MarshalAs(UnmanagedType.LPArray)] string[] rgszNames,
                [In, MarshalAs(UnmanagedType.U4)] int cNames, [In, MarshalAs(UnmanagedType.U4)] int lcid,
                [Out, MarshalAs(UnmanagedType.LPArray)] int[] rgDispId);

            void Invoke(int dispIdMember, ref Guid riid, uint lcid, ushort wFlags,
                ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams, out object pVarResult,
                ref System.Runtime.InteropServices.ComTypes.EXCEPINFO pExcepInfo, IntPtr[] pArgErr);

            // IDispatchEx
            void GetDispID([MarshalAs(UnmanagedType.BStr)] string bstrName, uint grfdex, [Out] out int pid);

            void InvokeEx(int id, uint lcid, ushort wFlags,
                [In] ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pdp,
                [In, Out] ref object pvarRes,
                [In, Out] ref System.Runtime.InteropServices.ComTypes.EXCEPINFO pei,
                IServiceProvider pspCaller);

            void DeleteMemberByName([MarshalAs(UnmanagedType.BStr)] string bstrName, uint grfdex);
            void DeleteMemberByDispID(int id);
            void GetMemberProperties(int id, uint grfdexFetch, [Out] out uint pgrfdex);
            int GetMemberName(int id, [Out, MarshalAs(UnmanagedType.BStr)] out string pbstrName);

            [PreserveSig]
            [return: MarshalAs(UnmanagedType.I4)]
            int GetNextDispID(uint grfdex, int id, [In, Out] ref int pid);

            void GetNameSpaceParent([Out, MarshalAs(UnmanagedType.IUnknown)] out object ppunk);
        }
    }
}