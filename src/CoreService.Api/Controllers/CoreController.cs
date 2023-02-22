namespace CoreService.Api.Controllers;

using System.Text;
using CoreService.Api.Vaults;
using CoreService.Shared.Internals;
using CoreService.Shared.Payloads;
using Microsoft.AspNetCore.Mvc;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;

[ApiController]
[Route("api/[controller]")]
public class CoreController : ControllerBase
{
    private readonly IVault vault;
    private readonly ILogger logger;

    public CoreController(IVault vault, ILogger<CoreController> logger)
    {
        this.vault = vault;
        this.logger = logger;
    }

    /// <summary>
    ///     Get initialization status and health status of CoreService.
    /// </summary>
    /// <returns>InitStatus.</returns>
    /// <remarks>
    ///     The `SubkeyPublic` is in Hex format.
    /// </remarks>
    /// <response code="200">Returns CoreService is initialized or not, and if initialized, returns the public key of its subkey.</response>
    [HttpGet("status", Name = "Get initialization status")]
    public async ValueTask<ActionResult<SetupStatus>> GetStatusAsync()
    {
        try
        {
            var internals = await vault.LoadInternalAsync();
            return new SetupStatus(!string.IsNullOrEmpty(internals.Subkey.Signature), internals.Subkey.Public);
        }
        catch (Exception)
        {
            return new SetupStatus(false, string.Empty);
        }
    }

    /// <summary>
    ///     Generate a key pair as subkey which is to be signed by the Avatar.
    ///     This is the pre-setup step.
    /// </summary>
    /// <returns>InitStatus.</returns>
    /// <remarks>
    ///     The `SubkeyPublic` is in Hex format.
    ///     To sign the public key using the Avatar, the signature **MUST** be `eth_sign`ed with
    ///     `Subkey certification signature: ${subkey_public_key_hex}`.
    /// </remarks>
    /// <response code="200">Returns the public key to be signed.</response>
    /// <response code="400">If already setup.</response>
    [HttpPost("generate", Name = "Generate subkey keypair")]
    public async ValueTask<ActionResult<SetupStatus>> GenerateAsync()
    {
        Internal internals;

        try
        {
            internals = await vault.LoadInternalAsync();
        }
        catch (Exception)
        {
            internals = new Internal(
                new Subkey(
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty),
                new Host(string.Empty));
        }

        if (!string.IsNullOrEmpty(internals.Subkey.Signature))
        {
            return Problem("Already setup.", null, StatusCodes.Status409Conflict);
        }

        var eckey = EthECKey.GenerateKey();
        internals = internals with
        {
            Subkey = new(
                eckey.GetPrivateKey(),
                eckey.GetPubKey(true).ToHex(true),
                string.Empty,
                string.Empty),
        };

        try
        {
            await vault.SaveInternalAsync(internals);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, null, StatusCodes.Status500InternalServerError);
        }

        return new SetupStatus(false, internals.Subkey.Public);
    }

    /// <summary>
    ///     Sending the subkey certification signature (by the Avatar) to the backend
    ///     to complete the initialization process.
    /// </summary>
    /// <param name="setup">The configuration.</param>
    /// <returns>Ok.</returns>
    /// <remarks>
    ///     `HOST:DOMAIN` should be the domain name of CoreService, e.g. `localhost`.
    /// </remarks>
    /// <response code="200">If setup correctly.</response>
    /// <response code="400">If signature is invalid or domain is invalid.</response>
    /// <response code="409">If already setup.</response>
    [HttpPost("setup")]
    public async ValueTask<ActionResult> SetupAsync(Internal setup)
    {
        Internal internals;

        try
        {
            internals = await vault.LoadInternalAsync();
        }
        catch (Exception)
        {
            internals = new Internal(
            new Subkey(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty),
            new Host(string.Empty));
        }

        if (!string.IsNullOrEmpty(internals.Subkey.Signature))
        {
            return Problem("Already setup.", null, StatusCodes.Status409Conflict);
        }

        if (string.IsNullOrEmpty(setup.Host.Domain))
        {
            return Problem("Invalid domain.", null, StatusCodes.Status400BadRequest);
        }

        string pubKey;
        var privKey = string.IsNullOrEmpty(internals.Subkey.Private)
            ? setup.Subkey.Private
            : internals.Subkey.Private;

        try
        {
            pubKey = ExtractPublicKey(privKey, setup.Subkey.Avatar, setup.Subkey.Signature);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, null, StatusCodes.Status400BadRequest);
        }

        var result = setup with
        {
            Subkey = setup.Subkey with
            {
                Private = privKey,
                Public = pubKey,
            },
        };

        await vault.SaveInternalAsync(result);
        return Ok();
    }

    private static string ExtractPublicKey(string privKey, string avatar, string signature)
    {
        var subkey = new EthECKey(privKey);
        var signer = new EthereumMessageSigner();

        var recoveredAvatar = EthECKey.RecoverFromSignature(
            MessageSigner.ExtractEcdsaSignature(signature),
            signer.HashPrefixedMessage(
                Encoding.UTF8.GetBytes($"Subkey certification signature: {subkey.GetPubKey(true).ToHex(true)}")))
        .GetPubKey(true)
        .ToHex(true);

        if (!string.Equals(avatar, recoveredAvatar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Avatar recovered does not match.", nameof(avatar));
        }

        return subkey.GetPubKey(true).ToHex(true);
    }
}
