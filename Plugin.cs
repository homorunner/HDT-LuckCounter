using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.BobsBuddy;
using Hearthstone_Deck_Tracker.Plugins;
using HarmonyLib;

public class MsgBox
{
    [DllImport("User32.dll", CharSet = CharSet.Auto, ThrowOnUnmappableChar = false)]
    static extern int MessageBox(IntPtr handle, string message, string title, int type);

    public static void ShowMessage(string title, string message)
    {
        MessageBox(IntPtr.Zero, message, title, 0);
    }
}

namespace HDT_LuckCounter
{
    public class LuckCounterPlugin: IPlugin
    {
        public MenuItem MenuItem { get; private set; }

        internal static LuckCounterPanel luckCounterPanel = null;

        internal const int COUNT = 1000_0000;
        internal const double VALUE_WIN = 1.0;
        internal const double VALUE_LOSE = -2.0;
        internal const double VALUE_KILL = 0.5;
        internal const double VALUE_BEKILLED = -0.5;
        internal static double[] lucky_value = new double[COUNT];
        internal static Random random = new Random();
        internal static Type BobsBuddyInvokerType;
        internal static MethodInfo ValidateSimulationResultAsyncMethod;
        internal static MethodInfo GetLastCombatResultMethod;
        internal static MethodInfo GetLastLethalResultMethod;
        internal static MethodInfo GetLastCombatDamageDealtMethod;
        internal static PropertyInfo OutputProperty;
        internal static Object _lock = new();

        public string Author
        {
            get { return "Homorunner"; }
        }

        public string ButtonText
        {
            get { return "No Settings"; }
        }

        public string Description
        {
            get { return "Show how lucky you are during battleground combats."; }
        }

        public string Name
        {
            get { return "HDT_LuckCounter"; }
        }

        public void OnButtonPress()
        {
        }

        public static void AfterValidateSimulationResult(
            Object __instance
        )
        {
            // TODO: skip duos, check if battling with KelThuzad

            // TODO: check and skip if output result is not reliable

            lock (_lock)
            {
                var combatResult = (CombatResult)GetLastCombatResultMethod.Invoke(__instance, null);
                var lethalResult = (LethalResult)GetLastLethalResultMethod.Invoke(__instance, null);
                var damage = (int)GetLastCombatDamageDealtMethod.Invoke(__instance, null);
                var output = OutputProperty.GetValue(__instance) as BobsBuddy.Simulation.Output;

                var lambda = 1.0;

                if (combatResult == CombatResult.Win)
                {
                    lucky_value[0] += VALUE_WIN;
                    if (lethalResult == LethalResult.OpponentDied)
                    {
                        lucky_value[0] += VALUE_KILL;
                    }
                }
                else if (combatResult == CombatResult.Loss)
                {
                    lucky_value[0] += VALUE_LOSE;
                    if (lethalResult == LethalResult.FriendlyDied)
                    {
                        lucky_value[0] += VALUE_BEKILLED;
                        lambda -= 0.1;
                    }
                }

                luckCounterPanel.SetDebugInfo($"win={output.winRate} draw={1-output.winRate-output.lossRate} loss={output.lossRate} my_death={output.myDeathRate} their_death={output.theirDeathRate}");

                double total_percentage_counter = 0.5;
                for (int i = 1; i < COUNT; i++)
                {
                    var x = random.NextDouble();
                    if (x <= output.winRate)
                    {
                        lucky_value[i] += VALUE_WIN;
                        if (x <= output.theirDeathRate)
                        {
                            lucky_value[i] += VALUE_KILL;
                        }
                    }
                    else if ((1 - x) <= output.lossRate)
                    {
                        lucky_value[i] += VALUE_LOSE;
                        if ((1 - x) <= output.myDeathRate)
                        {
                            lucky_value[i] += VALUE_BEKILLED;
                        }
                    }
                    if (Math.Abs(lucky_value[i] - lucky_value[0]) < 1e-7)
                    {
                        total_percentage_counter += 0.5;
                    }
                    else if (lucky_value[i] < lucky_value[0])
                    {
                        total_percentage_counter += 1;
                    }
                }

                if (lambda != 1.0)
                {
                    for (int i = 0; i < COUNT; i++)
                    {
                        lucky_value[i] *= lambda;
                    }
                }

                luckCounterPanel.SetCounter(total_percentage_counter, COUNT);
            }
        }

