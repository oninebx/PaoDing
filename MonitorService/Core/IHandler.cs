using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MonitorService.Core
{
    public interface IHandler<R, M> where M : IMessage
    {
        Task<R> Handle(M message);
    }
}