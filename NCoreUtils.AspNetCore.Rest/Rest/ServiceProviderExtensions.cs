using System;
using System.Runtime.CompilerServices;

namespace NCoreUtils.AspNetCore.Rest
{
    static class ServiceProviderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T? GetOptionalService<T>(this IServiceProvider serviceProvider)
            where T : class
            => (T?)serviceProvider.GetService(typeof(T));
    }
}