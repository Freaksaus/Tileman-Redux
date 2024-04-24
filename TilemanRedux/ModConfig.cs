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

	/// <summary>
	/// Keybinding used to change the difficulty
	/// </summary>
	public KeybindList ChangeDifficultyKey { get; set; }

	/// <summary>
	/// Default tile price, this gets used as the base price upon which the prices get raised based on the difficulty
	/// </summary>
	public int TilePrice { get; set; } = 1;

	/// <summary>
	/// Amount to raise the tile price with when on the easiest difficulty
	/// </summary>
	public float TilePriceRaise { get; set; } = 0.0008f;

	/// <summary>
	/// The default difficulty mode to use for new saves
	/// </summary>
	public int DifficultyMode { get; set; } = 1;
}
