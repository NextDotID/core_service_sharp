namespace CoreService.Api.Controllers;

using System.Text;
using CoreService.Api.Vaults;
using CoreService.Shared.Internals;
using CoreService.Shared.Payloads;
using FluentResults;
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
    /// <response code="200">Initialized or not, and if initialized, returns the public key of its subkey.</response>
    [HttpGet("status", Name = "Get initialization status")]
    public async ValueTask<ActionResult<InitStatus>> GetStatusAsync()
    {
        var res = await vault.LoadInternalAsync();
        return new InitStatus(
            res.IsSuccess && !string.IsNullOrEmpty(res.Value.Subkey.Signature),
            res.ValueOrDefault?.Subkey?.Public ?? string.Empty);
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
    /// <response code="400">Already setup.</response>
    /// <response code="500">Error when generating the signature.</response>
    [HttpPost("generate", Name = "Generate subkey keypair")]
    public async ValueTask<ActionResult<InitStatus>> GenerateAsync()
    {
        var load = await vault.LoadInternalAsync();
        if (load.IsSuccess && !string.IsNullOrEmpty(load.Value.Subkey.Signature))
        {
            return Problem("already setup");
        }

        var internals = load.ValueOrDefault ?? new Internal(
            new Subkey(
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty),
            new Host(string.Empty));
        var eckey = EthECKey.GenerateKey();
        internals = internals with
        {
            Subkey = new(
                eckey.GetPrivateKey(),
                eckey.GetPubKey(true).ToHex(true),
                string.Empty,
                string.Empty),
        };

        var save = await vault.SaveInternalAsync(internals);
        if (save.IsFailed)
        {
            return Problem(save.Errors.First().Message, null, StatusCodes.Status500InternalServerError);
        }

        return new InitStatus(false, internals.Subkey.Public);
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
    /// <response code="400">Invalid signature.</response>
    /// <response code="500">Error when saving the configuration.</response>
    [HttpPost("setup")]
    public async ValueTask<ActionResult> SetupAsync(Internal setup)
    {
        var load = await vault.LoadInternalAsync();
        if (load.IsSuccess && !string.IsNullOrEmpty(load.Value.Subkey.Signature))
        {
            return Problem("already setup");
        }

        if (string.IsNullOrEmpty(setup.Host.Domain))
        {
            return Problem("invalid domain");
        }

        var privKey = string.IsNullOrEmpty(load.ValueOrDefault?.Subkey?.Private)
            ? setup.Subkey.Private
            : load.Value.Subkey.Private;
        var validate = ExtractPublicKey(privKey, setup.Subkey.Avatar, setup.Subkey.Signature);
        if (validate.IsFailed)
        {
            return Problem(string.Join('\n', validate.Reasons.Select(r => r.Message)));
        }

        var internals = setup with
        {
            Subkey = setup.Subkey with
            {
                Private = privKey,
                Public = validate.Value,
            },
        };

        var res = await vault.SaveInternalAsync(internals);
        return res.IsSuccess ? Ok() : Problem("failed to init", null, StatusCodes.Status500InternalServerError);
    }

    private static Result<string> ExtractPublicKey(string privKey, string avatar, string signature)
    {
        EthECKey subkey;
        var signer = new EthereumMessageSigner();

        try
        {
            subkey = new EthECKey(privKey);
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }

        var recoveredAvatar = EthECKey.RecoverFromSignature(
            MessageSigner.ExtractEcdsaSignature(signature),
            signer.HashPrefixedMessage(
                Encoding.UTF8.GetBytes($"Subkey certification signature: {subkey.GetPubKey(true).ToHex(true)}")))
        .GetPubKey(true)
        .ToHex(true);

        if (!string.Equals(avatar, recoveredAvatar, StringComparison.OrdinalIgnoreCase))
        {
            return Result.Fail("invalid subkey");
        }

        return Result.Ok(subkey.GetPubKey(true).ToHex(true));
    }
}
