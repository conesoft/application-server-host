using System.Runtime.InteropServices;

namespace Conesoft.Server_Host.Features.TrayIcon.Services;

partial class TrayIconService
{
    internal partial class NativeMethodsForTrayIconService
    {
        internal static unsafe void RunMessageLoop(CancellationToken stoppingToken, Action<MSG>? messageHandler = default)
        {
            var thread = GetCurrentThreadId();
            stoppingToken.Register(() => PostThreadMessage(thread, WM_QUIT, 0, 0));

            MSG msg;
            while (GetMessage(&msg, default, 0, 0) > 0)
            {
                TranslateMessage(&msg);
                DispatchMessage(&msg);

                messageHandler?.Invoke(msg);
            }
        }

        const uint WM_QUIT = 0x0012;
        public const uint WM_SETTINGCHANGE = 0x001A;

        [LibraryImport("Kernel32")]
        private static partial uint GetCurrentThreadId();

        [LibraryImport("User32", EntryPoint = "PostThreadMessageW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool PostThreadMessage(uint threadId, uint msg, nuint wParam, nuint lParam);

        [LibraryImport("User32", EntryPoint = "GetMessageW", SetLastError = true)]
        internal static unsafe partial int GetMessage(MSG* lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [LibraryImport("User32", EntryPoint = "TranslateMessage")]
        internal static unsafe partial int TranslateMessage(MSG* lpMsg);

        [LibraryImport("User32", EntryPoint = "DispatchMessageW")]
        internal static unsafe partial nint DispatchMessage(MSG* lpMsg);

        [StructLayout(LayoutKind.Sequential)]
        internal struct MSG
        {
            internal nint hwnd;
            internal uint message;
            internal uint wParam;
            internal uint lParam;
            internal uint time;
            internal int ptX;
            internal int ptY;
        }
    }
}