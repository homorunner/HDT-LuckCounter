using System;
using System.Windows.Controls;

namespace HDT_LuckCounter
{
    /// <summary>
    /// LuckCounterPanel.xaml 的交互逻辑
    /// </summary>
    public partial class LuckCounterPanel : UserControl, IDisposable
    {
        internal string[] lines;

        public LuckCounterPanel()
        {
            InitializeComponent();

            lines = new string[2];
        }

        void updateText()
        {
            LeaderText.Text = string.Join("\n", lines);
        }

        public void SetCounter(double counter, double total)
        {
            lines[0] = $"今日运势：{Math.Round(counter * 100 / total)}% ({counter}/{total})";

            updateText();
        }

        public void SetDebugInfo(string info)
        {
            lines[1] = info;

            updateText();
        }

        public void Dispose()
        {
        }
    }
}
