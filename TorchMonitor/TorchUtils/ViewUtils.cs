﻿using System.Threading;
using System.Windows;

namespace TorchUtils
{
    internal static class ViewUtils
    {
        public static void CopyToClipboard(string text)
        {
            var thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
    }
}