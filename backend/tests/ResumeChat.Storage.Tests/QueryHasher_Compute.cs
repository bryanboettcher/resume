using ResumeChat.Storage;

namespace ResumeChat.Storage.Tests;

public abstract class QueryHasher_Compute
{
    protected string Result = null!;

    public class When_identical_queries : QueryHasher_Compute
    {
        [Test]
        public void It_should_produce_same_hash()
        {
            QueryHasher.Compute("big data").ShouldBe(QueryHasher.Compute("big data"));
        }
    }

    public class When_case_differs : QueryHasher_Compute
    {
        [Test]
        public void It_should_produce_same_hash()
        {
            QueryHasher.Compute("Big Data").ShouldBe(QueryHasher.Compute("big data"));
        }
    }

    public class When_punctuation_differs : QueryHasher_Compute
    {
        [Test]
        public void It_should_produce_same_hash()
        {
            QueryHasher.Compute("Bryan's big-data experience!").ShouldBe(
                QueryHasher.Compute("bryans bigdata experience"));
        }
    }

    public class When_whitespace_differs : QueryHasher_Compute
    {
        [Test]
        public void It_should_produce_same_hash()
        {
            QueryHasher.Compute("big  data").ShouldBe(QueryHasher.Compute("big data"));
        }
    }

    public class When_queries_are_different : QueryHasher_Compute
    {
        [Test]
        public void It_should_produce_different_hashes()
        {
            QueryHasher.Compute("big data").ShouldNotBe(QueryHasher.Compute("kubernetes"));
        }
    }

    public class When_input_is_empty : QueryHasher_Compute
    {
        [Test]
        public void It_should_not_throw()
        {
            Should.NotThrow(() => QueryHasher.Compute(""));
        }

        [Test]
        public void It_should_return_8_char_hex()
        {
            QueryHasher.Compute("").Length.ShouldBe(8);
        }
    }
}
