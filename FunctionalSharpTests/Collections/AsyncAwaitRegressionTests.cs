using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace FunctionalSharp.Collections.Tests
{
    [TestClass]
    public class AsyncAwaitRegressionTests
    {
        [TestMethod]
        public async Task ThenAsync_RemainsIncomplete_UntilItsCallbackCompletes()
        {
            var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callbackFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var source = new[] { 1, 2, 3 };
            var operation = source.ThenAsync(async values =>
            {
                await release.Task;
                callbackFinished.SetResult(true);
            });

            try
            {
                Assert.IsFalse(operation.IsCompleted, "The operation must await its callback.");
            }
            finally
            {
                release.TrySetResult(true);
                await operation.WaitAsync(TimeSpan.FromSeconds(10));
                await callbackFinished.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }

            Assert.AreSame(source, await operation);
        }
    }
}
