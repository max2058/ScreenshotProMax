using System.ComponentModel;
using System.Globalization;
using ScreenshotProMax.Resources;

namespace ScreenshotProMax.Localization
{
    public class LocalizedStrings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public void Refresh()
        {
            // Raise PropertyChanged for all properties so UI updates
            OnPropertyChanged(nameof(SettFlyLang));
            OnPropertyChanged(nameof(SettFlyStyle));
            OnPropertyChanged(nameof(SettFlyAppDetailInfo));
            OnPropertyChanged(nameof(AppVersionText));
            OnPropertyChanged(nameof(SettFlyMainCol));
            OnPropertyChanged(nameof(TtNewBugOrFeatureCommand));
            OnPropertyChanged(nameof(MasterInfoText));
            OnPropertyChanged(nameof(SettFlyHeader));
            // add more as needed
        }

        public string SettFlyLang => LanguageGUI.SettFlyLang;
        public string SettFlyStyle => LanguageGUI.SettFlyStyle;
        public string SettFlyAppDetailInfo => LanguageGUI.SettFlyAppDetailInfo;
        public string AppVersionText => LanguageGUI.AppVersion;
        public string SettFlyMainCol => LanguageGUI.SettFlyMainCol;
        public string TtNewBugOrFeatureCommand => LanguageGUI.TtNewBugOrFeatureCommand;
        public string MasterInfoText => LanguageGUI.ResourceManager.GetString("MasterInfoText", LanguageGUI.Culture) ?? string.Empty;
        public string SettFlyHeader => LanguageGUI.ResourceManager.GetString("SettFlyHeader", LanguageGUI.Culture) ?? string.Empty;
    }
}
