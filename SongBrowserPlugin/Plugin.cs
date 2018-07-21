﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRUI;
using IllusionPlugin;
using TMPro;
using UnityEngine.UI;
using System.Collections;


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
			get { return "v2.0"; }
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
            //if (level != SongBrowserMasterViewController.MenuIndex) return;
            //SongBrowserMasterViewController.OnLoad();

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