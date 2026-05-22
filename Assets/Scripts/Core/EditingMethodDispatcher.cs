using System;
using System.Collections.Generic;
using TvmVr2.Api.Enums;
using TvmVr2.Core.Abstractions;
using TvmVr2.Core.Common;

namespace TvmVr2.Core
{
    /// <summary>
    /// Dispatches method inputs to registered handlers.
    /// </summary>
    public sealed class EditingMethodDispatcher
    {
        private readonly Dictionary<MethodKind, IEditingMethodHandler> _handlers;

        /// <summary>
        /// Creates dispatcher from method handlers.
        /// </summary>
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

        /// <summary>
        /// Executes the handler for the selected method.
        /// </summary>
        public IMethodResult Dispatch(MethodKind methodKind, IMethodInput input)
        {
            if (_handlers.TryGetValue(methodKind, out var handler))
                return handler.Execute(input);

            return MethodExecutionResult.Failed($"No handler is registered for method kind '{methodKind}'.");
        }
    }
}
