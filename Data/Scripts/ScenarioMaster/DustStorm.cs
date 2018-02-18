using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Scenario
{
	public class DustStorm : ModSystemUpdatable
	{
		private readonly List<DustCloud> DustClouds = new List<DustCloud>();
		private readonly IMyEntity playerEntity;
		private readonly MyPlanet planet;

		public DustStorm()
		{
			playerEntity = MyAPIGateway.Session?.Player?.Controller?.ControlledEntity?.Entity;
			planet = DuckUtils.FindPlanetInGravity(playerEntity.GetPosition());

//TODO find vector4 of wheat color and apply into particle effect
			
			MyAPIGateway.Utilities.ShowNotification("Wheat: " + Color.Wheat.ToVector4(), 30000);


//			DustClouds.Add(new DustCloud(center =>
//				new MatrixD(center) {Translation = center.Translation + center.Up * 3}));
//			DustClouds.Add(new DustCloud(center =>
//				new MatrixD(center) {Translation = center.Translation + center.Forward * 30}));
			DustClouds.Add(new DustCloud(center =>
				new MatrixD(center) {Translation = center.Translation + center.Forward * 1}));
//			DustClouds.Add(new DustCloud(center =>
//				new MatrixD(center) {Translation = center.Translation + center.Forward * 60}));
//			DustClouds.Add(new DustCloud(center =>
//				new MatrixD(center) {Translation = center.Translation + center.Right * 3}));
		}

		public override void Update60()
		{
			if (playerEntity != null && planet != null)
			{				
				foreach (var dustCloud in DustClouds)
				{
					dustCloud.UpdateRaycasts();
				}
			}
		}

		public void Draw()
		{
			MatrixD origin = MyAPIGateway.Session.Camera.WorldMatrix;
			//playerEntity.WorldMatrix;
			origin.Up = -Vector3D.Normalize(planet.GetGravityAtPoint(playerEntity.GetPosition()));
			origin.Translation = origin.Translation + 2 * origin.Up;
			foreach (var dustCloud in DustClouds)
			{
				dustCloud.Draw(origin);
			}
		}
	}


	internal class DustCloud
	{
		private Vector4 debugLineColor = Color.AliceBlue;

		private MyParticleEffect effect;
		private readonly Func<MatrixD, MatrixD> positionAlgorithm;
		internal MatrixD AbovePosition { get; set; }
		internal MatrixD TargetPosition { get; set; }
		internal bool ShouldDraw { get; set; }

		internal DustCloud(Func<MatrixD, MatrixD> positionAlgorithm)
		{
			this.positionAlgorithm = positionAlgorithm;
		}

		internal void UpdateRaycasts()
		{
			ShouldDraw = true; 
			IHitInfo hitinfo;
			if (MyAPIGateway.Physics.CastRay(AbovePosition.Translation, TargetPosition.Translation, out hitinfo))
			{
				var distSq = Vector3D.DistanceSquared(TargetPosition.Translation, hitinfo.Position);
				if (distSq > 25) // More than 5m indicates it could be hitting a roof
				{
					ShouldDraw = false;
				}
			}
			//	MyAPIGateway.Utilities.ShowNotification("Drawing: " + ShouldDraw, 1000);
		}

		internal void Draw(MatrixD aboveplayer)
		{
			TargetPosition = positionAlgorithm(aboveplayer);
			AbovePosition = new MatrixD(TargetPosition)
			{
				Translation = TargetPosition.Translation + TargetPosition.Up * 100
			};

			//TODO hide behind debug bool
			MySimpleObjectDraw.DrawLine(AbovePosition.Translation, TargetPosition.Translation,
				VRage.Utils.MyStringId.GetOrCompute("Square"), ref debugLineColor, 0.05f);

			if (ShouldDraw && (effect == null || effect.IsStopped))
			{
				MyParticlesManager.TryCreateParticleEffect("sandstorm", out effect);
				effect.UserScale = 1.0f;
				effect.Loop = true;
			}

			if (ShouldDraw)
			{
				//TODO why does this work but not just using the TargetPosition =/
				effect.WorldMatrix = MatrixD.CreateWorld(TargetPosition.Translation, TargetPosition.Up, TargetPosition.Forward);
			}
			else
			{
				if (effect != null)
				{
					effect.Stop();
				}
			}
		}

		/*
			
			//MyParticleEffect effect;

			///PlanetCrashDust? Landing_Jet_Ground?
			//	if (MyParticlesManager.TryCreateParticleEffect("sandstorm", out effect))
//{
			//effect.UserScale = 10.0f;
			//	effect.UserAxisScale = new Vector3(10);//
			//		effect.UserRadiusMultiplier = 4;//
			//		effect.UserEmitterScale = 60;//
			//		effect.UserBirthMultiplier = 10;//
			//
			//	effect.Loop = true;//
			//	effect.WorldMatrix = entity.WorldMatrix;
			//	}
			//MyVisualScriptLogicProvider.FogSetAll(0.7f, 0.7f, Color.Wheat);
			//MyVisualScriptLogicProvider. ScreenColorFadingSetColor(Color.Wheat);

			//	MyVisualScriptLogicProvider.SetCustomSkybox("C:\\Users\\Stu\\AppData\\Roaming\\SpaceEngineers\\Mods\\EscapeFromMars\\Textures\\SB_U_D1.dds");	
			//	MyVisualScriptLogicProvider.FogSetAll(0f, 0f, Color.Wheat);

			//	MyVisualScriptLogicProvider.ScreenColorFadingStart(15.0f, false);
			
			// Was in Draw()
			/*	if (effect != null && entity != null)
				{
					effect.WorldMatrix = entity.WorldMatrix;
				}
			*/
	}
}