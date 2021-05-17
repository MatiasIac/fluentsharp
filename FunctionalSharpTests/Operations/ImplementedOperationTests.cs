using Microsoft.VisualStudio.TestTools.UnitTesting;
using FunctionalSharp.Validators;
using System;
using System.Collections.Generic;
using System.Text;

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


            var chain = FunctionalSharp.Patterns.Chain<MyData>.Create(true);
            chain.AddLink("FirstLink", (cargo) =>
            {
                
            });
        }
    }

    public class MyData
    {

    }
}