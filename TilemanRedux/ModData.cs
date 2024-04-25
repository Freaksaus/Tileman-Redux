namespace TilemanRedux;

internal sealed class ModData
{
	public bool ToPlaceTiles { get; set; } = true;
	public bool DoCollision { get; set; } = true;
	public bool AllowPlayerPlacement { get; set; } = false;
	public bool ToggleOverlay { get; set; } = true;
	public float TilePrice { get; set; } = 1f;
	public float TilePriceRaise { get; set; } = 0.0008f;
	public int DifficultyMode { get; set; } = 1;
	public int PurchaseCount { get; set; } = 0;
	public int OverlayMode { get; set; } = 0;
}
