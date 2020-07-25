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
        public void When_ThrowExceptionIfTrue_Evaluates_True_Expects_Exception()
        {
            true.ThrowExceptionIfTrue();
        }

        [TestMethod()]
        public void When_ThrowExceptionIfTrue_Evaluates_False_Expects_NoException()
        {
            false.ThrowExceptionIfTrue();
            Assert.IsTrue(true);
        }

        [TestMethod()]
        [ExpectedException(typeof(CustomException))]
        public void When_ThrowExceptionIfTrue_Evaluates_TrueWithCustomException_Expects_CustomException()
        {
            true.ThrowExceptionIfTrue(new CustomException("Custom message"));
        }

        private class CustomException : Exception
        {
            public CustomException(string message) : base(message) { }
        }
    }
}