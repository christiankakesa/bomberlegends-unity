using System;
using System.Collections;
using BomberLegends.Services.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BomberLegends.Tests.PlayMode
{
    /// <summary>
    /// Covers the threaded save path, which needs a running player loop to hop back to the main
    /// thread and therefore cannot be exercised from EditMode.
    /// </summary>
    public sealed class SaveBackgroundIoTests
    {
        private const int TimeoutFrames = 600;

        [UnityTest]
        public IEnumerator SaveAndLoad_OffTheMainThread_RoundTripsAndReturnsToTheMainThread()
        {
            var repository = new MemorySaveRepository(supportsBackgroundIo: true);
            var writer = new SaveService(repository);
            writer.Data.DataCoins = 8080;
            writer.Data.BombRangeLevel = 2;

            var mainThreadId = Environment.CurrentManagedThreadId;
            var finished = false;
            var completedOnThreadId = -1;
            Exception? failure = null;

            async void RunRoundTrip()
            {
                try
                {
                    await writer.SaveAsync();
                    await writer.LoadAsync();
                    completedOnThreadId = Environment.CurrentManagedThreadId;
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
                finally
                {
                    finished = true;
                }
            }

            RunRoundTrip();

            var frames = 0;
            while (!finished && frames < TimeoutFrames)
            {
                frames++;
                yield return null;
            }

            Assert.That(failure, Is.Null, $"the threaded path threw: {failure}");
            Assert.That(finished, Is.True, "the threaded save did not complete within the timeout");
            Assert.That(completedOnThreadId, Is.EqualTo(mainThreadId),
                "control must return to the main thread before the caller resumes");
            Assert.That(writer.Data.DataCoins, Is.EqualTo(8080));
            Assert.That(writer.Data.BombRangeLevel, Is.EqualTo(2));
            Assert.That(writer.IsDirty, Is.False);
        }

        [UnityTest]
        public IEnumerator LifecycleHandler_FlushesWhenTheApplicationIsBackgrounded()
        {
            var repository = new MemorySaveRepository(supportsBackgroundIo: false);
            var service = new SaveService(repository);
            service.Data.DataCoins = 321;
            service.MarkDirty();

            var host = new GameObject(nameof(SaveLifecycleHandler));
            try
            {
                var handler = host.AddComponent<SaveLifecycleHandler>();
                handler.Initialise(service);

                yield return null;

                // Unity delivers OnApplicationPause to every component on the object.
                host.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);

                Assert.That(repository.WriteCount, Is.EqualTo(1),
                    "backgrounding must flush synchronously; on Android this is often the last callback");
                Assert.That(service.IsDirty, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [UnityTest]
        public IEnumerator LifecycleHandler_WithNoPendingChanges_DoesNotWrite()
        {
            var repository = new MemorySaveRepository(supportsBackgroundIo: false);
            var service = new SaveService(repository);

            var host = new GameObject(nameof(SaveLifecycleHandler));
            try
            {
                var handler = host.AddComponent<SaveLifecycleHandler>();
                handler.Initialise(service);

                yield return null;

                host.SendMessage("OnApplicationPause", true, SendMessageOptions.DontRequireReceiver);

                Assert.That(repository.WriteCount, Is.EqualTo(0), "a clean save should not be rewritten");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }
}
