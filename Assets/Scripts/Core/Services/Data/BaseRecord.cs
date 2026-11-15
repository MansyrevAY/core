using System;

namespace Core.Services.Data
{
    [Serializable]
    public abstract class BaseRecord
    {
        public abstract string Name { get; }
    }
}