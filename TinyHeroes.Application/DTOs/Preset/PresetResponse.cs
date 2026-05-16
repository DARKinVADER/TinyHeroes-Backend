namespace TinyHeroes.Application.DTOs.Preset;

public record PresetResponse(Guid Id, string Label, string ImageValue, bool Enabled, bool IsSystem);
public record CreatePresetRequest(string Label, string ImageValue);
