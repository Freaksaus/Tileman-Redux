using StardewModdingAPI.Utilities;

namespace TilemanRedux;

/// <summary>
/// Configuration settings
/// </summary>
internal sealed class ModConfig
{
	/// <summary>
	/// Keybinding used to toggle the tile overlay
	/// </summary>
	public KeybindList ToggleOverlayKey { get; set; } = KeybindList.Parse("G");

	/// <summary>
	/// Keybinding used to toggle the tile overlay mode
	/// </summary>
	public KeybindList ToggleOverlayModeKey { get; set; } = KeybindList.Parse("H");
}
