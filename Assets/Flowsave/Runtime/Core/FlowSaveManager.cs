using System;
using System.Collections.Generic;

namespace FlowSave
{
    /// <summary>
    /// Central registry that manages <see cref="FlowSaveContext"/> instances.
    /// </summary>
    public sealed class FlowSaveManager
    {
        private static readonly Lazy<FlowSaveManager> _instance = new Lazy<FlowSaveManager>(() => new FlowSaveManager());
        private readonly Dictionary<string, FlowSaveContext> _contexts = new Dictionary<string, FlowSaveContext>(StringComparer.Ordinal);
        private readonly object _syncRoot = new object();

        private FlowSaveManager()
        {
        }

        /// <summary>
        /// Gets the singleton instance of the manager.
        /// </summary>
        public static FlowSaveManager Instance => _instance.Value;

        /// <summary>
        /// Registers a context with the manager.
        /// </summary>
        /// <param name="context">Context to register.</param>
        /// <param name="overwrite">When true the context replaces any existing registration with the same name.</param>
        public void RegisterContext(FlowSaveContext context, bool overwrite = false)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            lock (_syncRoot)
            {
                if (!overwrite && _contexts.ContainsKey(context.Name))
                {
                    throw new InvalidOperationException($"A context with the name '{context.Name}' is already registered.");
                }

                _contexts[context.Name] = context;
            }
        }

        /// <summary>
        /// Attempts to retrieve a context by name.
        /// </summary>
        public bool TryGetContext(string name, out FlowSaveContext context)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Context name cannot be null or empty.", nameof(name));
            }

            lock (_syncRoot)
            {
                return _contexts.TryGetValue(name, out context);
            }
        }

        /// <summary>
        /// Retrieves a context by name, throwing when it does not exist.
        /// </summary>
        public FlowSaveContext GetContext(string name)
        {
            if (!TryGetContext(name, out FlowSaveContext context))
            {
                throw new KeyNotFoundException($"No context registered with the name '{name}'.");
            }

            return context;
        }

        /// <summary>
        /// Removes a context by name.
        /// </summary>
        /// <param name="name">Name of the context.</param>
        /// <returns>True when the context was removed.</returns>
        public bool RemoveContext(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Context name cannot be null or empty.", nameof(name));
            }

            lock (_syncRoot)
            {
                return _contexts.Remove(name);
            }
        }

        /// <summary>
        /// Clears the context registry.
        /// </summary>
        public void Clear()
        {
            lock (_syncRoot)
            {
                _contexts.Clear();
            }
        }
    }
}
