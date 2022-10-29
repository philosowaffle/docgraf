using Common;

namespace Core.Settings
{
	public interface ISettingsService
	{
		Task<Configuration> GetSettingsAsync();
	}
}
