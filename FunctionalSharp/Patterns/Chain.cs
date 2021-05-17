using System;
using System.Collections.Generic;
using System.Text;

namespace FunctionalSharp.Patterns
{
    public sealed class Chain<T>
    {
        public struct DataCargo
        {
            public T Data;
        }

        private DataCargo _dataCargo;
        private bool _stopOnFailure = false;

        internal Chain(bool stopOnFailure)
        {
            _dataCargo = new DataCargo();
            _stopOnFailure = stopOnFailure;
        }

        public static Chain<T> Create(bool stopOnFailure = false)
        {
            return new Chain<T>(stopOnFailure);
        }

        public Chain<T> AddLink(string name, Action<DataCargo> action)
        {
            return this;
        }

        public Chain<T> Success()
        {
            return this;
        }

        public Chain<T> Error()
        {
            return this;
        }

        public void Run()
        {

        }

        public sealed class Link
        {

        }
    }
}