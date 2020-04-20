using System;
using NCoreUtils.AspNetCore.Rest.Internal;

namespace NCoreUtils.AspNetCore.Rest
{
    public interface IRestOperationAccessConfigurationBuilder
    {
        IRestOperationAccessConfigurationBuilder Add(AccessValidatorDescriptor factory);
    }

    public interface IRestOperationAccessConfigurationBuilder<TOperation> : IRestOperationAccessConfigurationBuilder
        where TOperation : RestOperation
    { }
}