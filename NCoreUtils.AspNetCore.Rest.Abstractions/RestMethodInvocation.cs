using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NCoreUtils.AspNetCore.Rest
{
    public abstract class RestMethodInvocation
    {
        public abstract Type ItemType { get; }

        public abstract object Instance { get; }

        public abstract IReadOnlyList<object> Arguments { get; }

        public abstract Type ReturnType { get; }
    }

    public abstract class ViodRestMethodInvocation : RestMethodInvocation
    {
        public override Type ReturnType => typeof(void);

        public abstract ValueTask InvokeAsync(CancellationToken cancellationToken = default);

        public abstract ViodRestMethodInvocation UpdateArguments(IReadOnlyList<object> arguments);
    }

    public abstract class RestMethodInvocation<T> : RestMethodInvocation
    {
        public override Type ReturnType => typeof(T);

        public abstract ValueTask<T> InvokeAsync(CancellationToken cancellationToken = default);

        public abstract RestMethodInvocation<T> UpdateArguments(IReadOnlyList<object> arguments);
    }
}