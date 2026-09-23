using System.Runtime.InteropServices;

namespace Example2;

// ---------------------------------------------------------------------------
// 키보드 (1.2절: 입력을 기다리지 않고 확인만 한다)
// Windows API GetAsyncKeyState로 "지금 눌려 있는가"만 본다.
// ---------------------------------------------------------------------------
public static class Keyboard
{
    public const int Left = 0x25, Up = 0x26, Right = 0x27, Down = 0x28;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public static bool IsDown(int vKey) => (GetAsyncKeyState(vKey) & 0x8000) != 0;

    public static bool IsDown(char key) => IsDown((int)char.ToUpperInvariant(key));

    // 콘솔에 쌓인 키 입력을 비운다. (안 비우면 종료 후 셸에 글자가 쏟아진다)
    public static void FlushConsoleBuffer()
    {
        while (Console.KeyAvailable)
            Console.ReadKey(intercept: true);
    }
}
