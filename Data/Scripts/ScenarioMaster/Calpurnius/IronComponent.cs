using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Scenario.Calpurnius
{
	internal class IronComponent
	{
		private static readonly List<IronComponent> IronComponents = new List<IronComponent>();
		private static int _totalProbability;

		static IronComponent()
		{
			AddComponentType("LargeTube", 30, 3);
			AddComponentType("SteelPlate", 21, 7);
			AddComponentType("Construction", 10, 6);
			AddComponentType("Girder", 7, 2);
			AddComponentType("SmallTube", 5, 4);
			AddComponentType("InteriorPlate", 3.5f, 3);
		}

		private static void AddComponentType(string subtypeId, float ironValue, int probability)
		{
			var startRange = _totalProbability;
			_totalProbability += probability;
			IronComponents.Add(new IronComponent(subtypeId, ironValue, startRange, _totalProbability));
		}

		internal static IronComponent GenerateComponent(Random random)
		{
			var randomNumber = random.Next(_totalProbability);
			foreach (var compType in IronComponents)
			{
				if (compType.IsThisYourNumber(randomNumber))
				{
					return compType;
				}
			}
			throw new InvalidOperationException("No random type for number: " + randomNumber); // This should never happen!
		}

		internal string SubtypeId { get; }
		internal float IronValue { get; }
		internal MyObjectBuilder_Component ObjectBuilder { get; }
		private readonly int probabilityRangeStart, probabilityRangeEnd; // Start (inclusive) to end (exclusive)

		private IronComponent(string subtypeId, float ironValue, int probabilityRangeStart, int probabilityRangeEnd)
		{
			SubtypeId = subtypeId;
			IronValue = ironValue;
			this.probabilityRangeStart = probabilityRangeStart;
			this.probabilityRangeEnd = probabilityRangeEnd;
			ObjectBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Component>(subtypeId);
		}

		private bool IsThisYourNumber(int number)
		{
			if (number < probabilityRangeStart)
			{
				return false;
			}
			return number < probabilityRangeEnd;
		}
	}
}