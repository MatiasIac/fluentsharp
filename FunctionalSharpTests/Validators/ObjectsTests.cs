using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FunctionalSharp.Validators.Tests
{
    [TestClass()]
    public class ObjectsTests
    {
        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void When_IfNull_Evaluates_False_Expects_Exception()
        {
            default(object)
                .IfNull()
                .Throw(new Exception("message"));
        }

        [TestMethod()]
        [ExpectedException(typeof(CustomException))]
        public void When_IfNull_Evaluates_FalseWithCustomException_Expects_CustomException()
        {
            default(object)
                .IfNull()
                .Throw(new CustomException("Custom message"));
        }

        private class CustomException : Exception
        {
            public CustomException(string message) : base(message) { }
        }
    }
}