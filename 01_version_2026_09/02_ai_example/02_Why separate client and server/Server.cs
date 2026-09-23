namespace Example2;

// ---------------------------------------------------------------------------
// 서버 (2.2절 ~ 2.4절)
// - 진짜 게임 상태를 가진 유일한 곳
// - 게임 규칙(속도, 벽, 맵 경계)을 아는 유일한 곳
// - 화면도 입력 장치도 없다. 메시지를 받고, 상태를 바꾸고, 결과를 보낸다.
//
// 이 파일은 Client 클래스를 참조하지 않는다. 서버는 누가 메시지를 보냈는지 모른다.
// ---------------------------------------------------------------------------
public sealed class Server
{
    public sealed class Options
    {
        public bool TrustClient;   // 실험 2-1, 2-2: 클라이언트가 보낸 위치를 검증 없이 저장
        public bool NoResponse;    // 실험 2-5: StateResponse를 보내지 않음
    }

    // 게임 규칙. 규칙의 권위는 서버에 있으므로 속도도 서버만 안다.
    private const float Speed = 5.0f; // 초당 5칸

    private static readonly string[] MapRows =
    {
        "                    ",
        "         #          ",
        "         #          ",
        "         #          ",
        "                    ",
        "                    ",
        "   ####             ",
        "              #     ",
        "              #     ",
        "                    ",
    };

    // 진짜 게임 상태 (1.3절). private이라 바깥에서 바꿀 수 없다.
    private sealed class GameState
    {
        public float X = 4.0f;
        public float Y = 2.0f;
        public int InputDx;   // 가장 최근에 받은 이동 의도
        public int InputDy;
    }

    private readonly GameState _state = new();
    private readonly Pipe _incoming;
    private readonly Pipe _outgoing;
    private readonly Options _options;
    private readonly int _width = MapRows[0].Length;
    private readonly int _height = MapRows.Length;

    public Server(Pipe incoming, Pipe outgoing, Options options)
    {
        _incoming = incoming;
        _outgoing = outgoing;
        _options = options;

        // 접속 직후 맵 모양과 처음 위치를 알려 준다. (2장에서는 클라이언트가 하나뿐이고 처음부터 접속해 있다고 친다)
        _outgoing.Send(new MapInfo(_width, _height, string.Concat(MapRows)));
        _outgoing.Send(new StateResponse(_state.X, _state.Y));
    }

    // 디버그 전용: Program의 관찰용 화면에서만 읽는다.
    // 실제 게임이라면 이 값은 다른 컴퓨터에 있어서 클라이언트가 절대 볼 수 없다.
    public (float X, float Y) DebugPosition => (_state.X, _state.Y);

    // toServer 큐에 쌓인 요청을 전부 꺼낸다.
    public void ReceiveRequests()
    {
        while (_incoming.TryReceive(out var msg))
        {
            switch (msg)
            {
                case MoveRequest move:
                    // 의도만 기록한다. 실제 이동은 Update에서 규칙에 따라 계산한다.
                    // 값이 -1, 0, +1 범위인지도 확인한다. (클라이언트가 dx = 100을 보낼 수도 있다)
                    _state.InputDx = Math.Clamp(move.Dx, -1, 1);
                    _state.InputDy = Math.Clamp(move.Dy, -1, 1);
                    break;

                case PositionRequest pos when _options.TrustClient:
                    // 나쁜 예 (실험 2-1, 2-2): 클라이언트가 정한 결과를 그대로 믿는다.
                    _state.X = pos.X;
                    _state.Y = pos.Y;
                    break;

                // 그 밖의 메시지는 무시한다.
            }
        }
    }

    // 진짜 상태를 바꾸는 유일한 곳 (1.4절의 Update가 서버로 이사 왔다)
    public void Update(float dt)
    {
        if (_options.TrustClient)
            return; // 위치를 클라이언트가 정하므로 서버는 할 일이 없다

        float step = Speed * dt;

        // x, y를 따로 검사해야 벽에 비스듬히 부딪혀도 벽을 따라 미끄러진다.
        float nextX = _state.X + _state.InputDx * step;
        if (CanStandOn(nextX, _state.Y))
            _state.X = nextX;

        float nextY = _state.Y + _state.InputDy * step;
        if (CanStandOn(_state.X, nextY))
            _state.Y = nextY;
    }

    // 검증 (2.6절): 그 칸에 서 있을 수 있는가?
    private bool CanStandOn(float x, float y)
    {
        int cx = Grid.ToCell(x);
        int cy = Grid.ToCell(y);
        if (cx < 0 || cx >= _width || cy < 0 || cy >= _height)
            return false;
        return MapRows[cy][cx] != '#';
    }

    // 결과를 toClient 큐에 넣는다.
    public void SendState()
    {
        if (_options.NoResponse)
            return; // 실험 2-5: 처음 위치(생성자)만 보내고 그 뒤로는 안 보낸다

        _outgoing.Send(new StateResponse(_state.X, _state.Y));
    }
}
