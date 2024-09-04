using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTTStressTest
{
    static class R
    {
        public static bool IsSlog { get; set; }
        public static void Slog(string msg)
        {
            if (IsSlog)
            {
                Console.WriteLine(msg);
            }

        }

        public static void ShowInfo(string msg) {
            System.Windows.MessageBox.Show(msg, "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        public static void ShowWarning(string msg)
        {
            System.Windows.MessageBox.Show(msg, "警告", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }

        public static void ShowError(string msg)
        {
            System.Windows.MessageBox.Show(msg, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }

        public static bool ShowConfirm(string msg)
        {
            var result = System.Windows.MessageBox.Show(msg, "确认", System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question);
            return result == System.Windows.MessageBoxResult.OK;
        }
    }
}
