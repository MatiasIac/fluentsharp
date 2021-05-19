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

        public struct DataCargo
        {
            public T Payload;
        }

        private DataCargo _dataCargo;
        private readonly bool _stopOnFailure = false;
        private readonly List<LinkBase> _chain;
        private Action<DataCargo> _completeAction;
        private Action<DataCargo, Exception> _errorAction;

        internal Chain(T payload, bool stopOnFailure)
        {
            _dataCargo = new DataCargo
            {
                Payload = payload ?? Activator.CreateInstance<T>()
            };

            _stopOnFailure = stopOnFailure;
            _chain = new List<LinkBase>();
        }

        public static Chain<T> Create(bool stopOnFailure = false) => new Chain<T>(null, stopOnFailure);

        public static Chain<T> Create(T payload, bool stopOnFailure = false) => new Chain<T>(payload, stopOnFailure);

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
                    link.OnExecute(_dataCargo);
                }
                catch (Exception ex)
                {
                    _errorAction?.Invoke(_dataCargo, ex);

                    if (_stopOnFailure) break;
                }
            }

            _completeAction?.Invoke(_dataCargo);
        }

        public Chain<T> OnComplete(Action<DataCargo> action)
        {
            _completeAction = action;
            return this;
        }

        public Chain<T> OnError(Action<DataCargo, Exception> action)
        {
            _errorAction = action;
            return this;
        }

    }
}