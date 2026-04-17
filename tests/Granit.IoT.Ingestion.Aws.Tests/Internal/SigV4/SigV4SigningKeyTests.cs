using Granit.IoT.Ingestion.Aws.Internal.SigV4;
using Shouldly;

namespace Granit.IoT.Ingestion.Aws.Tests.Internal.SigV4;

/// <summary>
/// Exercises key derivation + signing against the <c>get-vanilla</c> vector —
/// proving end-to-end we can reproduce the same 64-char hex signature the
/// AWS SDK emits for the same canonical request.
/// </summary>
public class SigV4SigningKeyTests
{
    private const string SecretAccessKey = "wJalrXUtnFEMI/K7MDENG+bPxRfiCYEXAMPLEKEY";

    [Fact]
    public void Derive_produces_a_32_byte_signing_key()
    {
        SigV4Scope scope = new("20150830", "us-east-1", "service");
        byte[] key = SigV4SigningKey.Derive(SecretAccessKey, scope);
        key.Length.ShouldBe(32);
    }

    [Fact]
    public void Derive_is_deterministic_for_the_same_scope()
    {
        SigV4Scope scope = new("20150830", "us-east-1", "service");
        byte[] first = SigV4SigningKey.Derive(SecretAccessKey, scope);
        byte[] second = SigV4SigningKey.Derive(SecretAccessKey, scope);
        first.ShouldBe(second);
    }

    [Fact]
    public void Derive_differs_across_scopes_even_with_the_same_secret()
    {
        byte[] a = SigV4SigningKey.Derive(SecretAccessKey, new("20150830", "us-east-1", "service"));
        byte[] b = SigV4SigningKey.Derive(SecretAccessKey, new("20150831", "us-east-1", "service"));
        a.ShouldNotBe(b);
    }

    [Fact]
    public void Sign_reproduces_the_get_vanilla_signature_from_the_AWS_test_suite()
    {
        // From AWS SigV4 test suite (get-vanilla.sreq).
        SigV4Scope scope = new("20150830", "us-east-1", "service");
        byte[] signingKey = SigV4SigningKey.Derive(SecretAccessKey, scope);

        const string stringToSign =
            "AWS4-HMAC-SHA256\n" +
            "20150830T123600Z\n" +
            "20150830/us-east-1/service/aws4_request\n" +
            "bb579772317eb040ac9ed261061d46c1f17a8133879d6129b6e1c25292927e63";

        string signature = SigV4SigningKey.Sign(signingKey, stringToSign);
        signature.ShouldBe("5fa00fa31553b73ebf1942676e86291e8372ff2a2260956d9b8aae1d763fbf31");
    }
}
