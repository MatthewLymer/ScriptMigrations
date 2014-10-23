using System;

namespace MigratorConsole
{
    public enum ActivatorResultCode
    {
        Successful,
        TypeNotFound,
        AssemblyNotFound
    }

    public class ActivatorResult<T> where T : class
    {
        private readonly T _instance;

        public ActivatorResult(ActivatorResultCode resultCode)
        {
            ResultCode = resultCode;
        }

        public ActivatorResult(T instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }

            _instance = instance;
            ResultCode = ActivatorResultCode.Successful;
        }

        public bool HasInstance { get { return _instance != null; }}

        public T Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException("Instance was not created");
                }

                return _instance;
            }
        }

        public ActivatorResultCode ResultCode { get; private set; }
    }
}
