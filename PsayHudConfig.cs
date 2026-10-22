using System;
using CounterStrikeSharp.API.Core;

namespace PsayHud;

public sealed class PsayHudConfig : BasePluginConfig
{
    public string AdminPermission { get; set; } = "@css/generic";
    public string MessageHeader { get; set; } = "MESAJ DE LA ADMIN";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AdminPermission) || !AdminPermission.TrimStart().StartsWith('@'))
        {
            throw new InvalidOperationException(
                "PsayHud AdminPermission must be a CounterStrikeSharp permission flag starting with '@'.");
        }

        if (string.IsNullOrWhiteSpace(MessageHeader))
        {
            throw new InvalidOperationException("PsayHud MessageHeader cannot be empty.");
        }

        AdminPermission = AdminPermission.Trim();
        MessageHeader = MessageHeader.Trim();
    }
}
