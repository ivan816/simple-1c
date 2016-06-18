using System;
using System.Runtime.InteropServices;
using LinqTo1C.Impl;
using LinqTo1C.Impl.Com;

namespace LinqTo1C
{
    public class GlobalContextFactory : IDisposable
    {
        private readonly object lockObject = new object();
        private bool disposed;
        private COMConnector comConnector;

        public GlobalContext Create(string connectionString)
        {
            lock (lockObject)
            {
                if (disposed)
                    throw new ObjectDisposedException("GlobalContextFactory");
                if (comConnector == null)
                    comConnector = new COMConnector();
                var globalContextComObject = comConnector.Connect(connectionString);
                return new GlobalContext(globalContextComObject);
            }
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                if (disposed)
                    return;
                disposed = true;
                if (comConnector != null)
                {
                    comConnector.Dispose();
                    comConnector = null;
                }
            }
        }

        private class COMConnector : DispatchObject, IDisposable
        {
            public COMConnector()
                : base(CreateComObject())
            {
            }

            public object Connect(string connectionString)
            {
                return Invoke("Connect", connectionString);
            }

            private static object CreateComObject()
            {
                var connectorType = Type.GetTypeFromProgID("V83.COMConnector");
                return Activator.CreateInstance(connectorType);
            }

            public void Dispose()
            {
                Marshal.FinalReleaseComObject(ComObject);
            }
        }
    }
}