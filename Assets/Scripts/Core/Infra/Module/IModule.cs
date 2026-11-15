using System;

namespace Core.Infra.Module
{
    public interface IModule : IDisposable
    {
        void Start();
    }
}
