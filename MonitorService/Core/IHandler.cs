using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Core
{
    public interface IHandler<T> where T : IMessage
    {
        Task Handle(T message);
    }
}