using Sandbox.Game.Entities;
using Sandbox.ModAPI;

namespace Scenario
{
	public class InterruptingAudioSystem
	{
		private MyEntity3DSoundEmitter interruptingSoundEmitter;
		private long playerEntityId2 = -1;
		private IAudioClip currentlyPlaying;

		public void EnsurePlaying(IAudioClip clip)
		{
			if (currentlyPlaying != null && currentlyPlaying.Id == clip.Id)
			{
				return;
			}

			var player = MyAPIGateway.Session.Player;
			var ent = player?.Controller?.ControlledEntity?.Entity;
			if (ent != null)
			{
				if (playerEntityId2 != ent.EntityId)
				{
					if (interruptingSoundEmitter != null)
					{
						interruptingSoundEmitter.StopSound(true);
					}

					interruptingSoundEmitter = new MyEntity3DSoundEmitter(ent as VRage.Game.Entity.MyEntity);
					playerEntityId2 = ent.EntityId;
				}

				var soundPair = new MySoundPair(clip.Filename);
				interruptingSoundEmitter.StopSound(true);
				interruptingSoundEmitter.PlaySingleSound(soundPair);
				currentlyPlaying = clip;
			}
		}

		public void Stop()
		{
			if (interruptingSoundEmitter != null)
			{
				interruptingSoundEmitter.StopSound(true);
			}
			currentlyPlaying = null;
		}
	}
}