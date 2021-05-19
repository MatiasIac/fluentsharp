using FunctionalSharp.Validators;
using System;
using System.Collections.Generic;

namespace FunctionalSharp.Patterns
{

    public sealed class Chain<T> where T : class, new()
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
        private Action<T, Exception> _errorAction;

        internal Chain(T payload, Configuration configuration)
        {
            _dataCargo = new DataCargo
            {
                Payload = payload ?? Activator.CreateInstance<T>()
            };

            _configuration = configuration ?? new Configuration();

            _chain = new List<LinkBase>();
        }

        public static Chain<T> Create() => new Chain<T>(null, null);

        public static Chain<T> Create(Configuration configuration) => new Chain<T>(null, configuration);

        public static Chain<T> Create(T payload, Configuration configuration = null) => new Chain<T>(payload, configuration);

        public Chain<T> AddLink(Action<DataCargo> action) => AddLink(new Link(action));

        public Chain<T> AddLink(LinkBase link)
        {
            link.IfNull().Throw(new Exception("Chain Link cannot be null"));

            _chain.Add(link);

            return this;
        }

        public void Run()
        {
            foreach (var link in _chain)
            {
                try
                {
                    _dataCargo.Cancel = false;

                    link.OnExecute(_dataCargo);

                    if (_dataCargo.Cancel) break;
                }
                catch (Exception ex)
                {
                    _errorAction?.Invoke(_dataCargo.Payload, ex);

                    if (_configuration.StopOnFailure) break;
                }
            }

            _completeAction?.Invoke(_dataCargo.Payload);
        }

        public Chain<T> OnComplete(Action<T> action)
        {
            _completeAction = action;
            return this;
        }

        public Chain<T> OnError(Action<T, Exception> action)
        {
            _errorAction = action;
            return this;
        }

    }
}