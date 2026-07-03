using Autofac;
using Autofac.Extras.CommonServiceLocator;
using CommonServiceLocator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


namespace VCM.BLUEPOS.Infrastructure
{
    public class ServiceLocator : IDisposable
    {
        private static ServiceLocator _serviceLocator;
        private static readonly object Lock = new object();
        private IServiceLocator _container;
        private ServiceLocator()
        {
        }
        private static ServiceLocator Instance
        {
            get
            {
                lock (Lock)
                {
                    if (_serviceLocator == null)
                    {
                        _serviceLocator = new ServiceLocator();
                    }
                    return _serviceLocator;
                }
            }
        }
        public static T GetInstance<T>()
        {
            var instane = Instance._container.GetInstance<T>();
            return instane == null ? default(T) : instane;
        }
        public static object GetInstance(Type type)
        {
            return Instance._container.GetInstance(type);
        }
        public static void Init(IContainer container)
        {
            Instance._container = new AutofacServiceLocator(container);
        }
        public void Dispose()
        {
            _serviceLocator = null;
        }
    }

}