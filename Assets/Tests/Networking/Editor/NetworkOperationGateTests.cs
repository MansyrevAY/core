using Core.Resources;
using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Networking.Core;
using NUnit.Framework;

namespace Core.Tests.Networking
{
    public sealed class NetworkOperationGateTests
    {
        private static NetworkOperationGate Gate() => new(() => new RunnerGeneration(1));

        private static Task<OperationResult> Success(NetworkOperationContext context)
        {
            var result = context.Complete(OperationStatus.Success);
            return Task.FromResult(result);
        }
        [Test]
        public async Task CancellationAfterCommitDoesNotRewriteSuccess()
        {
            using var gate = Gate();
            using var cancellation = new CancellationTokenSource();
            var result = await gate.Run(context =>
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                var committed = context.Complete(OperationStatus.Success);
                cancellation.Cancel();
                return Task.FromResult(committed);
            }, cancellation.Token);
            var actual = result.Status;
            var expression = Is.EqualTo(OperationStatus.Success);
            Assert.That(actual, expression);
            Assert.That(gate.IsBusy, Is.False);
            var actual2 = (await gate.Run(Success)).IsSuccess;
            Assert.That(actual2, Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void CreateWithoutNameGetsDistinctUsableRoomNames(string name)
        {
            var first = new CreateSessionRequest(name);
            var second = new CreateSessionRequest(name);
            var actual = string.IsNullOrWhiteSpace(first.Name);
            Assert.That(actual, Is.False);
            var actual2 = first.Name;
            var expression = Is.Not.EqualTo(second.Name);
            Assert.That(actual2, expression);
        }

        [Test]
        public void CreatePreservesExplicitNameAndOptions()
        {
            var request = new CreateSessionRequest("My room", isVisible: false, saveSession: false, isSolo: true);
            var actual = request.Name;
            var expression = Is.EqualTo("My room");
            Assert.That(actual, expression);
            Assert.That(request.IsVisible, Is.False);
            Assert.That(request.SaveSession, Is.False);
            Assert.That(request.IsSolo, Is.True);
        }

        [Test]
        public async Task T01_SecondCommandIsBusyAndDoesNotRun()
        {
            using var gate = Gate();
            var release = new TaskCompletionSource<bool>();
            var calls = 0;
            var first = gate.Run(async context =>
            {
                calls++;
                await release.Task;
                return context.Complete(OperationStatus.Success);
            });
            var second = await gate.Run(context =>
            {
                calls++;
                return Success(context);
            });
            var actual = second.Status;
            var expression = Is.EqualTo(OperationStatus.Busy);
            Assert.That(actual, expression);
            var expression2 = Is.EqualTo(1);
            Assert.That(calls, expression2);
            release.SetResult(true);
            Assert.That((await first).IsSuccess, Is.True);
            Assert.That(gate.IsBusy, Is.False);
            var actual3 = (await gate.Run(Success)).IsSuccess;
            Assert.That(actual3, Is.True);
        }

        [Test]
        public async Task T02_LeaveWaitsForCancelledJoinCleanupAndSharesExit()
        {
            using var gate = Gate();
            var sdkFinished = new TaskCompletionSource<bool>();
            var cleanupFinished = new TaskCompletionSource<bool>();
            var exitFinished = new TaskCompletionSource<bool>();
            NetworkOperationContext joining = null;
            var cleanupObserved = false;
            var exitCalls = 0;
            var join = gate.Run(async context =>
            {
                joining = context;
                try
                {
                    await sdkFinished.Task;
                    context.CancellationToken.ThrowIfCancellationRequested();
                }
                finally
                {
                    await cleanupFinished.Task;
                    cleanupObserved = true;
                }

                return context.Complete(OperationStatus.Success);
            });
            var first = gate.Leave(LeaveMode.KeepReconnect, async context =>
            {
                exitCalls++;
                Assert.That(cleanupObserved, Is.True);
                await exitFinished.Task;
                return context.Complete(OperationStatus.Success);
            });
            var second = gate.Leave(LeaveMode.KeepReconnect, _ => throw new Exception("Must not run twice"));
            Assert.That(joining.CancellationToken.IsCancellationRequested, Is.True);
            Assert.That(exitCalls, Is.Zero);
            sdkFinished.SetResult(true);
            Assert.That(join.IsCompleted, Is.False);
            var actual = (await gate.Run(Success)).Status;
            var expression = Is.EqualTo(OperationStatus.Busy);
            Assert.That(actual, expression);
            cleanupFinished.SetResult(true);
            var actual2 = (await join).Status;
            var expression2 = Is.EqualTo(OperationStatus.Cancelled);
            Assert.That(actual2, expression2);
            await Task.Yield();
            var expression3 = Is.EqualTo(1);
            Assert.That(exitCalls, expression3);
            exitFinished.SetResult(true);
            var actual4 = (await first).OperationId;
            var expression4 = Is.EqualTo((await second).OperationId);
            Assert.That(actual4, expression4);
            Assert.That(gate.IsBusy, Is.False);
        }

        [Test]
        public async Task T02_CancellingOneWaiterDoesNotCancelExitAndForgetDominates()
        {
            using var gate = Gate();
            using var waiting = new CancellationTokenSource();
            var release = new TaskCompletionSource<bool>();
            var appliedMode = LeaveMode.KeepReconnect;
            var first = gate.Leave(LeaveMode.KeepReconnect, async context =>
            {
                await release.Task;
                Assert.That(context.CancellationToken.IsCancellationRequested, Is.False);
                appliedMode = context.LeaveMode;
                return context.Complete(OperationStatus.Success);
            }, waiting.Token);
            var second = gate.Leave(LeaveMode.ForgetSession, Success);
            waiting.Cancel();
            var actual = (await first).Status;
            var expression = Is.EqualTo(OperationStatus.Cancelled);
            Assert.That(actual, expression);
            Assert.That(gate.IsBusy, Is.True);
            Assert.That(second.IsCompleted, Is.False);
            release.SetResult(true);
            Assert.That((await second).IsSuccess, Is.True);
            var expression2 = Is.EqualTo(LeaveMode.ForgetSession);
            Assert.That(appliedMode, expression2);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task T04_ExceptionAtEachBoundaryReleasesGateAfterCleanup(int failureAt)
        {
            using var gate = Gate();
            var cleaned = false;
            var result = await gate.Run(async context =>
            {
                try
                {
                    for (var step = 0; step < 4; step++)
                    {
                        await Task.Yield();
                        if (step == failureAt)
                        {
                            throw new InvalidOperationException("Injected failure");
                        }
                    }

                    return context.Complete(OperationStatus.Success);
                }
                finally
                {
                    await Task.Yield();
                    cleaned = true;
                }
            });
            var actual = result.Status;
            var expression = Is.EqualTo(OperationStatus.Failed);
            Assert.That(actual, expression);
            Assert.That(cleaned, Is.True);
            Assert.That(gate.IsBusy, Is.False);
            var actual2 = (await gate.Run(Success)).IsSuccess;
            Assert.That(actual2, Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public async Task T04_CancellationAtEachBoundaryCompletesCleanupBeforeNextCommand(int cancelAt)
        {
            using var gate = Gate();
            using var cancellation = new CancellationTokenSource();
            var cleaned = false;
            var result = await gate.Run(async context =>
            {
                try
                {
                    for (var step = 0; step < 4; step++)
                    {
                        await Task.Yield();
                        if (step == cancelAt)
                        {
                            cancellation.Cancel();
                        }

                        context.CancellationToken.ThrowIfCancellationRequested();
                    }

                    return context.Complete(OperationStatus.Success);
                }
                finally
                {
                    await Task.Yield();
                    cleaned = true;
                }
            }, cancellation.Token);
            var actual = result.Status;
            var expression = Is.EqualTo(OperationStatus.Cancelled);
            Assert.That(actual, expression);
            Assert.That(cleaned, Is.True);
            var actual2 = (await gate.Run(Success)).IsSuccess;
            Assert.That(actual2, Is.True);
        }

        [Test]
        public async Task T04_CleanupFailureIsNotHiddenByCancellation()
        {
            using var gate = Gate();
            using var cancellation = new CancellationTokenSource();
            var result = await gate.Run(async context =>
            {
                try
                {
                    cancellation.Cancel();
                    context.CancellationToken.ThrowIfCancellationRequested();
                }
                finally
                {
                    await Task.Yield();
                    throw new InvalidOperationException("Shutdown failed");
                }
            }, cancellation.Token);
            var actual = result.Status;
            var expression = Is.EqualTo(OperationStatus.Failed);
            Assert.That(actual, expression);
            Assert.That(gate.IsBusy, Is.False);
        }

        [Test]
        public async Task T04_CancelledBeforeEntryDoesNotStartBody()
        {
            using var gate = Gate();
            Func<NetworkOperationContext, Task<OperationResult>> body = _ => throw new Exception("Must not run");
            var cancellationToken = new CancellationToken(true);
            var result = await gate.Run(body, cancellationToken);
            var actual = result.Status;
            var expression = Is.EqualTo(OperationStatus.Cancelled);
            Assert.That(actual, expression);
            Assert.That(gate.IsBusy, Is.False);
        }

        [Test]
        public async Task T04_ThrowingCancellationCallbackDoesNotStrandLeave()
        {
            using var gate = Gate();
            var finish = new TaskCompletionSource<bool>();
            var join = gate.Run(async context =>
            {
                using (context.CancellationToken.Register(() =>
                           throw new InvalidOperationException("Injected callback failure")))
                {
                    await finish.Task;
                    context.CancellationToken.ThrowIfCancellationRequested();
                    return context.Complete(OperationStatus.Success);
                }
            });
            var leave = gate.Leave(LeaveMode.ForgetSession, Success);
            Assert.That(leave.IsCompleted, Is.False);
            finish.SetResult(true);
            var actual = (await join).Status;
            var expression = Is.EqualTo(OperationStatus.Failed);
            Assert.That(actual, expression);
            Assert.That((await leave).IsSuccess, Is.True);
            Assert.That(gate.IsBusy, Is.False);
        }

        [Test]
        public async Task DisposedGateRejectsAllCommandsWithoutRunningWork()
        {
            var gate = Gate();
            gate.Dispose();
            var ordinary = await gate.Run(_ => throw new Exception("Must not run"));
            var leave = await gate.Leave(LeaveMode.ForgetSession, _ => throw new Exception("Must not run"));
            var actual = ordinary.Error.Code;
            var expression = Is.EqualTo(NetworkErrorCode.Disposed);
            Assert.That(actual, expression);
            var actual2 = leave.Error.Code;
            var expression2 = Is.EqualTo(NetworkErrorCode.Disposed);
            Assert.That(actual2, expression2);
        }

        [Test]
        public void T03_RetiredRunnerCannotPublishOrRetireReplacement()
        {
            var epoch = new RunnerEpoch<object>();
            var first = new object();
            var second = new object();
            var firstGeneration = epoch.Activate(first);
            Assert.Throws<InvalidOperationException>(() => epoch.Activate(second));
            var actual = epoch.Retire(first, firstGeneration);
            Assert.That(actual, Is.True);
            var secondGeneration = epoch.Activate(second);
            var actual2 = secondGeneration.Value;
            var expression = Is.GreaterThan(firstGeneration.Value);
            Assert.That(actual2, expression);
            var actual3 = epoch.IsCurrent(first, secondGeneration);
            Assert.That(actual3, Is.False);
            var actual4 = epoch.IsCurrent(second, firstGeneration);
            Assert.That(actual4, Is.False);
            var actual5 = epoch.Retire(first, firstGeneration);
            Assert.That(actual5, Is.False);
            var actual6 = epoch.IsCurrent(second, secondGeneration);
            Assert.That(actual6, Is.True);
        }

        [TestCase(OperationStatus.Failed)]
        [TestCase(OperationStatus.Busy)]
        [TestCase(OperationStatus.Rejected)]
        public void CompatibilityWrapperCannotTurnFailureIntoSuccess(OperationStatus status)
        {
            var operationId = new OperationId(1);
            var runnerGeneration = new RunnerGeneration(1);
            var result = new OperationResult(status, operationId, runnerGeneration);
            Assert.Throws<NetworkOperationException>(() => result.ThrowIfNotSuccess());
        }
    }
}
