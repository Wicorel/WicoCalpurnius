using VRage.Game.ModAPI;

namespace Scenario
{
	public abstract class ModSystemUpdatable : ModSystemCloseable
	{
		public virtual void GridInitialising(IMyCubeGrid grid)
		{
		}		

		public virtual void AllGridsInitialised()
		{	
		}
		
		public virtual void Update30()
		{
		}	

		public virtual void Update60()
		{
		}	

		public virtual void Update300()
		{
		}	

		public virtual void Update1200()
		{
		}
	}
}