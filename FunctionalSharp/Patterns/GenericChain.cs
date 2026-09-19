using FunctionalSharp.Decorators;
using FunctionalSharp.Validators;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public T Payload;
        /// <summary>Gets or sets whether the chain should stop after the current link.</summary>
        public bool Cancel { get; set; }
    }

    /// <summary>Configures how a chain handles failures and repeated attempts.</summary>
    public sealed class Configuration
    {
        /// <summary>Gets whether a link failure stops the chain without retrying.</summary>
        public bool StopOnFailure { get; }
        /// <summary>Gets the total attempt limit when retries are enabled.</summary>
        /// <remarks>Retries require StopOnFailure to be false. Zero stops the chain on a failure.</remarks>
        public int RepeatTimesOnFailure { get; }

        /// <summary>Creates a chain's failure-handling configuration.</summary>
        /// <param name="stopOnFailure">Whether to stop immediately when a link fails.</param>
        /// <param name="repeatTimesOnFailure">The total attempt limit when not stopping on failure.</param>
        public Configuration(
            bool stopOnFailure = true,
            int repeatTimesOnFailure = 0
        )
        {
            StopOnFailure = stopOnFailure;
            RepeatTimesOnFailure = repeatTimesOnFailure;
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
        private Action<T> _completeAction;
        private Action<T, Exception> _errorAction;
        private Dictionary<string, LinkBase<T>> _decoratedLinkDictionary;

        internal GenericChain(T payload, Configuration configuration)
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
        public static GenericChain<T> Create(T payload, Configuration configuration = null) => new GenericChain<T>(payload, configuration);

        /// <summary>Appends an action to the chain and returns this chain.</summary>
        public GenericChain<T> AddLink(Action<DataCargo<T>> action) => AddLink(new Link(action));

        /// <summary>Appends a custom link to the chain and returns this chain.</summary>
        public GenericChain<T> AddLink(LinkBase<T> link)
        {
            link.IfNull().Throw(new Exception("Chain Link cannot be null"));

            _chain.Add(link);

            return this;
        }

        /// <summary>Finds a named LinkAttribute-decorated link in loaded assemblies and appends it.</summary>
        public GenericChain<T> AddDecoratedLink(string linkName) => AddLink(GetLinkByDecorationName(linkName));

        #region Events
        /// <summary>Executes the links in order using the current payload.</summary>
        /// <remarks>Cancellation or a stopping failure suppresses the completion callback.</remarks>
        public void Run()
        {
            bool failed = false;

            foreach (var link in _chain)
            {
                if (failed = RunLinkAndStop(link)) break;
            }

            if (!failed) _completeAction?.Invoke(_dataCargo.Payload);
        }

        /// <summary>
        /// After the chain is fully executed, OnCompleted is called
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public GenericChain<T> OnCompleted(Action<T> action)
        {
            _completeAction = action;
            return this;
        }

        /// <summary>
        /// When any link of the chain throws an exception OnError is called
        /// </summary>
        /// <param name="action"></param>
        /// <returns>This chain</returns>
        public GenericChain<T> OnError(Action<T, Exception> action)
        {
            _errorAction = action;
            return this;
        }
        #endregion

        #region Privates
        private LinkBase<T> GetLinkByDecorationName(string name)
        {
            _decoratedLinkDictionary.IfNull()
                .Then(() => CreateDecoratedLinkDictionary());

            (_decoratedLinkDictionary.TryGetValue(name, out LinkBase<T> link))
                .IfFalse()
                .Throw(new Exception($"Decorated link {name} not found"));

            return link;
        }

        private void CreateDecoratedLinkDictionary()
        {
            //TODO: potential bug or innecessary iteration
            // if LinkBase<T>, concrete type differs from its T type
            // will be included into the dictionary as null
            // find a way to filter out null values from the main where
            _decoratedLinkDictionary = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                    assembly.GetTypes()
                        .Where(type => 
                            type.GetCustomAttributes(typeof(LinkAttribute), true).Count() > 0 
                            //&& type.IsAssignableFrom(typeof(LinkBase<T>))
                            ))
                .ToDictionary(k => 
                    (k.GetCustomAttributes(typeof(LinkAttribute), true)[0] as LinkAttribute).LinkName, 
                    v => Activator.CreateInstance(v) as LinkBase<T>);
        }

        private bool RunLinkAndStop(LinkBase<T> link, int attempt = 0)
        {
            try
            {
                _dataCargo.Cancel = false;
                link.OnExecute(_dataCargo);
                return _dataCargo.Cancel;
            }
            catch (Exception ex)
            {
                _errorAction?.Invoke(_dataCargo.Payload, ex);

                if (!_configuration.StopOnFailure && 
                    _configuration.RepeatTimesOnFailure > 0 &&
                    attempt < _configuration.RepeatTimesOnFailure - 1)
                {
                    return RunLinkAndStop(link, attempt + 1);
                }

                if (attempt == _configuration.RepeatTimesOnFailure) return true;

                return _configuration.StopOnFailure;
            }
        }

        private T GetPayloadOrInstance(T payload)
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
