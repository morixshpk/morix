namespace Morix_Tests
{
    public class AllTests
    {
        [Fact]
        public void TestNotNull()
        {
            string emer = "prova";

            Morix.Ensure.NotNull(emer, nameof(emer));
        }

        [Fact]
        public void TestNull()
        {
            object prova = null;

            try
            {
                Morix.Ensure.NotNull(prova, nameof(prova));

                Assert.Fail("null object, expeted exception");
            }
            catch(Exception ex)
            {
                if (ex == null)
                { 

                }
            }
        }
    }
}