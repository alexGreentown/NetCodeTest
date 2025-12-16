using System;
using System.Collections.Generic;

namespace NetCodeTest.Core.DI
{
    public static class ServiceContainer
    {
        private static readonly Dictionary<Type, object> _services = new();
    
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
                throw new Exception($"Service {type.Name} already registered");
    
            _services[type] = service;
        }
    
        public static T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;
    
            throw new Exception($"Service {typeof(T).Name} not registered");
        }
    
        public static void Clear()
        {
            _services.Clear();
        }
    }
}
