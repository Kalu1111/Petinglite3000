using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using PetingoLightUI;
using System.Threading.Tasks;
using System.Threading;

public class InterceptKeys
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYDOWN = 0x0100;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    public static KeyCombo KeyCombo_TOGGLE_PAUSE { get; private set; } = new KeyCombo(new int[] { 162, 165, 80 }, new Action(() => { MainManager.Instance.TogglePauseResume(); }), 4000); //AltGr+P to Pause/Resume


    public static void Hook()
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                GetModuleHandle(curModule.ModuleName), 0);
        }
    }
    public static void UnHook()
    {
        UnhookWindowsHookEx(_hookID);
    }
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
        {
            int vkCode = Marshal.ReadInt32(lParam);

            KeyCombo_TOGGLE_PAUSE.Check(vkCode);
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }






    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
        IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}


public class KeyCombo
{
    private byte idx = 0;
    private int cooldownTimerTargetMs = 0;
    private readonly int[] combo;
    public Action Action { get; private set; }
    public bool inCooldown { get; private set; } = false;


    public KeyCombo(int[] combo, Action action, int cooldown)
    {
        this.combo = combo;
        this.Action = action;
        cooldownTimerTargetMs = cooldown;
    }

    public bool Check(int key)
    {
        if (!inCooldown)
            if (combo[idx] == key)
            {
                idx++;
                if (combo.Length == idx)
                {
                    idx = 0;
                    if (Action != null)
                    {
                        Action();
                        ReloadCooldown();
                    }
                }
                return true;
            }
            else
            {
                if (idx != 0)
                {
                    idx = 0;
                    Check(key);
                }
            }

        return false;
    }

    public void ReloadCooldown()
    {
        inCooldown = true;
        Task.Run(() =>
        {
            lock (Action)
            {
                Thread.Sleep(cooldownTimerTargetMs);
                inCooldown = false;
            }
        });
    }
}