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
        return new InitStatus(res.IsSuccess);
    }

    [HttpPost("setup")]
    public async ValueTask<ActionResult> SetupAsync(Internal setup)
    {
        var loadRes = await vault.LoadInternalAsync();
        if (loadRes.IsSuccess)
        {
            return Problem("already setup");
        }

        if (string.IsNullOrEmpty(setup.Host.Domain))
        {
            return Problem("invalid domain");
        }

        var validate = ExtractPublicKey(setup.Subkey.Private, setup.Subkey.Avatar, setup.Subkey.Signature);
        if (validate.IsFailed)
        {
            return Problem(string.Join('\n', validate.Reasons.Select(r => r.Message)));
        }

        var internals = setup with
        {
            Subkey = setup.Subkey with { Public = validate.Value },
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
