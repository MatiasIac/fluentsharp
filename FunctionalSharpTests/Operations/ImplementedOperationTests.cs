using FunctionalSharp.Validators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FunctionalSharp.Operations.Tests
{
    [TestClass()]
    public class ImplementedOperationTests
    {
        [TestMethod()]
        public void When_Then_Evaluates_ExpectResults()
        {
            var result = string.Empty;

            true
                .IfTrue()
                .Then(() => result = "test passed");
            
            Assert.AreEqual("test passed", result);
        }
    }

}