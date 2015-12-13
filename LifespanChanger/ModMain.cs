using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using System;
using System.Collections.Generic;
using System.IO;

namespace LifespanChanger
{
    public class ModMain : IUserMod
    {
        public const string SETTINGFILENAME = "LifespanChanger.xml";
        public string MODNAME = "Lifespan Changer";
        public const string DEFAULT = "Default";
        public const string MAX_AGE = "Maximum age";
        public const string SENIOR_AGE = "Age when they become seniors";
        public const string MORE_RANDOM_AGE = "More random age";
        public const string VERY_RANDOM_AGE = "Very random age";

        public static string[] LifespanValues = new string[]
        {
            DEFAULT,
            MORE_RANDOM_AGE,
            VERY_RANDOM_AGE,
            MAX_AGE,
            SENIOR_AGE,
        };

        public static string configPath;

        private PluginManager.PluginInfo pluginInfo;

        List<RedirectCallsState> m_redirectionStates = new List<RedirectCallsState>();

		public RedirectCallsState[] revertMethods = new RedirectCallsState[8];

		public string Name
		{
			get
			{
                return MODNAME;
			}
        }

        public string Description
        {
            get { return "Changes cims' lifespan to a specified or more random age."; }
        }

        public ModMain()
        {
            this.pluginInfo = getPluginInfo();
        }

        public static ModConfiguration ModConf;

        public bool isActive()
        {
            if (this.pluginInfo == null)
            {
                return true;
            }
            return this.pluginInfo.isEnabled;
        }

        private PluginManager.PluginInfo getPluginInfo()
        {
            foreach (PluginManager.PluginInfo current in Singleton<PluginManager>.instance.GetPluginsInfo())
            {
                if (current.name == "573925048" && current.publishedFileID.ToString() == "573925048")
                {
                    return current;
                }
            }
            return null;
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            if (!isActive())
            {
                return;
            }
            this.InitConfigFile();
            UIHelperBase group = helper.AddGroup("Lifespan settings");
            int num = Array.IndexOf<string>(ModMain.LifespanValues, ModMain.ModConf.LifespanValue);
            if (num < 0)
            {
                num = 0;
            }
            UIDropDown dropDown = (UIDropDown)group.AddDropdown("Cims' lifespan", ModMain.LifespanValues, num, delegate (int c)
            {
                ModMain.ModConf.LifespanValue = ModMain.LifespanValues[c];
                ModConfiguration.Serialize(ModMain.configPath, ModMain.ModConf);
            });
            dropDown.width *= 3f;
            dropDown.listWidth = (int)dropDown.width;
        }

        private void InitConfigFile()
        {
            try
            {
                string pathName = GameSettings.FindSettingsFileByName("gameSettings").pathName;
                string str = "";
                if (pathName != "")
                {
                    str = Path.GetDirectoryName(pathName) + Path.DirectorySeparatorChar;
                }
                ModMain.configPath = str + SETTINGFILENAME;
                ModMain.ModConf = ModConfiguration.Deserialize(ModMain.configPath);
                if (ModMain.ModConf == null)
                {
                    ModMain.ModConf = ModConfiguration.Deserialize(SETTINGFILENAME);
                    if (ModMain.ModConf != null && ModConfiguration.Serialize(str + SETTINGFILENAME, ModMain.ModConf))
                    {
                        try
                        {
                            File.Delete(SETTINGFILENAME);
                        }
                        catch
                        {
                        }
                    }
                }
                if (ModMain.ModConf == null)
                {
                    ModMain.ModConf = new ModConfiguration();
                    if (!ModConfiguration.Serialize(ModMain.configPath, ModMain.ModConf))
                    {
                        ModMain.configPath = SETTINGFILENAME;
                        ModConfiguration.Serialize(ModMain.configPath, ModMain.ModConf);
                    }
                }
            }
            catch
            {
            }
        }
    }
    
    public sealed class LoadingExtension : LoadingExtensionBase
    {
        List<RedirectCallsState> m_redirectionStates = new List<RedirectCallsState>();

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame)
            {
                RedirectionHelper.RedirectCalls(m_redirectionStates, typeof(ResidentAI), typeof(CustomResidentAI), "UpdateAge", 2);
                RedirectionHelper.RedirectCalls(m_redirectionStates, typeof(CustomResidentAI), typeof(ResidentAI), "FinishSchoolOrWork", 2);
                RedirectionHelper.RedirectCalls(m_redirectionStates, typeof(CustomResidentAI), typeof(ResidentAI), "Die", 2);
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            foreach (RedirectCallsState rcs in m_redirectionStates)
            {
                RedirectionHelper.RevertRedirect(rcs);
            }
        }
    }
}