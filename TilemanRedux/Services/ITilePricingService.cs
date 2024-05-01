namespace TilemanRedux.Services;
public interface ITilePricingService
{
	float CalculateTilePrice(
		int difficultyMode,
		float currentTilePrice,
		float startingTilePrice,
		float tilePriceRaise,
		int tilesBought);
}
