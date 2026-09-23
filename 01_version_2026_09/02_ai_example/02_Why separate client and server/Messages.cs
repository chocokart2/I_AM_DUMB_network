namespace Example2;

// ---------------------------------------------------------------------------
// 메시지 (2.7절)
// 클라이언트와 서버가 주고받는 "순수한 데이터".
// 숫자와 문자열만 담는다. 객체 참조를 담지 않아야 3장에서 바이트로 바꿔 네트워크로 보낼 수 있다.
// ---------------------------------------------------------------------------
public abstract record Message;

// Client -> Server: "이 방향으로 가고 싶다" (의도, 2.6절)
// dx, dy는 각각 -1, 0, +1
public sealed record MoveRequest(int Dx, int Dy) : Message;

// Client -> Server: "내 위치는 여기다" (결과)
// 나쁜 설계. 실험 2-1, 2-2에서만 쓴다.
public sealed record PositionRequest(float X, float Y) : Message;

// Server -> Client: "진짜 위치는 여기다"
public sealed record StateResponse(float X, float Y) : Message;

// Server -> Client: 맵 모양. 접속 직후 한 번 보낸다.
// Cells는 한 줄로 이어 붙인 맵 (' ' = 빈칸, '#' = 벽). 길이 = Width * Height
// 클라이언트는 이걸 "그리기"에만 쓴다. 벽에 막히는지는 서버만 판단한다.
public sealed record MapInfo(int Width, int Height, string Cells) : Message;

public static class Grid
{
    // 좌표(float) -> 칸 번호. 0.5는 위로 올린다. (Math.Round의 기본 방식은 8.5 -> 8이라 쓰지 않는다)
    public static int ToCell(float v) => (int)MathF.Floor(v + 0.5f);
}
