# 예제 1 — 미니 게임 루프

교과서: [01_ai_textbook/01_How do game programs work/00_main.md](../../01_ai_textbook/01_How%20do%20game%20programs%20work/00_main.md)

## 실행 환경

- Python 3.10 이상
- **Windows 전용** (키가 눌려 있는지 확인할 때 Windows API `GetAsyncKeyState`를 사용)
- 외부 라이브러리 없음

## 실행

```text
python main.py
```

| 키 | 동작 |
| --- | --- |
| 방향키 / WASD | 이동 |
| Q | 종료 |

> `GetAsyncKeyState`는 콘솔 창에 포커스가 없어도 키를 읽습니다.
> 게임을 켜 둔 채 다른 창에서 타이핑하면 캐릭터가 움직일 수 있습니다.

## 코드와 교과서 연결

| 교과서 | 코드 |
| --- | --- |
| 1.2 게임 루프 | `main()`의 `while state.running:` |
| 1.2 입력을 기다리지 않기 | `process_input()` — 키가 눌려 있는지 확인만 함 |
| 1.3 게임 상태 | `GameState` |
| 1.4 Update | `update()` — 상태를 바꾸는 유일한 함수 |
| 1.4 Render | `render()` — 상태를 읽기만 함 |
| 1.5 Delta Time | `main()`의 `dt` 계산, `update()`의 `SPEED * dt` |

## 고장 내기 실험

코드를 고치지 않고 실행 옵션만 바꿔서 교과서의 실험을 해 볼 수 있습니다.

| 실험 | 명령 | 관찰할 것 |
| --- | --- | --- |
| 1-1 | `python main.py --no-dt --fps 10` | dt가 없으면 캐릭터가 6배 느려진다 |
| 1-2 | `python main.py --fps 10` | dt가 있으면 속도는 같지만 뚝뚝 끊겨 보인다 |
| 1-3 | `python main.py --blocking-input` | 키를 안 누르면 FPS 표시가 멈춘다. 오래 기다린 뒤 키를 누르면 캐릭터가 한 번에 멀리 점프한다 (왜 그럴까?) |
| 1-4 | `python main.py --update-sleep 0.5` | 1초에 2프레임 정도만 돌고, 한 번에 2.5칸씩 점프한다 |
| 1-5 | `python main.py --no-render` | 화면이 없어도 1초마다 찍히는 로그에서 좌표가 바뀐다 |

`--no-render`로 실행하면 화면이 없어서 Q 키 안내가 보이지 않습니다. 그래도 Q나 Ctrl+C로 종료할 수 있습니다.

옵션은 섞어서 쓸 수 있습니다. 예: `python main.py --no-dt --fps 120`
