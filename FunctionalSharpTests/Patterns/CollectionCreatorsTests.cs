using Microsoft.VisualStudio.TestTools.UnitTesting;
using FunctionalSharp.Patterns;
using System;
using System.Collections.Generic;
using System.Text;
using static FunctionalSharp.Patterns.CollectionCreators;

namespace FunctionalSharp.Patterns.Tests
{
    [TestClass()]
    public class CollectionCreatorsTests
    {
        [TestMethod()]
        public void RangeTest()
        {
            Creator<int>.Range(10);
        }
    }
}