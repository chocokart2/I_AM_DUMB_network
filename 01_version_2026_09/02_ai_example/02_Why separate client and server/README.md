# 예제 2 — 싱글플레이 게임을 클라이언트/서버로 나누기

교과서: [01_ai_textbook/02_Why separate client and server/00_main.md](../../01_ai_textbook/02_Why%20separate%20client%20and%20server/00_main.md)

## 실행 환경

- .NET 8 SDK 이상
- **Windows 전용** (키가 눌려 있는지 확인할 때 Windows API `GetAsyncKeyState`를 사용)
- 외부 라이브러리 없음

## 실행

이 폴더에서:

```text
dotnet run
```

| 키 | 동작 |
| --- | --- |
| 방향키 / WASD | 이동 |
| Q | 종료 |
| T | `--trust-client`일 때만: x = 9999를 보낸다 (실험 2-2) |

> `GetAsyncKeyState`는 콘솔 창에 포커스가 없어도 키를 읽습니다.
> 게임을 켜 둔 채 다른 창에서 타이핑하면 캐릭터가 움직일 수 있습니다.

## 화면

```text
FPS: 60   Server: (   4.0,    2.0)  Client: (   4.0,    2.0)   [Q] quit
+--------------------+
|                    |
|         #          |
|    @    #          |
|         #          |
...
```

첫 줄은 **관찰용**입니다. 프로그램 바깥에서 서버와 클라이언트를 동시에 들여다보는 시점입니다.
실제 온라인 게임이라면 서버 좌표는 다른 컴퓨터에 있어서 클라이언트가 볼 수 없습니다.
그래서 이 줄은 `Client`가 아니라 `Program`이 그립니다.

## 파일 구성

| 파일 | 역할 |
| --- | --- |
| `Program.cs` | 게임 루프. 통로 두 개를 만들고 `Client`와 `Server`를 번갈아 돌린다 |
| `Server.cs` | 진짜 게임 상태, 게임 규칙(속도, 벽), 검증 |
| `Client.cs` | 입력 → 요청, 응답 → 사본, 사본 → 화면 |
| `Messages.cs` | `MoveRequest`, `StateResponse`, `MapInfo` 등 순수 데이터 메시지 |
| `Pipe.cs` | 한 방향 메시지 큐. 3장에서 이 자리에 네트워크가 들어간다 |
| `Keyboard.cs` | Non-blocking 키 입력 |

`Client.cs`는 `Server`를, `Server.cs`는 `Client`를 참조하지 않습니다. 둘이 공유하는 것은 `Pipe`와 `Messages`뿐입니다.

## 코드와 교과서 연결

| 교과서 | 코드 |
| --- | --- |
| 2.2 진짜 상태와 사본 | `Server.GameState` (진짜) / `Client._x, _y` (사본) |
| 2.3 권위 | 속도·벽·맵 경계는 `Server`에만 있다 |
| 2.4 클라이언트의 책임 | `Client.Input()`, `ReceiveResponses()`, `Render()` |
| 2.4 서버의 책임 | `Server.ReceiveRequests()`, `Update()`, `SendState()` |
| 2.6 의도를 보낸다 | `MoveRequest(Dx, Dy)` |
| 2.6 검증 | `Server.CanStandOn()`, `ReceiveRequests()`의 `Math.Clamp` |
| 2.7 Request / Response | `Messages.cs` |
| 2.7 메시지 큐 | `Pipe.cs`, `Program.cs`의 `toServer`, `toClient` |
| 2.7 한 프레임의 흐름 | `Program.cs`의 `while` 루프 |

### 교과서 질문: 속도와 dt는 누가 알아야 하는가?

<details>
<summary>이 예제의 선택 (직접 생각해 본 뒤에 열어 보세요)</summary>

- **속도**는 게임 규칙이라 `Server`만 압니다(`Server.Speed`). 클라이언트가 "초당 100칸으로 가고 싶다"고 보낼 수 없도록, 클라이언트는 방향만 보냅니다.
- **dt**도 서버가 자기 시계로 잽니다. 클라이언트가 dt를 보내게 하면 "지난 프레임이 10초 걸렸다"고 거짓말해서 한 번에 50칸을 갈 수 있기 때문입니다.
- 이 예제는 한 프로그램 안이라 둘이 같은 dt를 쓰지만, 서버가 다른 컴퓨터로 가면 서버는 자기 dt(나중에는 Tick, 10장)로 계산합니다.
- `Client.LocalSpeed`는 실험 2-1 ~ 2-3에서만 쓰는 값입니다. 클라이언트가 규칙을 흉내 내는 순간 무슨 일이 생기는지 보기 위한 것입니다.

</details>

## 고장 내기 실험

코드를 고치지 않고 실행 옵션만 바꿔서 교과서의 실험을 해 볼 수 있습니다.

| 실험 | 명령 | 관찰할 것 |
| --- | --- | --- |
| 2-1 | `dotnet run -- --trust-client` | 벽을 그냥 통과한다. 맵 밖으로도 나갈 수 있다. 서버 좌표도 똑같이 따라온다 |
| 2-2 | `dotnet run -- --trust-client` 후 `T` | 서버 좌표가 9999가 된다. 서버는 아무 의심도 하지 않는다 |
| 2-3 | `dotnet run -- --client-first` | 빈 곳에서는 두 좌표가 같다. 벽으로 걸어가면 Client만 벽을 뚫고, Server는 벽 앞에 멈춘다 |
| 2-4 | `dotnet run -- --delay 6` / `--delay 30` | 키를 누른 뒤 0.1초 / 0.5초 뒤에 움직인다. 키를 떼도 그만큼 더 간다 |
| 2-5 | `dotnet run -- --no-response` | `@`는 처음 위치에서 안 움직이는데, Server 좌표는 계속 바뀐다 |

옵션은 섞어서 쓸 수 있습니다. 예를 들어 `dotnet run -- --client-first --delay 30`으로 실행하면, 캐릭터는 바로 움직이지만 서버는 0.5초 늦게 따라옵니다. 13장 Client Prediction이 푸는 문제입니다.

### 실험 2-5를 볼 때 생각해 볼 것

화면의 `@`만 보면 "캐릭터가 안 움직인다"고 느낍니다. 그런데 진짜 캐릭터는 움직이고 있습니다.
실제 게임이었다면 다른 플레이어의 화면에서는 내 캐릭터가 걸어가고 있고, 몬스터에게 맞고 있을 수도 있습니다.
**내 화면은 진짜 상태가 아니라 서버가 보내 준 사본**이기 때문입니다.

## 3장으로 가져갈 것

`Pipe`의 `Send()`와 `TryReceive()` 두 개만 네트워크로 바꿀 수 있으면, `Client`와 `Server` 코드는 거의 그대로 쓸 수 있습니다.
그러려면 `Message`를 **바이트 배열로 바꾸는 방법(직렬화)** 이 필요합니다. 이 내용은 7장에서 다룹니다.
