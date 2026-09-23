using System.Text;

namespace Example2;

// ---------------------------------------------------------------------------
// 클라이언트 (2.2절 ~ 2.4절)
// - 입력을 읽어 요청을 보낸다.
// - 서버가 보낸 결과로 "사본"을 갱신한다.
// - 사본을 읽어 화면을 그린다.
//
// 이 파일은 Server 클래스를 참조하지 않는다. (직접 구현 순서 6번)
// 클라이언트가 가진 것은 메시지 통로 두 개뿐이다.
// ---------------------------------------------------------------------------
public sealed class Client
{
    public sealed class Options
    {
        public bool TrustMode;    // 실험 2-1, 2-2: 위치를 직접 계산해서 보낸다
        public bool ClientFirst;  // 실험 2-3: 서버를 기다리지 않고 사본을 먼저 움직인다
    }

    // 실험 2-1 ~ 2-3에서만 쓴다. 원래 속도는 게임 규칙이라 클라이언트가 몰라야 한다.
    // 클라이언트가 규칙을 흉내 내기 시작하면, 서버와 규칙이 달라질 여지가 생긴다.
    private const float LocalSpeed = 5.0f;

    // 클라이언트 쪽 상태 = 서버 상태의 사본 (2.2절)
    private float _x = float.NaN;   // 서버에게 첫 응답을 받기 전에는 위치를 모른다
    private float _y = float.NaN;
    private int _mapWidth;
    private int _mapHeight;
    private string _mapCells = "";

    private int _inputDx;
    private int _inputDy;

    private readonly Pipe _outgoing;
    private readonly Pipe _incoming;
    private readonly Options _options;

    public Client(Pipe outgoing, Pipe incoming, Options options)
    {
        _outgoing = outgoing;
        _incoming = incoming;
        _options = options;
    }

    public bool QuitRequested { get; private set; }

    // 디버그 전용: Program의 관찰용 화면에서 사본 좌표를 보여줄 때 쓴다.
    public (float X, float Y) DebugCopyPosition => (_x, _y);

    // 키를 읽고, 요청을 toServer 큐에 넣는다.
    public void Input(float dt)
    {
        Keyboard.FlushConsoleBuffer();

        // 종료는 게임 상태가 아니라 이 프로그램의 일이라서 서버에 묻지 않는다.
        if (Keyboard.IsDown('Q'))
        {
            QuitRequested = true;
            return;
        }

        bool left = Keyboard.IsDown(Keyboard.Left) || Keyboard.IsDown('A');
        bool right = Keyboard.IsDown(Keyboard.Right) || Keyboard.IsDown('D');
        bool up = Keyboard.IsDown(Keyboard.Up) || Keyboard.IsDown('W');
        bool down = Keyboard.IsDown(Keyboard.Down) || Keyboard.IsDown('S');
        _inputDx = (right ? 1 : 0) - (left ? 1 : 0);
        _inputDy = (down ? 1 : 0) - (up ? 1 : 0);

        if (float.IsNaN(_x))
            return; // 아직 서버에게 위치를 못 받았다

        if (_options.TrustMode)
        {
            // 나쁜 예 (실험 2-1): 클라이언트가 위치를 직접 계산한다. 벽 검사는 없다.
            _x += _inputDx * LocalSpeed * dt;
            _y += _inputDy * LocalSpeed * dt;

            // 실험 2-2: T를 누르면 말도 안 되는 위치를 보낸다.
            if (Keyboard.IsDown('T'))
                _x = 9999f;

            _outgoing.Send(new PositionRequest(_x, _y));
            return;
        }

        if (_options.ClientFirst)
        {
            // 실험 2-3: 서버를 기다리지 않고 사본을 먼저 움직인다. 벽 검사는 없다. (서버만 한다)
            _x += _inputDx * LocalSpeed * dt;
            _y += _inputDy * LocalSpeed * dt;
        }

        // 좋은 예: 결과가 아니라 의도만 보낸다.
        _outgoing.Send(new MoveRequest(_inputDx, _inputDy));
    }

    // toClient 큐에서 결과를 꺼내 사본을 갱신한다.
    public void ReceiveResponses()
    {
        while (_incoming.TryReceive(out var msg))
        {
            switch (msg)
            {
                case MapInfo map:
                    _mapWidth = map.Width;
                    _mapHeight = map.Height;
                    _mapCells = map.Cells;
                    break;

                case StateResponse state:
                    // 실험 2-1 ~ 2-3에서는 서버 결과를 무시하고 내 계산을 믿는다.
                    // 서버 결과로 되돌리는 방법(Reconciliation)은 13장에서 배운다.
                    // 단, 처음 위치는 서버에게 받아야 한다.
                    if ((_options.TrustMode || _options.ClientFirst) && !float.IsNaN(_x))
                        break;
                    _x = state.X;
                    _y = state.Y;
                    break;
            }
        }
    }

    // 사본을 읽어서 그린다. 여기서 사본을 바꾸지 않는다. (1.4절)
    public string Render()
    {
        var sb = new StringBuilder();
        string border = "+" + new string('-', _mapWidth) + "+";

        if (_mapCells.Length == 0)
            return "서버에게 맵을 받는 중...\n";

        int px = float.IsNaN(_x) ? -1 : Grid.ToCell(_x);
        int py = float.IsNaN(_y) ? -1 : Grid.ToCell(_y);

        sb.Append(border).Append('\n');
        for (int y = 0; y < _mapHeight; y++)
        {
            sb.Append('|');
            for (int x = 0; x < _mapWidth; x++)
            {
                if (x == px && y == py)
                    sb.Append('@');
                else
                    sb.Append(_mapCells[y * _mapWidth + x]);
            }
            sb.Append('|').Append('\n');
        }
        sb.Append(border).Append('\n');

        // 맵 밖에 있으면 @가 안 보이므로 알려 준다 (실험 2-1, 2-2)
        bool outside = px < 0 || px >= _mapWidth || py < 0 || py >= _mapHeight;
        sb.Append(outside && px != -1 ? "(@가 맵 밖에 있다)           \n" : "                             \n");
        return sb.ToString();
    }
}
