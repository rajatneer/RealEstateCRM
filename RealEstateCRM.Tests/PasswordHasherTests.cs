using RealEstateCRM.Security;

namespace RealEstateCRM.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_then_verify_succeeds()
    {
        var hash = PasswordHasher.Hash("correct horse battery");
        Assert.Equal(PasswordVerifyResult.Success, PasswordHasher.Verify("correct horse battery", hash));
    }

    [Fact]
    public void Wrong_password_fails()
    {
        var hash = PasswordHasher.Hash("correct horse battery");
        Assert.Equal(PasswordVerifyResult.Failed, PasswordHasher.Verify("wrong", hash));
    }

    [Fact]
    public void Hashes_are_salted()
    {
        Assert.NotEqual(PasswordHasher.Hash("same"), PasswordHasher.Hash("same"));
    }

    [Fact]
    public void Legacy_sha256_hash_is_accepted_and_flagged_for_upgrade()
    {
        var legacy = PasswordHasher.LegacySha256("Test123");
        Assert.Equal(64, legacy.Length);
        Assert.Equal(PasswordVerifyResult.SuccessRehashNeeded, PasswordHasher.Verify("Test123", legacy));
        Assert.Equal(PasswordVerifyResult.Failed, PasswordHasher.Verify("nope", legacy));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("v1.notanumber.AAAA.BBBB")]
    [InlineData("v1.1000.!!!.!!!")]
    public void Malformed_stored_values_fail_safely(string stored)
    {
        Assert.Equal(PasswordVerifyResult.Failed, PasswordHasher.Verify("anything", stored));
    }
}
