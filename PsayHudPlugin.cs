using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Commands.Targeting;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using PanoramaManager;

namespace PsayHud;

public sealed class PsayHudPlugin : BasePlugin, IPluginConfig<PsayHudConfig>
{
    private const string LayoutPath = "panorama/layout/custom_game/psay_card.vxml_c";
    private const string RootPanelId = "PsayRoot";
    private const string CardPanelId = "PsayCard";
    private const string CloseButtonId = "psay_close";
    private const string HeaderVariable = "psay_header";
    private const string MessageVariable = "psay_message";
    private const float SlideDurationSeconds = 0.55f;
    private const int MaximumMessageLength = 800;

    private readonly Dictionary<int, Timer> _entryTimers = new();
    private readonly Dictionary<int, Timer> _closeTimers = new();
    private PanelHandle? _card;

    public PsayHudConfig Config { get; set; } = new();

    public override string ModuleName => "Psay HUD";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "GSM-RO";
    public override string ModuleDescription => "Private messages displayed in a Panorama HUD card.";

    public void OnConfigParsed(PsayHudConfig config)
    {
        config.Validate();
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        Panorama.Init(this);
        _card = Panorama.Spawn(
            LayoutPath,
            new LayoutContract
            {
                RootPanelId = RootPanelId,
                RowCount = 0,
                CloseButtonId = CloseButtonId,
                CaptureInput = true,
                RevealClass = "shown",
                HideFromSpectators = true
            });
        _card.OnEvent += OnCardEvent;
    }

    public override void Unload(bool hotReload)
    {
        KillTimers(_entryTimers);
        KillTimers(_closeTimers);

        if (_card is not null)
        {
            _card.OnEvent -= OnCardEvent;
            _card.Dispose();
            _card = null;
        }

        Panorama.Shutdown();
    }

    [ConsoleCommand("css_psay", "Send a private Panorama message to a player.")]
    [CommandHelper(minArgs: 2, usage: "<#userid, name, @all, @ct or @t> <message>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPsay(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller is not null
            && (caller is not { IsValid: true }
                || !AdminManager.PlayerHasPermissions(caller, Config.AdminPermission)))
        {
            command.ReplyToCommand("You do not have permission to use this command.");
            return;
        }

        if (command.ArgCount < 3)
        {
            command.ReplyToCommand("Usage: css_psay <#userid, name, @all, @ct or @t> <message>");
            return;
        }

        var selector = command.ArgByIndex(1);
        var message = string.Join(
            " ",
            Enumerable.Range(2, command.ArgCount - 2).Select(command.ArgByIndex));

        if (string.IsNullOrWhiteSpace(message))
        {
            command.ReplyToCommand("The message cannot be empty.");
            return;
        }

        if (message.Length > MaximumMessageLength)
        {
            command.ReplyToCommand($"The message is too long (maximum {MaximumMessageLength} characters).");
            return;
        }

        if (!TryResolvePlayers(selector, caller, out var targets, out var error))
        {
            command.ReplyToCommand(error);
            return;
        }

        if (_card is null)
        {
            command.ReplyToCommand("The Panorama message panel is not available.");
            Logger.LogError("Cannot send a private HUD message because the Panorama panel was not initialized.");
            return;
        }

        foreach (var target in targets)
            SendMessage(target, message);
    }

