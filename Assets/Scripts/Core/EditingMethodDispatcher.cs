using System;
using System.Collections.Generic;
using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;

namespace TvmVr2.Core
{
    public sealed class EditingMethodDispatcher
    {
        private readonly Dictionary<MethodKind, IEditingMethodHandler> _handlers;

        public EditingMethodDispatcher(IEnumerable<IEditingMethodHandler> handlers)
        {
            _handlers = new Dictionary<MethodKind, IEditingMethodHandler>();

            foreach (var handler in handlers)
            {
                if (handler == null)
                    continue;

                _handlers[handler.SupportedMethod] = handler;
            }
        }

        public IMethodResult Dispatch(MethodKind methodKind, IMethodInput input)
        {
            if (_handlers.TryGetValue(methodKind, out var handler))
                return handler.Execute(input);

            return MethodExecutionResult.NotImplemented($"No handler is registered for method kind '{methodKind}'.");
        }
    }
}
