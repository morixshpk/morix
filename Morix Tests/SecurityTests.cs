namespace Morix_Tests
{
    public class SecurityTests
    {
        [Fact]
        public void TestNull()
        {
            var hash1 = Morix.Security.Instance.Hash("0");
            var hash2 = Morix.Security.Instance.Hash("0");

            Assert.NotNull(hash1);
            Assert.NotNull(hash2);

            Assert.True(hash1.Length == hash2.Length);
            Assert.True(hash1.Length == 64);
            Assert.True(hash1.Equals(hash2));
        }

        [Fact]
        public void TestEncryptionDecryption()
        {
            var plaintext = "test";
            var encrypted = Morix.Security.Instance.Encrypt(plaintext);
            var decrypted = Morix.Security.Instance.Decrypt(encrypted);

            Assert.True(plaintext.Equals(decrypted));
        }
    }
}