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

    [HttpGet("status")]
    public async ValueTask<ActionResult<InitStatus>> GetStatusAsync()
    {
        var res = await vault.LoadInternalAsync();
        return new InitStatus(
            res.IsSuccess && !string.IsNullOrEmpty(res.Value.Subkey.Signature),
            res.ValueOrDefault?.Subkey?.Public ?? string.Empty);
    }

    [HttpPost("generate")]
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
            return Problem(save.Errors.First().Message);
        }

        return new InitStatus(false, internals.Subkey.Public);
    }

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
        return res.IsSuccess ? Ok() : Problem("failed to init");
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
