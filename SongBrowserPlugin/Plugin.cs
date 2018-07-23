using UnityEngine.SceneManagement;
using IllusionPlugin;


namespace SongBrowserPlugin
{
	public class Plugin : IPlugin
	{
		public string Name
		{
			get { return "Song Browser"; }
		}

		public string Version
		{
			get { return "v2.0-beta"; }
		}
		
		public void OnApplicationStart()
		{

        }

		public void OnApplicationQuit()
		{      

		}

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
        }

        public void OnLevelWasLoaded(int level)
		{
            //Console.WriteLine("OnLevelWasLoaded=" + level);            
            if (level != SongBrowserApplication.MenuIndex) return;
            SongBrowserApplication.OnLoad();
        }

		public void OnLevelWasInitialized(int level)
		{
            //Console.WriteLine("OnLevelWasInitialized=" + level);
        }

        public void OnUpdate()
		{
			
		}

		public void OnFixedUpdate()
		{
			
		}
	}
}