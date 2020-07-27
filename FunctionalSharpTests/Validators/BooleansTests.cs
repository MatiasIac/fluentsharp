using FunctionalSharp.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace FunctionalSharp.Tests
{
    [TestClass()]
    public class BooleansTests
    {
        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void When_IfTrue_Evaluates_True_Expects_Exception()
        {
            true
                .IfTrue()
                .Throw(new Exception("message"));
        }

        [TestMethod()]
        public void When_IfTrue_Evaluates_False_Expects_NoException()
        {
            false
                .IfTrue()
                .Throw(new Exception("message"));

            Assert.IsTrue(true);
        }

        [TestMethod()]
        [ExpectedException(typeof(CustomException))]
        public void When_IfTrue_Evaluates_TrueWithCustomException_Expects_CustomException()
        {
            true
                .IfTrue()
                .Throw(new CustomException("Custom message"));
        }

        [TestMethod()]
        [ExpectedException(typeof(Exception))]
        public void When_IfFalse_Evaluates_False_Expects_Exception()
        {
            false
                .IfFalse()
                .Throw(new Exception("message"));
        }

        [TestMethod()]
        public void When_IfFalse_Evaluates_True_Expects_NoException()
        {
            true
                .IfFalse()
                .Throw(new Exception("message"));

            Assert.IsTrue(true);
        }

        [TestMethod()]
        [ExpectedException(typeof(CustomException))]
        public void When_IfFalse_Evaluates_FalseWithCustomException_Expects_CustomException()
        {
            false
                .IfFalse()
                .Throw(new CustomException("Custom message"));
        }

        private class CustomException : Exception
        {
            public CustomException(string message) : base(message) { }
        }
    }
}