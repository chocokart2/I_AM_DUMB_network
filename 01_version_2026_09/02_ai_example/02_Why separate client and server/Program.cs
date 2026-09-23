// 예제 2 — 싱글플레이 게임을 클라이언트/서버로 나누기
//
// 교과서: 01_ai_textbook/02_Why separate client and server/00_main.md
//
// 1장의 미니 게임 루프를 "같은 프로그램 안에서" Client와 Server로 나눈다.
// Client와 Server는 서로를 모르고, 메시지 통로(Pipe) 두 개로만 대화한다.
// Windows 전용 (키 상태를 읽기 위해 Windows API를 사용한다).

using System.Diagnostics;
using System.Text;
using Example2;

var opt = ParseArgs(args);
if (opt is null)
    return;

// --- 통로 두 개 (2.7절) -------------------------------------------------------
var toServer = new Pipe(delayFrames: opt.Delay);   // 실험 2-4: 요청이 늦게 도착한다
var toClient = new Pipe();

// --- 서버와 클라이언트 --------------------------------------------------------
// 서로의 참조를 넘기지 않는다. 각자 받는 것은 통로뿐이다.
var server = new Server(incoming: toServer, outgoing: toClient,
    new Server.Options { TrustClient = opt.TrustClient, NoResponse = opt.NoResponse });
var client = new Client(outgoing: toServer, incoming: toClient,
    new Client.Options { TrustMode = opt.TrustClient, ClientFirst = opt.ClientFirst });

Console.OutputEncoding = Encoding.UTF8;
Console.Clear();
Console.CursorVisible = false;

double frameTime = 1.0 / opt.Fps;
int fps = 0, frameCount = 0;
double fpsTimer = 0;

var clock = Stopwatch.StartNew();
double last = clock.Elapsed.TotalSeconds;

try
{
    while (!client.QuitRequested)
    {
        double now = clock.Elapsed.TotalSeconds;
        float dt = (float)(now - last);
        last = now;

        // --- 클라이언트 ---
        client.Input(dt);             // 키를 읽고, 요청을 toServer 큐에 넣는다

        // --- 서버 ---
        server.ReceiveRequests();     // toServer 큐에서 요청을 꺼낸다
        server.Update(dt);            // 검증하고 진짜 상태를 바꾼다
        server.SendState();           // 결과를 toClient 큐에 넣는다

        // --- 클라이언트 ---
        client.ReceiveResponses();    // toClient 큐에서 결과를 꺼내 사본을 갱신한다
        string screen = client.Render();

        toServer.Tick();
        toClient.Tick();

        // FPS 측정
        frameCount++;
        fpsTimer += dt;
        if (fpsTimer >= 1.0)
        {
            fps = frameCount;
            frameCount = 0;
            fpsTimer -= 1.0;
        }

        // 관찰용 줄: 이 프로그램을 "밖에서 보는 사람"의 시점이다.
        // 서버 좌표는 원래 클라이언트가 볼 수 없는 값이지만, 학습을 위해 같이 보여준다.
        var (sx, sy) = server.DebugPosition;
        var (cx, cy) = client.DebugCopyPosition;
        string debug = $"FPS: {fps,-3}  Server: ({sx,6:F1}, {sy,6:F1})  Client: ({cx,6:F1}, {cy,6:F1})   [Q] quit";

        Console.SetCursorPosition(0, 0);
        Console.Write(debug + "\n" + screen + ModeLine(opt));

        double elapsed = clock.Elapsed.TotalSeconds - now;
        if (elapsed < frameTime)
            Thread.Sleep(TimeSpan.FromSeconds(frameTime - elapsed));
    }
}
finally
{
    Keyboard.FlushConsoleBuffer();
    Console.CursorVisible = true;
    Console.WriteLine();
}

// ---------------------------------------------------------------------------

static string ModeLine(Opt o)
{
    var modes = new List<string>();
    if (o.TrustClient) modes.Add("--trust-client (T: x=9999)");
    if (o.ClientFirst) modes.Add("--client-first");
    if (o.Delay > 0) modes.Add($"--delay {o.Delay}");
    if (o.NoResponse) modes.Add("--no-response");
    return modes.Count == 0 ? "실험 옵션 없음                                  \n"
                            : "실험: " + string.Join(", ", modes) + "        \n";
}

static Opt? ParseArgs(string[] args)
{
    var o = new Opt();
    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--fps" when i + 1 < args.Length && int.TryParse(args[i + 1], out var f) && f > 0:
                o.Fps = f; i++; break;
            case "--delay" when i + 1 < args.Length && int.TryParse(args[i + 1], out var d) && d >= 0:
                o.Delay = d; i++; break;
            case "--trust-client": o.TrustClient = true; break;
            case "--client-first": o.ClientFirst = true; break;
            case "--no-response": o.NoResponse = true; break;
            default:
                Console.WriteLine($"알 수 없는 옵션: {args[i]}");
                Console.WriteLine("""
                    사용법: dotnet run -- [옵션]
                      --fps N          목표 FPS (기본 60)
                      --trust-client   클라이언트가 위치를 보내고 서버는 그대로 믿는다 (실험 2-1, 2-2)
                      --client-first   클라이언트가 사본을 먼저 움직이고 서버 결과는 무시한다 (실험 2-3)
                      --delay N        요청이 N프레임 뒤에 서버에 도착한다 (실험 2-4)
                      --no-response    서버가 결과를 보내지 않는다 (실험 2-5)
                    """);
                return null;
        }
    }
    return o;
}

sealed class Opt
{
    public int Fps = 60;
    public int Delay;
    public bool TrustClient;
    public bool ClientFirst;
    public bool NoResponse;
}
