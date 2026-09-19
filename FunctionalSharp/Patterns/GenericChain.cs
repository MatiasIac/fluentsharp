using FunctionalSharp.Decorators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace FunctionalSharp.Patterns
{

    /// <summary>Defines a reusable step in a chain of responsibility.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    public abstract class LinkBase<T>
    {
        /// <summary>Executes the link against the shared payload and cancellation flag.</summary>
        /// <param name="data">The shared chain state.</param>
        public abstract void OnExecute(DataCargo<T> data);
    }

    /// <summary>Holds the mutable state passed between links in a chain.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    public sealed class DataCargo<T>
    {
        /// <summary>The payload shared by all links.</summary>
        public T Payload = default!;
        /// <summary>Gets or sets whether the chain should stop after the current link.</summary>
        public bool Cancel { get; set; }
    }

    /// <summary>Configures how a chain handles failures and repeated attempts.</summary>
    public sealed class Configuration
    {
        /// <summary>Gets whether an exhausted link failure stops the chain instead of continuing.</summary>
        public bool StopOnFailure { get; }
        /// <summary>Gets the maximum attempts per link, including the initial attempt.</summary>
        /// <remarks>One means no retries. Attempts are allowed regardless of StopOnFailure.</remarks>
        public int MaxAttempts { get; }

        /// <summary>Creates a chain's failure-handling configuration.</summary>
        /// <param name="stopOnFailure">Whether to stop after a link exhausts its allowed attempts.</param>
        /// <param name="maxAttempts">The total attempt limit per link, including the initial attempt. Must be at least one.</param>
        /// <exception cref="ArgumentOutOfRangeException">maxAttempts is less than one.</exception>
        public Configuration(
            bool stopOnFailure = true,
            int maxAttempts = 1
        )
        {
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "At least one attempt is required.");

            StopOnFailure = stopOnFailure;
            MaxAttempts = maxAttempts;
        }
    }

    /// <summary>Executes an ordered sequence of links against a shared payload.</summary>
    /// <typeparam name="T">The payload type.</typeparam>
    public sealed class GenericChain<T>
    {
        private sealed class Link : LinkBase<T>
        {
            private readonly Action<DataCargo<T>> _action;

            public Link(Action<DataCargo<T>> action)
            {
                _action = action;
            }

            public override void OnExecute(DataCargo<T> data) => _action.Invoke(data);
        }

        private readonly DataCargo<T> _dataCargo;
        private readonly Configuration _configuration;
        private readonly List<LinkBase<T>> _chain;
        private Action<T>? _completeAction;
        private Action<T, Exception>? _errorAction;
        private Dictionary<string, Type>? _decoratedLinkDictionary;

        internal GenericChain(T? payload, Configuration? configuration)
        {
            _dataCargo = new DataCargo<T>
            {
                Payload = GetPayloadOrInstance(payload)
            };

            _configuration = configuration ?? new Configuration();

            _chain = new List<LinkBase<T>>();
        }

        /// <summary>Creates a chain with a default or newly constructed payload and default configuration.</summary>
        public static GenericChain<T> Create() => new GenericChain<T>(default, null);

        /// <summary>Creates a chain with a default or newly constructed payload and supplied configuration.</summary>
        public static GenericChain<T> Create(Configuration configuration) => new GenericChain<T>(default, configuration);

        /// <summary>Creates a chain with a supplied payload and optional configuration.</summary>
        /// <remarks>A null reference payload is constructed if its type has a public parameterless constructor.</remarks>
        public static GenericChain<T> Create(T? payload, Configuration? configuration = null) => new GenericChain<T>(payload, configuration);

        /// <summary>Appends an action to the chain and returns this chain.</summary>
        public GenericChain<T> AddLink(Action<DataCargo<T>> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            return AddLink(new Link(action));
        }

        /// <summary>Appends a custom link to the chain and returns this chain.</summary>
        public GenericChain<T> AddLink(LinkBase<T> link)
        {
            ArgumentNullException.ThrowIfNull(link);

            _chain.Add(link);

            return this;
        }

        /// <summary>Finds a named LinkAttribute-decorated link in loaded assemblies and appends it.</summary>
        /// <remarks>
        /// Discovery includes only concrete, closed LinkBase&lt;T&gt; subclasses with a public
        /// parameterless constructor. Names must be unique among eligible links for this payload type.
        /// </remarks>
        [RequiresUnreferencedCode("Decorated discovery scans loaded assemblies. Register a link explicitly with AddLink instead.")]
        public GenericChain<T> AddDecoratedLink(string linkName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(linkName);
            return AddLink(GetLinkByDecorationName(linkName));
        }

        #region Events
        /// <summary>Executes the links in order using the current payload.</summary>
        /// <remarks>
        /// Each run resets the cancellation flag and uses the current payload. Cancellation or an
        /// exhausted stopping failure suppresses the completion callback. OperationCanceledException
        /// propagates immediately without retries or error callbacks. Other link exceptions are
        /// reported to OnError and handled by the configuration. Callback exceptions propagate.
        /// </remarks>
        public void Run()
        {
            _dataCargo.Cancel = false;

            foreach (var link in _chain)
            {
                if (RunLinkAndStop(link)) return;
            }

            _completeAction?.Invoke(_dataCargo.Payload);
        }

        /// <summary>
        /// Registers a callback called once when a run reaches the end of the chain.
        /// </summary>
        /// <remarks>Continued link failures permit completion. Cancellation and stopping failures suppress it.</remarks>
        /// <param name="action">The completion callback. Exceptions propagate to the caller of Run.</param>
        /// <returns>This chain.</returns>
        public GenericChain<T> OnCompleted(Action<T> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            _completeAction = action;
            return this;
        }

        /// <summary>
        /// Registers a callback called for each failed link attempt, except cancellation exceptions.
        /// </summary>
        /// <param name="action">The error callback. Exceptions propagate without being retried or reported again.</param>
        /// <returns>This chain.</returns>
        public GenericChain<T> OnError(Action<T, Exception> action)
        {
            ArgumentNullException.ThrowIfNull(action);
            _errorAction = action;
            return this;
        }
        #endregion

        #region Privates
        [RequiresUnreferencedCode("Scans loaded assemblies for decorated links.")]
        private LinkBase<T> GetLinkByDecorationName(string name)
        {
            if (_decoratedLinkDictionary is null) CreateDecoratedLinkDictionary();
            if (!_decoratedLinkDictionary!.TryGetValue(name, out var type))
                throw new KeyNotFoundException($"Decorated link {name} not found");
            return (LinkBase<T>)Activator.CreateInstance(type)!;
        }

        [RequiresUnreferencedCode("Scans loaded assemblies for decorated links.")]
        private void CreateDecoratedLinkDictionary()
        {
            var candidates = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(type => typeof(LinkBase<T>).IsAssignableFrom(type)
                    && !type.IsAbstract
                    && !type.ContainsGenericParameters
                    && type.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => new
                {
                    Type = type,
                    Decoration = type.GetCustomAttribute<LinkAttribute>(true)
                })
                .Where(candidate => candidate.Decoration != null)
                .ToArray();

            var duplicate = candidates.GroupBy(candidate => candidate.Decoration!.LinkName, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                throw new InvalidOperationException($"Multiple links for '{typeof(T)}' use the name '{duplicate.Key}'. Register links explicitly with AddLink.");

            // Validate all names before constructing anything; instantiate only the requested link.
            _decoratedLinkDictionary = candidates.ToDictionary(candidate => candidate.Decoration!.LinkName, candidate => candidate.Type, StringComparer.Ordinal);
        }

        [RequiresUnreferencedCode("Enumerates assembly types.")]
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException error) { return error.Types.OfType<Type>(); }
        }

        private bool RunLinkAndStop(LinkBase<T> link)
        {
            for (var attempt = 0; attempt < _configuration.MaxAttempts; attempt++)
            {
                try
                {
                    link.OnExecute(_dataCargo);
                    return _dataCargo.Cancel;
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    _errorAction?.Invoke(_dataCargo.Payload, ex);

                    // A link can request cancellation before failing; cancellation takes
                    // precedence over retrying or continuing to the next link.
                    if (_dataCargo.Cancel) return true;
                }
            }

            return _configuration.StopOnFailure;
        }

        private T GetPayloadOrInstance(T? payload)
        {
            if (payload != null) return payload;

            var payloadType = typeof(T);

            if (payloadType.GetConstructor(Type.EmptyTypes) != null && !payloadType.IsAbstract)
            {
                return Activator.CreateInstance<T>();
            }

            throw new ArgumentException("Type must be creatable");
        }
        #endregion
    }
}
