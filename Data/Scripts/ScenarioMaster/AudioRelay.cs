namespace Scenario
{
	internal interface IAudioRelay
	{
		void QueueAudioMessageOnAllClients(IAudioClip audioClip);
	}
}