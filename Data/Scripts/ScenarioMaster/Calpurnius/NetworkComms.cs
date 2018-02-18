using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Scenario.Calpurnius
{
	public class NetworkComms : ModSystemCloseable, IAudioRelay
	{
		/// <summary>
		/// Uses last 5 digits of the mod steam workshop ID so as not to collide with other mods using multiplayer messaging
		/// </summary>
		private const ushort ModId = 52597;//TODO Change

		internal HashSet<long> RegisteredPlayers { get; set; }
		private QueuedAudioSystem audio;
		private ResearchControl research;
		private ResearchHacking hacking;

		internal NetworkComms()
		{
			RegisteredPlayers = new HashSet<long>();
		}

		internal void Init(QueuedAudioSystem audioSystem, ResearchControl researchControl, ResearchHacking researchHacking)
		{
			audio = audioSystem;
			research = researchControl;
			hacking = researchHacking;
			MyAPIGateway.Multiplayer.RegisterMessageHandler(ModId, IncomingMessage);
		}

		public override void Close()
		{
			MyAPIGateway.Multiplayer.UnregisterMessageHandler(ModId, IncomingMessage);
		}

		private void IncomingMessage(byte[] bytes)
		{
			try
			{
				var message = MyAPIGateway.Utilities.SerializeFromXML<Message>(Encoding.UTF8.GetString(bytes));
				switch (message.MessageType)
				{
					case MessageType.ClientJoined: // Server only
						HandleClientJoinedMessage(message);
						break;
					case MessageType.ClearToolbar:
						StartWipeClientToolbar();
						break;
					case MessageType.ToolbarClearedSuccessfully:
						RegisteredPlayers.Add(message.PlayerId);
						break;
					case MessageType.PlaySound:
						audio.PlayAudio(CalAudioClip.GetClipFromId(message.AudioClipId));
						break;
					case MessageType.HackingProgress:
						hacking.ShowLocalHackingProgress(message.HackingProgressTicks);
						break;
					case MessageType.HackingSuccess:
						hacking.ShowLocalHackingSuccess();
						break;
					case MessageType.HackingInterrupted:
						hacking.ShowLocalHackingInterrupted();
						break;
					case MessageType.HackingInterruptStopped:
						hacking.ShowLocalHackingInterruptStopped();
						break;
					default:
						throw new Exception("Unrecognised message type: " + message.MessageType);
				}
			}
			catch (Exception e)
			{
				ModLog.Error(e);
			}
		}

		internal void StartWipeHostToolbar()
		{
			WipeToolbar(player => RegisteredPlayers.Add(player.IdentityId));
		}

		internal void StartWipeClientToolbar()
		{
			WipeToolbar(player => SendMessageToServer(new Message
			{
				MessageType = MessageType.ToolbarClearedSuccessfully,
				PlayerId = player.IdentityId
			}));
		}

		private void WipeToolbar(Action<IMyPlayer> successAction)
		{
			var player = MyAPIGateway.Session.Player;
			if (player == null)
			{
				return;
			}

			if (RegisteredPlayers.Contains(player.IdentityId))
			{
				return; // No need, they've been in the game before
			}

			if (player.Controller.ControlledEntity == null)
			{
				// ReSharper disable once ObjectCreationAsStatement
				new LateToolbarClearer(player, successAction);
			}
			else
			{
				MyVisualScriptLogicProvider.ClearAllToolbarSlots(player.IdentityId);
				successAction(player);
			}
		}

		internal class LateToolbarClearer
		{
			private readonly IMyPlayer player;
			private readonly Action<IMyPlayer> successAction;

			internal LateToolbarClearer(IMyPlayer player, Action<IMyPlayer> successAction)
			{
				this.player = player;
				this.successAction = successAction;
				player.Controller.ControlledEntityChanged += PlayerChangedController;
			}

			private void PlayerChangedController(VRage.Game.ModAPI.Interfaces.IMyControllableEntity arg1, VRage.Game.ModAPI.Interfaces.IMyControllableEntity arg2)
			{
				MyVisualScriptLogicProvider.ClearAllToolbarSlots(player.IdentityId);
				MyAPIGateway.Session.Player.Controller.ControlledEntityChanged -= PlayerChangedController;
				successAction(player);
			}
		}

		internal void HandleClientJoinedMessage(Message message)
		{
			var playerId = message.PlayerId;
			research.UnlockTechForJoiningPlayer(playerId);
			if (!RegisteredPlayers.Contains(playerId))
			{
				var msgBytes = Encoding.UTF8.GetBytes(MyAPIGateway.Utilities.SerializeToXML(new Message
				{
					SenderSteamId = MyAPIGateway.Session.Player.SteamUserId,
					MessageType = MessageType.ClearToolbar,
					PlayerId = playerId
				}));

				MyAPIGateway.Multiplayer.SendMessageTo(ModId, msgBytes, message.SenderSteamId);
			}
		}

		public void NotifyServerClientJoined(IMyPlayer player)
		{
			SendMessageToServer(new Message
			{
				MessageType = MessageType.ClientJoined,
				PlayerId = player.IdentityId
			});
		}

		private void SendMessageToServer(Message message)
		{
			message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;
			var msgBytes = Encoding.UTF8.GetBytes(MyAPIGateway.Utilities.SerializeToXML(message));
			MyAPIGateway.Multiplayer.SendMessageToServer(ModId, msgBytes);
		}

		public class Message
		{
			public MessageType MessageType { get; set; }
			public long PlayerId { get; set; }
			public ulong SenderSteamId { get; set; }
			public int AudioClipId { get; set; }
			public HashSet<TechGroup> TechGroups { get; set; }
			public int HackingProgressTicks { get; set; }
		}

		public enum MessageType
		{
			ClientJoined,
			PlaySound,
			ClearToolbar,
			ToolbarClearedSuccessfully,
			HackingProgress,
			HackingSuccess,
			HackingInterrupted,
			HackingInterruptStopped,

		}

		public void QueueAudioMessageOnAllClients(IAudioClip audioClip)
		{
			SendMessageToAllClients(new Message
			{
				MessageType = MessageType.PlaySound,
				AudioClipId = audioClip.Id
			});
		}

		private void SendMessageToAllClients(Message message)
		{
			var players = new List<IMyPlayer>();
			MyAPIGateway.Multiplayer.Players.GetPlayers(players);
			var me = MyAPIGateway.Session.Player.IdentityId;
			message.SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;
			foreach (var player in players)
			{
				if (player.IdentityId == me)
				{
					continue;
				}
				var steamId = MyAPIGateway.Multiplayer.Players.TryGetSteamId(player.IdentityId);
				if (steamId > 0)
				{
					var msgBytes = Encoding.UTF8.GetBytes(MyAPIGateway.Utilities.SerializeToXML(message));
					MyAPIGateway.Multiplayer.SendMessageTo(ModId, msgBytes, steamId);
				}
			}
		}

		public void ShowHackingProgressOnAllClients(int hackProgressTicks)
		{
			SendMessageToAllClients(new Message
			{
				MessageType = MessageType.HackingProgress,
				HackingProgressTicks = hackProgressTicks
			});
		}

		public void ShowHackingSuccessOnAllClients()
		{
			SendMessageToAllClients(new Message
			{
				MessageType = MessageType.HackingSuccess
			});
		}

		public void ShowHackingInterruptedOnAllClients()
		{
			SendMessageToAllClients(new Message
			{
				MessageType = MessageType.HackingInterrupted
			});
		}

		public void ShowHackingInterruptStoppedOnAllClients()
		{
			SendMessageToAllClients(new Message
			{
				MessageType = MessageType.HackingInterruptStopped
			});
		}
	}

}