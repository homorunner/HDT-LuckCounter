using System;
using System.Windows.Controls;

namespace HDT_LuckCounter
{
    /// <summary>
    /// LuckCounterPanel.xaml 的交互逻辑
    /// </summary>
    public partial class LuckCounterPanel : UserControl, IDisposable
    {
        public LuckCounterPanel()
        {
            InitializeComponent();
        }

        public void SetCounter(double counter, double total)
        {
            LeaderText.Text = $"今日运势：{Math.Round(counter * 100 / total)}% ({counter}/{total})";
        }

        public void Dispose()
        {
        }
    }
}
