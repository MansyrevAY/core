using Core.Resources;
using Core.Networking.Core;
using NUnit.Framework;

namespace Core.Tests.Networking
{
    public sealed class SceneRegistrationTests
    {
        [Test]
        public void T19_PreviousRunnerRegistrationCannotCompleteNewRunnerWithSameScene()
        {
            var operationId = new OperationId(1);
            var runnerGeneration = new RunnerGeneration(1);
            var old = new SceneRegistrationOperation<string>(operationId, runnerGeneration, "Apart_01");
            old.ObserveRegistration(old.Generation, old.Target);
            var operationId2 = new OperationId(2);
            var runnerGeneration2 = new RunnerGeneration(2);
            var current = new SceneRegistrationOperation<string>(operationId2, runnerGeneration2, "Apart_01");
            var actual = current.ObserveRegistration(old.Generation, old.Target);
            Assert.That(actual, Is.False);
            Assert.That(current.IsWaiting, Is.True);
            var actual2 = current.ObserveRegistration(current.Generation, current.Target);
            Assert.That(actual2, Is.True);
        }

        [Test]
        public void T20_UnrelatedAndOldProgressDoNotChangeTargetWait()
        {
            var operationId = new OperationId(1);
            var runnerGeneration = new RunnerGeneration(2);
            var operation = new SceneRegistrationOperation<int>(operationId, runnerGeneration, 7);
            operation.ObserveProgress(operation.Generation, 7, 0.4f);
            var runnerGeneration2 = new RunnerGeneration(1);
            var actual = operation.ObserveProgress(runnerGeneration2, 7, 1f);
            Assert.That(actual, Is.False);
            var actual2 = operation.ObserveProgress(operation.Generation, 2, 1f);
            Assert.That(actual2, Is.False);
            var actual3 = operation.ObserveRegistration(operation.Generation, 2);
            Assert.That(actual3, Is.False);
            var actual4 = operation.Progress;
            var expression = Is.EqualTo(0.4f);
            Assert.That(actual4, expression);
            Assert.That(operation.IsWaiting, Is.True);
        }

        [Test]
        public void T19_FullLoadProgressStillRequiresActualRegistration()
        {
            var operationId = new OperationId(1);
            var runnerGeneration = new RunnerGeneration(2);
            var operation = new SceneRegistrationOperation<int>(operationId, runnerGeneration, 7);
            operation.ObserveProgress(operation.Generation, 7, 1f);
            Assert.That(operation.IsWaiting, Is.True);
            operation.ObserveProgress(operation.Generation, 7, float.NaN);
            var actual = operation.Progress;
            var expression = Is.EqualTo(1f);
            Assert.That(actual, expression);
            operation.ObserveRegistration(operation.Generation, 7);
            var actual2 = operation.Status;
            var expression2 = Is.EqualTo(SceneRegistrationStatus.Registered);
            Assert.That(actual2, expression2);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void T22_TerminalWaitCannotBeRevivedByLateRegistration(bool failure)
        {
            var operationId = new OperationId(1);
            var runnerGeneration = new RunnerGeneration(2);
            var old = new SceneRegistrationOperation<int>(operationId, runnerGeneration, 7);
            old.Stop(failure);
            var operationId2 = new OperationId(2);
            var retry = new SceneRegistrationOperation<int>(operationId2, old.Generation, old.Target);
            var actual = old.ObserveRegistration(old.Generation, old.Target);
            Assert.That(actual, Is.False);
            var actual2 = old.ObserveProgress(old.Generation, old.Target, 1f);
            Assert.That(actual2, Is.False);
            var actual3 = old.Status;
            var expression = Is.EqualTo(failure ? SceneRegistrationStatus.Failed : SceneRegistrationStatus.Cancelled);
            Assert.That(actual3, expression);
            Assert.That(retry.IsWaiting, Is.True);
            var actual4 = retry.ObserveRegistration(retry.Generation, retry.Target);
            Assert.That(actual4, Is.True);
        }
    }
}
