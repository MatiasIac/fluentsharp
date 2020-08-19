using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FunctionalSharp.Collections.Tests
{
    [TestClass()]
    public class AsyncIteratorsTests
    {
        private IEnumerable<int> _collection;

        [TestInitialize]
        public void Setup()
        {
            _collection = new List<int> { 1, 2, 3, 4, 5, 6, 7 };
        }

        [TestMethod()]
        public async Task When_ThenAsync_PerformsOperation_ExpectExecution()
        {
            var sum = 0;
            await _collection.ThenAsync(collection => sum = collection.Sum());

            Assert.AreEqual(28, sum);
        }

        [TestMethod()]
        public async Task When_ForEveryAsync_Iterate_ExpectExecution()
        {
            var sum = 0;
            await _collection.ForEveryAsync(item => sum += item);

            Assert.AreEqual(28, sum);
        }

        [TestMethod()]
        public async Task When_ForAsync_CannotContinue_Expect_LastIndex()
        {
            var index = -1;
            var sum = 0;

            await _collection.ForAsync(
                (item, i) => { 
                    index = i; 
                    return item < 3; 
                },
                item => sum += item
            );

            Assert.AreEqual(2, index);
            Assert.AreEqual(3, sum);
        }

        [TestMethod()]
        public async Task When_ForAsync_Iterate_ExpectExecution()
        {
            var index = 0;
            await _collection.ForAsync((item) => ++index < 3);

            Assert.AreEqual(3, index);
        }

        [TestMethod()]
        public async Task When_ForAsync_IterateWithoutIndex_ExpectExecution()
        {
            var sum = 0;
            await _collection.ForAsync(c => c <= 3, item => sum += item);

            Assert.AreEqual(6, sum);
        }

        [TestMethod()]
        public async Task When_Alter_ReturnsNewCollection_Expect_Modifications()
        {
            var newCollection = await _collection.AlterAsync(col => {
                var c = col.ToList();
                c.Add(10);
                return c;
            });

            Assert.AreEqual(8, newCollection.Count());
        }
    }
}