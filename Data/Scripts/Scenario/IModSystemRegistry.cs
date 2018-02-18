namespace Scenario
{
	public interface IModSystemRegistry
	{
		void AddCloseableModSystem(ModSystemCloseable modSystemCloseable);

		void AddUpatableModSystem(ModSystemUpdatable modSystemUpdatable);

		void AddRapidUpdatableModSystem(ModSystemRapidUpdatable modSystemRapidUpdatable);
	}
}