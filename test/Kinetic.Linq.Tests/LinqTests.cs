using Xunit;

namespace Kinetic.Linq.Tests
{
    public class LinqTests
    {
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