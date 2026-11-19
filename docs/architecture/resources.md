# Resources

Source folder: `Assets/Scripts/Core/Resources`. Files: ResourceScope.cs, LeaseSlot.cs.

Responsibility: [Coop sessions](coop-sessions.md).

## Purpose

`ResourceScope` releases owned resources in reverse order. It continues after cleanup failures. Late ownership releases the resource immediately. `LeaseSlot` prevents an old lease from clearing a newer registration.

## Ownership rules

`Own` transfers cleanup responsibility to the scope. Do not dispose the same resource from another owner.
`Dispose` is idempotent. It marks the scope disposed before it releases resources.
If cleanup methods throw, the scope still releases the other resources and then throws `AggregateException`.
`Own` on a disposed scope first disposes the incoming resource and then throws `ObjectDisposedException`.

`LeaseSlot.Register` replaces `Current` and returns a registration lease.
Disposing an older lease does not clear a newer registration.
The slot does not own the registered value. The application must dispose the value separately if required.

The local player bootstrap uses `ResourceScope` for the application's initialization lease.
The Fusion runner uses `LeaseSlot<IPlayerInputSource>` for its current input source.
The caller owns that registration lease for the local player's lifetime.
