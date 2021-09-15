using Xunit;

namespace Kinetic.Linq.Tests
{
    public class LinqTests
    {
        [Fact]
        public void AnyWithPredicate()
        {
            var executions = 0;
            var source = new Source<int>();

            source.Value.Changed
                .Any(delegate (int value) { return value > 2; })
                .Subscribe(delegate (bool value)
                {
                    executions += 1;
                    Assert.True(value);
                });

            source.Value.Set(1);
            source.Value.Set(2);
            source.Value.Set(3);

            Assert.Equal(1, executions);
        }

        [Fact]
        public void AnyWithoutPredicate()
        {
            var executions = 0;
            var source = new Source<bool>();

            source.Value.Changed
                .Any()
                .Subscribe(delegate (bool value)
                {
                    executions += 1;
                    Assert.True(value);
                });

            source.Value.Set(true);

            Assert.Equal(1, executions);
        }

        private sealed class Source<T> : Object
        {
            private T _value;

            public Source() => _value = default!;
            public Source(T value) => _value = value;
            public Property<T> Value => Property(ref _value);
        }
    }
}