using System.Diagnostics.CodeAnalysis;

[assembly:
    SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "<Pending>", Scope = "member",
        Target =
            "~M:ScrubbyWeb.Services.SuicideService.GetSuicidesForRound(System.Int32)~System.Threading.Tasks.Task{System.Collections.Generic.List{ScrubbyCommon.Data.Events.Suicide}}")]
[assembly:
    SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task",
        Justification = "<Pending>", Scope = "member",
        Target =
            "~M:ScrubbyWeb.Services.SuicideService.GetSuicidesForCKey(ScrubbyCommon.Data.CKey,System.Nullable{System.DateTime},System.Nullable{System.DateTime})~System.Threading.Tasks.Task{System.Collections.Generic.List{ScrubbyCommon.Data.Events.Suicide}}")]