using System;
using System.Collections.Generic;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Scenario.Calpurnius
{
	internal class CargoType
	{
		private static readonly List<CargoType> AllTypes = new List<CargoType>();
		private static int _totalProbability;
		private static readonly IObjectBuilderFactory ComponentType = new ComponentObjectBuilder();
		private static readonly IObjectBuilderFactory AmmoType = new AmmoObjectBuilder();
		private static readonly IObjectBuilderFactory IngotType = new IngotObjectBuilder();

		private static void AddComponent(string subtypeName, int amountPerCargoContainer, int probability)
		{
			Add(subtypeName, amountPerCargoContainer, probability, ComponentType);
		}

		private static void AddAmmo(string subtypeName, int amountPerCargoContainer, int probability)
		{
			Add(subtypeName, amountPerCargoContainer, probability, AmmoType);
		}

		private static void AddIngot(string subtypeName, int amountPerCargoContainer, int probability)
		{
			Add(subtypeName, amountPerCargoContainer, probability, IngotType);
		}

		private static void Add(string subtypeName, int amountPerCargoContainer, int probability,
			IObjectBuilderFactory objectBuilderFactory)
		{
			var startRange = _totalProbability;
			_totalProbability += probability;
			AllTypes.Add(new CargoType(subtypeName, amountPerCargoContainer, startRange, _totalProbability, objectBuilderFactory));
		}

		static CargoType()
		{
			AddComponent("SteelPlate", 500, 2);
			AddComponent("MetalGrid", 300, 4);
			AddComponent("Construction", 350, 5);
			AddComponent("InteriorPlate", 300, 3);
			AddComponent("Girder", 300, 2);
			AddComponent("SmallTube", 300, 4);
			AddComponent("LargeTube", 125, 4);
			AddComponent("Motor", 300, 2);
			AddComponent("Display", 200, 2);
			AddComponent("BulletproofGlass", 300, 3);
			AddComponent("Computer", 250, 3);
			AddComponent("Reactor", 100, 2);
			AddComponent("Medical", 50, 2);
			AddComponent("RadioCommunication", 50, 2);
			AddComponent("Explosives", 30, 2);
			AddComponent("SolarCell", 200, 4);
			AddComponent("PowerCell", 150, 4);
			AddAmmo("NATO_5p56x45mm", 100, 2);
			AddAmmo("NATO_25x184mm", 50, 5);
			AddAmmo("Missile200mm", 30, 4);
			AddIngot("Uranium", 15, 3);
		}

		internal static CargoType GenerateRandomCargo(Random random)
		{
			var randomNumber = random.Next(_totalProbability);
			foreach (var cargo in AllTypes)
			{
				if (cargo.IsThisYourNumber(randomNumber))
				{
					return cargo;
				}
			}
			throw new InvalidOperationException("No random type for number: " + randomNumber); // This should never happen!
		}

		internal readonly int AmountPerCargoContainer;
		private readonly string subtypeName;
		private readonly int probabilityRangeStart, probabilityRangeEnd; // Start (inclusive) to end (exclusive)
		private readonly IObjectBuilderFactory objectBuilderFactory;

		CargoType(string subtypeName, int amountPerCargoContainer, int probabilityRangeStart, int probabilityRangeEnd,
			IObjectBuilderFactory objectBuilderFactory)
		{
			this.subtypeName = subtypeName;
			this.probabilityRangeStart = probabilityRangeStart;
			this.probabilityRangeEnd = probabilityRangeEnd;
			this.objectBuilderFactory = objectBuilderFactory;
			AmountPerCargoContainer = amountPerCargoContainer;
		}

		private bool IsThisYourNumber(int number)
		{
			if (number < probabilityRangeStart)
			{
				return false;
			}
			return number < probabilityRangeEnd;
		}

		internal string GetDisplayName()
		{
			// This is kind of shitty but can't find the real ones
			if (subtypeName.Equals("SmallTube"))
			{
				return MyTexts.GetString("DisplayName_Item_SmallSteelTube");
			}
			if (subtypeName.Equals("LargeTube"))
			{
				return MyTexts.GetString("DisplayName_Item_LargeSteelTube");
			}
			if (subtypeName.Equals("Construction"))
			{
				return MyTexts.GetString("DisplayName_Item_ConstructionComponent");
			}
			if (subtypeName.Equals("RadioCommunication"))
			{
				return MyTexts.GetString("DisplayName_Item_RadioCommunicationComponents");
			}
			if (subtypeName.Equals("Uranium"))
			{
				return MyTexts.GetString("DisplayName_Item_UraniumIngot");
			}
			if (subtypeName.Equals("Reactor"))
			{
				return MyTexts.GetString("DisplayName_Item_ReactorComponents");
			}
			if (subtypeName.Equals("Medical"))
			{
				return MyTexts.GetString("DisplayName_Item_MedicalComponents");
			}

			return MyTexts.GetString("DisplayName_Item_" + subtypeName);
		}

		internal MyObjectBuilder_Base GetObjectBuilder()
		{
			return objectBuilderFactory.GetObjectBuilder(subtypeName);
		}

		private interface IObjectBuilderFactory
		{
			MyObjectBuilder_PhysicalObject GetObjectBuilder(String subtypeName);
		}

		private class ComponentObjectBuilder : IObjectBuilderFactory
		{
			public MyObjectBuilder_PhysicalObject GetObjectBuilder(string subtypeName)
			{
				return MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Component>(subtypeName);
			}
		}

		private class AmmoObjectBuilder : IObjectBuilderFactory
		{
			public MyObjectBuilder_PhysicalObject GetObjectBuilder(string subtypeName)
			{
				return MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_AmmoMagazine>(subtypeName);
			}
		}

		private class IngotObjectBuilder : IObjectBuilderFactory
		{
			public MyObjectBuilder_PhysicalObject GetObjectBuilder(string subtypeName)
			{
				return MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ingot>(subtypeName);
			}
		}
	}
}