        public void OnLoad()
        {
            CreateMenuItem();
            MenuItem.IsChecked = true;

            for (int i = 0; i < COUNT; i++)
            {
                lucky_value[i] = 0;
            }

            try
            {
                BobsBuddyInvokerType = Type.GetType("Hearthstone_Deck_Tracker.BobsBuddy.BobsBuddyInvoker, HearthstoneDeckTracker, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                if (BobsBuddyInvokerType == null)
                {
                    throw new Exception("GetType BobsBuddyInvokerType failed");
                }

                ValidateSimulationResultAsyncMethod = BobsBuddyInvokerType.GetMethod("ValidateSimulationResultAsync", BindingFlags.NonPublic | BindingFlags.Instance);
                if (ValidateSimulationResultAsyncMethod == null)
                {
                    throw new Exception("GetMethod ValidateSimulationResultAsync failed");
                }

                GetLastCombatResultMethod = BobsBuddyInvokerType.GetMethod("GetLastCombatResult", BindingFlags.NonPublic | BindingFlags.Instance);
                if (GetLastCombatResultMethod == null)
                {
                    throw new Exception("GetMethod GetLastCombatResultMethod failed");
                }

                GetLastLethalResultMethod = BobsBuddyInvokerType.GetMethod("GetLastLethalResult", BindingFlags.NonPublic | BindingFlags.Instance);
                if (GetLastLethalResultMethod == null)
                {
                    throw new Exception("GetMethod GetLastLethalResultMethod failed");
                }

                GetLastCombatDamageDealtMethod = BobsBuddyInvokerType.GetMethod("GetLastCombatDamageDealt", BindingFlags.NonPublic | BindingFlags.Instance);
                if (GetLastCombatDamageDealtMethod == null)
                {
                    throw new Exception("GetMethod GetLastCombatDamageDealtMethod failed");
                }

                OutputProperty = BobsBuddyInvokerType.GetProperty("Output", BindingFlags.Public | BindingFlags.Instance);
                if (OutputProperty == null)
                {
                    throw new Exception("GetField OutputField.public failed");
                }

                new Harmony("luck-counter-plugin").Patch(
                    ValidateSimulationResultAsyncMethod,
                    new HarmonyMethod(typeof(LuckCounterPlugin).GetMethod("AfterValidateSimulationResult", BindingFlags.Public | BindingFlags.Static))
                );
            }
            catch (Exception e)
            {
                MsgBox.ShowMessage("插件启动失败", $"exception={e}");
            }

            new Task(()=>
            {
                Thread.Sleep(5000);
            }).Start();
        }

        public void OnUnload()
        {
            MenuItem.IsChecked = false;
        }

        public void OnUpdate()
        {
        }

        private void CreateMenuItem()
        {
            MenuItem = new MenuItem()
            {
                Header = "Luck Counter"
            };

            MenuItem.IsCheckable = true;

            MenuItem.Checked += (sender, args) =>
            {
                if (luckCounterPanel == null)
                {
                    luckCounterPanel = new LuckCounterPanel();
                    Core.OverlayCanvas.Children.Add(luckCounterPanel);
                    luckCounterPanel.Visibility = System.Windows.Visibility.Visible;
                }
            };

            MenuItem.Unchecked += (sender, args) =>
            {
                using (luckCounterPanel)
                {
                    Core.OverlayCanvas.Children.Remove(luckCounterPanel);
                    luckCounterPanel = null;
                }
            };
        }

        public Version Version
        {
            get { return new Version(0, 1, 0); }
        }
    }
}

/* TODO: maybe show some messages like:
 *  终极无敌至尊非酋王
 *  万里挑一非酋王
 *  千里挑一非酋王
 *  大非酋
 *  小非酋
 *  小欧皇
 *  大欧皇
 *  千里挑一欧皇王
 *  万里挑一欧皇王
 *  终极无敌至尊欧皇王'
*/
