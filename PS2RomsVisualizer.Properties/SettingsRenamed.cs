using System.CodeDom.Compiler;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace PS2RomsVisualizer.Properties
{
	internal sealed class SettingsRenamed : ApplicationSettingsBase
	{
        public static SettingsRenamed Default { get; } = (SettingsRenamed)Synchronized(new SettingsRenamed());
        [DefaultSettingValue("")]
		public string gamesPath
		{
			get
			{
				return (string)this["gamesPath"];
			}
			set
			{
				this["gamesPath"] = value;
			}
		}
		[DefaultSettingValue("")]
		public string bannersPath
		{
			get
			{
				return (string)this["bannersPath"];
			}
			set
			{
				this["bannersPath"] = value;
			}
		}
	}
}
