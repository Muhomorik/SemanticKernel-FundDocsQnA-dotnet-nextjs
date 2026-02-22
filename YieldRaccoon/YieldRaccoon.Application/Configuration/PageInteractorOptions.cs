namespace YieldRaccoon.Application.Configuration;

/// <summary>
/// Configurable delay timings for page interactions in the AboutFund browser.
/// Registered at the composition root; consumed by the page interactor implementation.
/// </summary>
/// <param name="MinDelayMs">Post-click delay to let the page react before the next interaction.</param>
/// <param name="PanelOpenDelayMs">Delay for the side panel open/close animations to complete.</param>
public record PageInteractorOptions(int MinDelayMs, int PanelOpenDelayMs);
