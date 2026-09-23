namespace Example2;

// ---------------------------------------------------------------------------
// 메시지 통로 (2.7절 "메시지 큐")
// 한쪽 방향으로만 흐르는 큐. toServer, toClient 두 개를 만들어 쓴다.
// 3장에서는 이 클래스 자리에 네트워크(Socket)가 들어간다.
//
// 보내는 쪽은 Send()로 넣고 바로 다음 일을 한다. (받는 쪽을 기다리지 않는다)
// 받는 쪽은 자기 차례에 TryReceive()로 도착한 것만 꺼낸다. (없으면 그냥 지나간다)
// ---------------------------------------------------------------------------
public sealed class Pipe
{
    private readonly Queue<(long DeliverFrame, Message Msg)> _queue = new();
    private readonly int _delayFrames;
    private long _frame;

    // delayFrames: 넣은 메시지가 몇 프레임 뒤에 꺼내지는가 (실험 2-4: 네트워크 지연 흉내)
    public Pipe(int delayFrames = 0)
    {
        _delayFrames = delayFrames;
    }

    public void Send(Message msg)
    {
        _queue.Enqueue((_frame + _delayFrames, msg));
    }

    public bool TryReceive(out Message msg)
    {
        if (_queue.Count > 0 && _queue.Peek().DeliverFrame <= _frame)
        {
            msg = _queue.Dequeue().Msg;
            return true;
        }
        msg = null!;
        return false;
    }

    // 게임 루프가 프레임마다 한 번 호출한다.
    public void Tick() => _frame++;
}
