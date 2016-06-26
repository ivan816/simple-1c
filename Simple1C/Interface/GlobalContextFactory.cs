using System;
using Simple1C.Impl.Com;

namespace Simple1C.Interface
{
    public class GlobalContextFactory : IDisposable
    {
        private readonly object lockObject = new object();
        private bool disposed;
        private COMConnector comConnector;

        public object Create(string connectionString)
        {
            lock (lockObject)
            {
                if (disposed)
                    throw new ObjectDisposedException("GlobalContextFactory");
                if (comConnector == null)
                    comConnector = new COMConnector();
                return comConnector.Connect(connectionString);
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

            public new void Dispose()
            {
                base.Dispose();
            }
        }
    }
}