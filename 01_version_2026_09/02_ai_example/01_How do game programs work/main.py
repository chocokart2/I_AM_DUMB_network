"""
예제 1 — 미니 게임 루프

교과서: 01_ai_textbook/01_How do game programs work/00_main.md

콘솔에서 20x10 맵 위의 '@'를 방향키/WASD로 움직이는 게임.
Windows 전용 (키 상태를 읽기 위해 Windows API를 사용한다).

교과서의 함수 이름과 이 파일의 함수 이름:
    Input()  -> process_input()   (Python 내장 함수 input과 겹치지 않도록)
    Update() -> update()
    Render() -> render()
"""

import argparse
import ctypes
import msvcrt
import os
import sys
import time
from dataclasses import dataclass

MAP_W = 20
MAP_H = 10
SPEED = 5.0  # 초당 5칸 (1.5절: 프레임이 아니라 시간 기준)

# Windows 가상 키 코드
VK_LEFT, VK_UP, VK_RIGHT, VK_DOWN = 0x25, 0x26, 0x27, 0x28


# ---------------------------------------------------------------------------
# 게임 상태 (1.3절)
# 게임 세계가 "지금" 어떤 모습인지 나타내는 데이터는 전부 여기에 모은다.
# ---------------------------------------------------------------------------
@dataclass
class GameState:
    x: float = 4.0
    y: float = 2.0
    running: bool = True


# 이번 프레임에 눌려 있는 키. 게임 상태가 아니라 "입력"이다.
@dataclass
class InputState:
    left: bool = False
    right: bool = False
    up: bool = False
    down: bool = False
    quit: bool = False


# ---------------------------------------------------------------------------
# Input (1.2절)
# 키를 "기다리지 않고" 지금 눌려 있는지만 확인한다.
# ---------------------------------------------------------------------------
def key_down(vk: int) -> bool:
    return bool(ctypes.windll.user32.GetAsyncKeyState(vk) & 0x8000)


def process_input(blocking: bool) -> InputState:
    if blocking:
        # 실험 1-3: 계산기처럼 키가 눌릴 때까지 멈춘다. 게임 전체가 여기서 멈춘다.
        msvcrt.getwch()

    # 콘솔에 쌓인 키 입력을 비운다. (안 비우면 종료 후 셸에 글자가 쏟아진다)
    while msvcrt.kbhit():
        msvcrt.getwch()

    return InputState(
        left=key_down(VK_LEFT) or key_down(ord("A")),
        right=key_down(VK_RIGHT) or key_down(ord("D")),
        up=key_down(VK_UP) or key_down(ord("W")),
        down=key_down(VK_DOWN) or key_down(ord("S")),
        quit=key_down(ord("Q")),
    )


# ---------------------------------------------------------------------------
# Update (1.4절, 1.5절)
# 게임 상태를 "바꾸는" 유일한 곳. 화면에 대해서는 아무것도 모른다.
# ---------------------------------------------------------------------------
def clamp(value: float, low: float, high: float) -> float:
    return max(low, min(high, value))


def update(state: GameState, inp: InputState, dt: float, use_dt: bool, sleep_sec: float) -> None:
    if sleep_sec > 0:
        # 실험 1-4: Update가 느려지면 다음 프레임의 dt가 커진다.
        time.sleep(sleep_sec)

    if inp.quit:
        state.running = False
        return

    dx = int(inp.right) - int(inp.left)
    dy = int(inp.down) - int(inp.up)

    if use_dt:
        step = SPEED * dt  # 시간 기준: FPS와 상관없이 초당 5칸
    else:
        step = SPEED / 60  # 프레임 기준: 60 FPS일 때만 초당 5칸 (실험 1-1)

    state.x = clamp(state.x + dx * step, 0, MAP_W - 1)
    state.y = clamp(state.y + dy * step, 0, MAP_H - 1)


# ---------------------------------------------------------------------------
# Render (1.4절)
# 게임 상태를 "읽기만" 한다. 여기서 state를 바꾸면 안 된다.
# ---------------------------------------------------------------------------
def render(state: GameState, fps: int) -> None:
    px, py = round(state.x), round(state.y)

    lines = [f"FPS: {fps:<3}  Pos: ({state.x:4.1f}, {state.y:4.1f})   [Q] quit"]
    lines.append("+" + "-" * MAP_W + "+")
    for y in range(MAP_H):
        row = "".join("@" if (x, y) == (px, py) else " " for x in range(MAP_W))
        lines.append("|" + row + "|")
    lines.append("+" + "-" * MAP_W + "+")

    # 화면을 지우지 않고 커서만 맨 위로 옮겨서 덮어쓴다 (깜빡임 방지)
    sys.stdout.write("\x1b[H" + "\n".join(lines) + "\n")
    sys.stdout.flush()


# ---------------------------------------------------------------------------
# 게임 루프 (1.2절, 1.5절)
# ---------------------------------------------------------------------------
def main() -> None:
    parser = argparse.ArgumentParser(description="예제 1 — 미니 게임 루프")
    parser.add_argument("--fps", type=int, default=60, help="목표 FPS (기본 60)")
    parser.add_argument("--no-dt", action="store_true", help="dt를 쓰지 않고 프레임 기준으로 이동 (실험 1-1)")
    parser.add_argument("--blocking-input", action="store_true", help="입력을 기다리는 방식 (실험 1-3)")
    parser.add_argument("--update-sleep", type=float, default=0.0, help="Update 안에서 멈출 시간(초) (실험 1-4)")
    parser.add_argument("--no-render", action="store_true", help="Render를 호출하지 않음 (실험 1-5)")
    args = parser.parse_args()

    os.system("")  # Windows 콘솔에서 ANSI 제어 문자 사용 켜기
    sys.stdout.write("\x1b[2J\x1b[?25l")  # 화면 지우기, 커서 숨기기

    state = GameState()
    frame_time = 1.0 / args.fps

    fps = 0
    frame_count = 0
    fps_timer = 0.0
    total_time = 0.0
    log_timer = 0.0

    last = time.perf_counter()
    try:
        while state.running:
            now = time.perf_counter()
            dt = now - last
            last = now

            inp = process_input(args.blocking_input)
            update(state, inp, dt, use_dt=not args.no_dt, sleep_sec=args.update_sleep)

            # FPS 측정: 1초 동안 몇 프레임 돌았는지 센다
            frame_count += 1
            fps_timer += dt
            if fps_timer >= 1.0:
                fps = frame_count
                frame_count = 0
                fps_timer -= 1.0

            total_time += dt
            if args.no_render:
                # 실험 1-5: 화면 없이도 상태가 바뀌는지 1초마다 로그로 확인
                log_timer += dt
                if log_timer >= 1.0:
                    log_timer -= 1.0
                    print(f"[log] t={total_time:5.1f}s  fps={fps:<3}  pos=({state.x:4.1f}, {state.y:4.1f})")
            else:
                render(state, fps)

            # 프레임 제한: 남은 시간만큼 쉰다
            elapsed = time.perf_counter() - now
            if elapsed < frame_time:
                time.sleep(frame_time - elapsed)
    except KeyboardInterrupt:
        pass
    finally:
        while msvcrt.kbhit():
            msvcrt.getwch()
        sys.stdout.write("\x1b[?25h\n")  # 커서 다시 보이기
        sys.stdout.flush()


if __name__ == "__main__":
    main()
