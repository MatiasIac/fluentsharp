using FunctionalSharp.Validators;
using System;
using System.Collections.Generic;

namespace FunctionalSharp.Patterns
{

    public sealed class GenericChain<T>
    {
        public abstract class LinkBase
        {
            public abstract void OnExecute(DataCargo data);
        }

        private sealed class Link : LinkBase
        {
            private readonly Action<DataCargo> _action;

            public Link(Action<DataCargo> action)
            {
                _action = action;
            }

            public override void OnExecute(DataCargo data) => _action.Invoke(data);
        }

        public sealed class Configuration
        {
            public bool StopOnFailure { get; }
            public int RepeatTimesOnFailure { get; }

            public Configuration(
                bool stopOnFailure = true, 
                int repeatTimesOnFailure = 0
            )
            {
                StopOnFailure = stopOnFailure;
                RepeatTimesOnFailure = repeatTimesOnFailure;
            }
        }

        public class DataCargo
        {
            public T Payload;
            public bool Cancel { get; set; }
        }

        private readonly DataCargo _dataCargo;
        private readonly Configuration _configuration;
        private readonly List<LinkBase> _chain;
        private Action<T> _completeAction;
        private Action<DataCargo, Exception> _errorAction;

        internal GenericChain(T payload, Configuration configuration)
        {
            _dataCargo = new DataCargo
            {
                Payload = GetPayloadOrInstance(payload)
            };

            _configuration = configuration ?? new Configuration();

            _chain = new List<LinkBase>();
        }

        public static GenericChain<T> Create() => new GenericChain<T>(default, null);

        public static GenericChain<T> Create(Configuration configuration) => new GenericChain<T>(default, configuration);

        public static GenericChain<T> Create(T payload, Configuration configuration = null) => new GenericChain<T>(payload, configuration);

        public GenericChain<T> AddLink(Action<DataCargo> action) => AddLink(new Link(action));

        public GenericChain<T> AddLink(LinkBase link)
        {
            link.IfNull().Throw(new Exception("Chain Link cannot be null"));

            _chain.Add(link);

            return this;
        }

        public void Run()
        {
            foreach (var link in _chain)
            {
                if (RunLinkAndStop(link)) break;
            }

            _completeAction?.Invoke(_dataCargo.Payload);
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
        public GenericChain<T> OnError(Action<DataCargo, Exception> action)
        {
            _errorAction = action;
            return this;
        }

        private bool RunLinkAndStop(LinkBase link, int attempt = 0)
        {
            try
            {
                _dataCargo.Cancel = false;
                link.OnExecute(_dataCargo);
                return _dataCargo.Cancel;
            }
            catch (Exception ex)
            {
                _errorAction?.Invoke(_dataCargo, ex);

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
    }
}