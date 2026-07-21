using NuclearReactorSimulator.Simulation.Runtime;
using Xunit;

namespace NuclearReactorSimulator.Simulation.Tests.Runtime;

public sealed class SimulationCommandQueueTests
{
    [Fact]
    public void Enqueue_AssignsMonotonicSequenceNumbers()
    {
        var queue = new SimulationCommandQueue<string>();

        var first = queue.Enqueue("first");
        var second = queue.Enqueue("second");
        var third = queue.Enqueue("third");

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(3, third);
        Assert.Equal(3, queue.Count);
    }

    [Fact]
    public void RestoreToFront_PreservesOriginalOrderAheadOfLaterCommands()
    {
        var queue = new SimulationCommandQueue<string>();
        queue.Enqueue("first");
        queue.Enqueue("second");

        var drained = queue.Drain();
        queue.Enqueue("third");
        queue.RestoreToFront(drained);

        var restored = queue.Drain();

        Assert.Equal(new[] { "first", "second", "third" }, restored.Select(item => item.Command));
        Assert.Equal(new long[] { 1, 2, 3 }, restored.Select(item => item.Sequence));
    }
}