    private void SendMessage(CCSPlayerController target, string message)
    {
        if (_card is not { } card || target is not { IsValid: true })
            return;

        CancelTimer(_entryTimers, target.Slot);
        CancelTimer(_closeTimers, target.Slot);

        card.Open(target);
        card.SetVariableFor(target, HeaderVariable, Config.MessageHeader);
        card.SetVariableFor(target, MessageVariable, message);

        var recipientSlot = target.Slot;
        var recipientUserId = target.UserId;
        _entryTimers[recipientSlot] = AddTimer(0.05f, () =>
        {
            _entryTimers.Remove(recipientSlot);

            if (target is { IsValid: true }
                && target.UserId == recipientUserId
                && _card is { } card
                && card.IsOpenFor(target))
            {
                card.SetClassFor(target, CardPanelId, "entered", true);
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private void OnCardEvent(PanelEvent panelEvent)
    {
        if (panelEvent.Action != PanelAction.Button || panelEvent.ElementId != CloseButtonId)
            return;

        var player = panelEvent.Player;
        if (player is not { IsValid: true } || _card is not { } card || !card.IsOpenFor(player))
            return;

        var slot = player.Slot;
        if (_closeTimers.ContainsKey(slot))
            return;

        CancelTimer(_entryTimers, slot);
        card.SetClassFor(player, CardPanelId, "entered", false);
        card.SetClassFor(player, CardPanelId, "leaving-right", true);

        var recipientUserId = player.UserId;
        _closeTimers[slot] = AddTimer(SlideDurationSeconds, () =>
        {
            _closeTimers.Remove(slot);

            if (player is { IsValid: true }
                && player.UserId == recipientUserId
                && _card is { } currentCard
                && currentCard.IsOpenFor(player))
            {
                currentCard.Close(player);
            }
        }, TimerFlags.STOP_ON_MAPCHANGE);
    }

    private static bool TryResolvePlayers(
        string selector,
        CCSPlayerController? caller,
        out List<CCSPlayerController> targets,
        out string error)
    {
        targets = new List<CCSPlayerController>();
        error = "No player matched that name or userid.";

        if (selector.Equals("@all", StringComparison.OrdinalIgnoreCase)
            || selector.Equals("@ct", StringComparison.OrdinalIgnoreCase)
            || selector.Equals("@t", StringComparison.OrdinalIgnoreCase))
        {
            var result = Target.ProcessTargetString(
                caller,
                selector,
                ProcessTargetFilterFlag.None,
                tnIsMl: false,
                out _,
                out var matchedPlayers);

            if (result != ProcessTargetResultFlag.TargetFound)
            {
                error = selector.Equals("@all", StringComparison.OrdinalIgnoreCase)
                    ? "There are no players to message."
                    : $"There are no players in the {selector.ToUpperInvariant()} team to message.";
                return false;
            }

            targets = matchedPlayers
                .Where(player => player is { IsValid: true, IsHLTV: false })
                .ToList();

            if (targets.Count == 0)
            {
                error = "There are no players to message.";
                return false;
            }

            return true;
        }

        if (selector.StartsWith('#'))
        {
            if (!int.TryParse(selector.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var userId))
            {
                error = "Userid must use the format #<number>.";
                return false;
            }

            var playerByUserId = Utilities.GetPlayerFromUserid(userId);
            if (playerByUserId is { IsValid: true })
            {
                targets.Add(playerByUserId);
                return true;
            }

            return false;
        }

        var players = Utilities.GetPlayers()
            .Where(player => player is { IsValid: true, IsHLTV: false })
            .ToList();
        var exactMatches = players
            .Where(player => string.Equals(player.PlayerName, selector, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (exactMatches.Count == 1)
        {
            targets.Add(exactMatches[0]);
            return true;
        }

        if (exactMatches.Count > 1)
        {
            error = "More than one player has that exact name. Use #userid instead.";
            return false;
        }

        var partialMatches = players
            .Where(player => player.PlayerName.Contains(selector, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (partialMatches.Count == 1)
        {
            targets.Add(partialMatches[0]);
            return true;
        }

        if (partialMatches.Count > 1)
            error = "That name matches multiple players. Use a more specific name or #userid.";

        return false;
    }

    private static void CancelTimer(Dictionary<int, Timer> timers, int slot)
    {
        if (timers.Remove(slot, out var timer))
            timer.Kill();
    }

    private static void KillTimers(Dictionary<int, Timer> timers)
    {
        foreach (var timer in timers.Values)
            timer.Kill();

        timers.Clear();
    }
}
